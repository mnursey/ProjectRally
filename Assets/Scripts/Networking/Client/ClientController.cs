using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public delegate void ClientConnectedCallback ();
public delegate void ClientReceiveCallback (String data);
public delegate void ClientOnSendCallback ();

[Serializable]
public enum ClientState {IDLE, JOINING, JOINED, BEGIN_ANIMATION, ANIMATION, COMMAND, PROCESSING, DISCONNECTING}

public class ClientController : MonoBehaviour {

	public PlayerController playerController;

	ClientConnectionHandler ch;
	GameRunner gr;
	public string ip;
	public int port = 10018;
	public bool send;
	public string msg;
	public ClientState state;
	GameState newGameState;

    private float timeSinceLastAnim = 0.0f;
    private float targetAnimDelay = 16.0f / 1000.0f;

    private bool safeToDisconnet = false;

	// Use this for initialization
	void Start () {
		state = ClientState.IDLE;
		ch = new ClientConnectionHandler(RequestToJoinGame, HandleIncomingData);
        playerController = GetComponent<PlayerController>();
		playerController.endTurnCallback = SendPlayerCommands;
		gr = GetComponent<GameRunner>();
	}

	void Awake() {
		Application.runInBackground = true;
	}

	public void Connect() {

        if(state == ClientState.IDLE)
        {
            state = ClientState.JOINING;
            ch.Connect(ip, port);
        }
	}
	
	// Update is called once per frame
	void Update () {

        if (send) {
			send = false;
			SendMsg(msg);
		}

		if(state == ClientState.BEGIN_ANIMATION) {
			gr.SetCommands(newGameState.cmds);
			state = ClientState.ANIMATION;
		}

		if(state == ClientState.ANIMATION) {
            timeSinceLastAnim += Time.deltaTime;
            if(timeSinceLastAnim > targetAnimDelay)
            {
                timeSinceLastAnim = 0.0f;
                if (gr.StepThroughSim())
                {
                    gr.SetGameState(newGameState);

                    state = ClientState.COMMAND;

                    if (gr.IsGameFinished())
                    {
                        // END GAME
                        playerController.uiController.EnableWinText(true, gr.GetWinner() == playerController.playerID);

                        //SendDisconnect();
                    } else
                    {
                        playerController.SetTurnTimer(newGameState.turnTimer);
                        playerController.NewTurn(gr.GetShipControllers());
                    }
                }
            }
		}

        if(state == ClientState.DISCONNECTING)
        {
            if(safeToDisconnet)
            {
                Disconnect();
                DisableSafeToDisconnect();
            }
        }
	}

    void EnableSafeToDisconnect()
    {
        safeToDisconnet = true;
    }

    void DisableSafeToDisconnect()
    {
        safeToDisconnet = false;
    }

	void OnDestroy() {
		if(state != ClientState.IDLE) {
			SendDisconnect();
		}
	}

	void Disconnect() {
		ch.Disconnect();

        playerController.ShowMainMenu();
        gr.Reset(false);

        if (state != ClientState.DISCONNECTING) {
			Debug.LogWarning("Client Disconnected ungracefully");
		} else {
			Debug.Log("Client Disconnected");
		}

        state = ClientState.IDLE;
	}

	void HandleIncomingData(String data) {
		NetworkingMessage inmsg = NetworkingMessageTranslator.ParseMessage(data);

		Debug.Log("Client received : " + data);

		if(inmsg.type == NetworkingMessageType.SERVER_JOIN_RESPONSE) {
			HandleAcceptJoin(inmsg);
		}

		if(inmsg.type == NetworkingMessageType.DISCONNECT) {
			HandleDisconnect(inmsg);
		}

		if(inmsg.type == NetworkingMessageType.GAME_STATE) {
			HandleGameState(inmsg);
		}
	}

	void HandleDisconnect(NetworkingMessage inmsg) {
		this.state = ClientState.DISCONNECTING;
        EnableSafeToDisconnect();
    }

    void HandleAcceptJoin(NetworkingMessage inmsg) {
		int playerID = int.Parse(inmsg.content);

		if(playerID > -1) {
			state = ClientState.JOINED;
			Debug.Log("Client Joined Server. PlayerID is " + playerID + " ... Waiting for game to start");
			playerController.playerID = playerID;
            gr.Reset(playerController.playerID);
		} else {
			Debug.Log("Client was not allowed to connect... Disconnecting");
			SendDisconnect();
		}
	}

	void HandleGameState(NetworkingMessage inmsg) {
		GameState ngs = NetworkingMessageTranslator.ParseGameState(inmsg.content);

		// TODO
		// Add handling for out of place turns... this should never happen tho...

		// Flag Update game runner
		state = ClientState.BEGIN_ANIMATION;
		newGameState = ngs;
	}

	void SendMsg() {
		ch.BeginSend("HELLO WORLD");
	}

	void SendMsg(String msg) {
		ch.BeginSend(msg);
	}

	void SendMsg(String msg, ClientOnSendCallback cb) {
		ch.BeginSend(msg, cb);
	}

	void SendPlayerCommands(CmdState cmd) {
		string request = NetworkingMessageTranslator.GeneratePlayerCommandsMessage(cmd);
		SendMsg(request);
	}

	void RequestToJoinGame() {
		string request = NetworkingMessageTranslator.GenerateClientJoinMessage();

		SendMsg(request);
	}

	void SendDisconnect() {
		string request = NetworkingMessageTranslator.GenerateDisconnectMessage();
		state = ClientState.DISCONNECTING;
		SendMsg(request, EnableSafeToDisconnect);
	}
}

public class ClientConnectionHandler {

	private ConnectionObject co;
	ClientConnectedCallback connectedCallback;
	ClientReceiveCallback receiveCallback;

    public ClientConnectionHandler(ClientConnectedCallback ccb, ClientReceiveCallback crb) {
		connectedCallback = ccb;
		receiveCallback = crb;
	}

	public void Connect(string address, int port) {
		//IPHostEntry ipHostInfo = Dns.GetHostEntry(address);
		IPAddress ipAddress = IPAddress.Parse(address);
		IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

		co = new ConnectionObject(new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp));
		co.socket.NoDelay = true;
		co.socket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), co);
	}

	private void ConnectCallback(IAsyncResult ar) {
		co.socket.EndConnect(ar);

		Debug.Log("Client connected to " + co.socket.RemoteEndPoint.ToString());
        co.socketConnected = true;

        BeginReceive();

		if(connectedCallback != null) {
			connectedCallback();
		}
	}

	private void BeginReceive() {
        if(CheckIfSocketIsConnected(co.socket))
        {
            co.socket.BeginReceive(co.buffer, 0, ConnectionObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), null);
        }
    }

	private void ReceiveCallback(IAsyncResult ar) {
		String data = String.Empty;

		int bytesRead = co.socket.EndReceive(ar);

		if(bytesRead > 0) {
			co.stringBuilder.Append(Encoding.ASCII.GetString(co.buffer, 0, bytesRead));

			data = co.stringBuilder.ToString();

			co.stringBuilder.Length = 0;
		}

        if(CheckIfSocketIsConnected(co.socket))
        {
            BeginReceive();
        }

        if (receiveCallback != null) {
			receiveCallback(data);
		}
	}

	public void BeginSend(String message) {
		BeginSend(message, null);
	}

	public void BeginSend(String message, ClientOnSendCallback cb) {
		byte[] byteData = Encoding.ASCII.GetBytes(message);

		if(byteData.Length > ConnectionObject.BufferSize) {
			Debug.LogWarning("Message length is larger then max buffer size");
		}
			
		if(co.socketConnected && co.socket.Connected) {
			co.socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), cb);
		} else {
			Debug.LogWarning("Did not send message because client was not connected");
		}
	}

	private void SendCallback(IAsyncResult ar) {
        if(CheckIfSocketIsConnected(co.socket))
        {
            int bytesSend = co.socket.EndSend(ar);

            if (ar.AsyncState != null)
            {
                ClientOnSendCallback cb = (ClientOnSendCallback)ar.AsyncState;
                cb();
            }
        }
	}

    bool CheckIfSocketIsConnected(Socket s)
    {
        if (!s.Connected || !co.socketConnected)
        {
            return false;
        }

        bool part1 = s.Poll(1000, SelectMode.SelectRead);
        bool part2 = (s.Available == 0);

        if (part1 && part2)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void Disconnect() {
		if(co != null && co.socket != null) {
			if(CheckIfSocketIsConnected(co.socket)) {
				co.socket.Shutdown(SocketShutdown.Send);
			}

			co.socket.Close();
            co.socketConnected = false;
		}
	}
}
