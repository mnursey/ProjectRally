using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public delegate void ServerConnectionObjectCallback (ConnectionObject sco);
public delegate void IncomingDataCallback (ConnectionObject co, String data);
public delegate void ServerOnSendCallback(ConnectionObject co);


[Serializable]
public enum ServerState {OFF, WAITING_TO_START, SETUP_GAME, STARTING, ANIMATION, COMMAND, PROCESSING, FINISHING, RESETTING}

public class ServerController : MonoBehaviour {
	
	ServerConnectionListener cl;
	public ServerConnectionHandler ch;
	GameRunner gr;

	public ServerState state;

	public int port = 10018;

	public int numberOfPlayersUntilStart = 2;
	public int playerIDTracker = 0;

    public float endOfGameDisconnectWait = 60.0f;
    private float endOfGameDisconnectWaitTimer = 0.0f;

	// Use this for initialization
	void Start () {

		state = ServerState.OFF;

		gr = GetComponent<GameRunner>();

		ch = new ServerConnectionHandler(this.HandleIncomingData);

		cl = new ServerConnectionListener(port, 100, ch.AddConnection);
	}

	void Awake() {
		Application.runInBackground = true;
	}
	
	// Update is called once per frame
	void Update () {

        if(state == ServerState.OFF && Input.GetKeyDown(KeyCode.L))
        {
            state = ServerState.WAITING_TO_START;
            cl.StartListening();
        }

        if (state == ServerState.SETUP_GAME) {
			StartGame();
		}

		if(state == ServerState.PROCESSING) {
			bool finished = false;

			while(!finished) {
				finished = gr.StepThroughSim();
			}

			if(finished) {
				// Send gamestate to players

				SendGameState();

                if(gr.IsGameFinished())
                {
                    // GAME OVER.
                    state = ServerState.FINISHING;
                } else
                {
                    gr.NewTurn();
                }
            }
		}

        if(state == ServerState.FINISHING)
        {
            // WAIT 60 seconds then disconnect clients
            //DISCONNECT FROM CLIENTS. RESET SERVER

            if(endOfGameDisconnectWaitTimer > endOfGameDisconnectWait)
            {
                SendDisconnectToAllClients();
                endOfGameDisconnectWaitTimer = 0.0f;

                // TODO
                // RESET SERVER
                state = ServerState.RESETTING;
            }
            else
            {
                endOfGameDisconnectWaitTimer += Time.deltaTime;
            }
        }

        if(state == ServerState.RESETTING)
        {
            ResetServer();
        }
    }

    void ResetServer()
    {
        gr.Reset(true);
        state = ServerState.WAITING_TO_START;

        if(playerIDTracker > Int32.MaxValue * 0.9f)
        {
            playerIDTracker = 0;
        }
    }


    void HandleIncomingData(ConnectionObject co, String data) {
		NetworkingMessage inmsg = NetworkingMessageTranslator.ParseMessage(data);

		if(inmsg.type == NetworkingMessageType.CLIENT_JOIN) {
			HandleJoin(co, inmsg);
		}

		if(inmsg.type == NetworkingMessageType.DISCONNECT) {
			HandleDisconnect(co, inmsg);
		}

		if(inmsg.type == NetworkingMessageType.CMDS) {
			HandlePlayerCommands(co, inmsg);
		}
	}

	void HandleDisconnect(ConnectionObject co, NetworkingMessage inmsg) {
		ch.CloseConnection(co);

		// Modify Game Logic??
	}

	void HandlePlayerCommands(ConnectionObject co, NetworkingMessage inmsg) {
		CmdState cmd = NetworkingMessageTranslator.ParseShipCommands(inmsg.content);
		if(state == ServerState.COMMAND) {
			bool valid = gr.AddPlayersCommands(cmd, co.serverPlayerID);

			if(!valid) {
				//TODO
				// Send msg back to client saying cmd are invalid
			}

			// CHECK IF ALL PLAYER CMDS ARE IN
			// IF SO BEGIN PROCESSING
			if(ReadyToProcess()) {
				state = ServerState.PROCESSING;
				Debug.Log("Beginning to process gamestate");
			}
		}
	}

	bool ReadyToProcess() {
		return gr.CheckIfAllCommandsAreIn();
	}

	void HandleJoin(ConnectionObject co, NetworkingMessage inmsg) {
		bool canJoin = PlayersCanJoin();

		if(canJoin) {
			co.serverPlayerID = playerIDTracker++;
		}

		String outmsg = NetworkingMessageTranslator.GenerateServerAcceptJoinMessage(co.serverPlayerID);

		ch.BeginSend(co, outmsg);

		// Apply game logic...

		// If enough players begin game
		if(CanStartGame()) {
			state = ServerState.SETUP_GAME;
		}
	}

    List<ConnectionObject> GetAllConnectedClients()
    {
        return ch.GetConnectionsList();
    }

    List<ConnectionObject> GetPlayingConnectedClients () {
		List<ConnectionObject> playingClients = new List<ConnectionObject>();

		foreach(ConnectionObject co in ch.GetConnectionsList()) {
			if(co.serverPlayerID > -1) {
				playingClients.Add(co);
			}
		}

		return playingClients;
	}

	int GetNumberOfReadyClients () {
		return GetPlayingConnectedClients().Count;
	}

	void StartGame() {
		state = ServerState.STARTING;
		Debug.Log("Server Starting Game");

		gr.Reset(true);

		// Spawn player ships

		foreach (ConnectionObject co in GetPlayingConnectedClients()) {
			gr.AddPlayer(co.serverPlayerID);
		}

		SendGameState();
	}

	void SendGameState() {
		GameState gs = gr.GetGameState();

		String outmsg = NetworkingMessageTranslator.GenerateGameStateMessage(gs);

		// Send players the location of all ships, env variables, player names, useable moves,
		// usable actions, everything for the game setup, etc
		// and request player move cmd. send turn time

		SendToAllPlayingClients(outmsg);

		// TODO
		// Fix this
		// each client should send "received game state msg" then "animation finished"
		// then server goes to cmd state
		state = ServerState.COMMAND;
	}

    void SendDisconnectToAllClients() {
        String outmsg = NetworkingMessageTranslator.GenerateDisconnectMessage();

        SendToAllClients(outmsg, ch.CloseConnection);
    }

    void SendToAllClients(string msg)
    {
        SendToAllClients(msg, null);
    }

    void SendToAllClients(string msg, ServerOnSendCallback cb) {

        SendToClients(msg, GetAllConnectedClients(), cb);
    }

    void SendToAllPlayingClients(string msg) {
        SendToClients(msg, GetPlayingConnectedClients());
	}

    void SendToClients(string msg, List<ConnectionObject> clients)
    {
        SendToClients(msg, clients, null);
    }

    void SendToClients(string msg, List<ConnectionObject> clients, ServerOnSendCallback cb)
    {
        foreach (ConnectionObject co in clients)
        {
            ch.BeginSend(co, msg, cb);
        }
    }

    bool CanStartGame() {
		if(state == ServerState.WAITING_TO_START && GetNumberOfReadyClients() == numberOfPlayersUntilStart) {
			return true;
		}

		return false;
	}

	bool PlayersCanJoin () {
		if(state == ServerState.WAITING_TO_START) {
			return true;
		} else {
			return false;
		}
	}

	void OnDestroy() {
		ch.CloseAll();
		cl.Close();
	}
}

[Serializable]
public class ServerConnectionHandler {

	public List<ConnectionObject> connections;
	IncomingDataCallback incomingDataCallback;

	public ServerConnectionHandler(IncomingDataCallback incomingDataCallback) {
		connections = new List<ConnectionObject>();
		this.incomingDataCallback = incomingDataCallback;
	}

	public void AddConnection(ConnectionObject newConnection) {
		connections.Add(newConnection);

		newConnection.socket.BeginReceive(newConnection.buffer, 0, ConnectionObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), newConnection);
	}

	private void ReceiveCallback(IAsyncResult ar) {
		String data = String.Empty;

		ConnectionObject sco = (ConnectionObject) ar.AsyncState;
		Socket s = sco.socket;

		int bytesRead = s.EndReceive(ar);

		if(bytesRead > 0) {
			sco.stringBuilder.Append(Encoding.ASCII.GetString(sco.buffer, 0, bytesRead));

			data = sco.stringBuilder.ToString();
			sco.stringBuilder.Length = 0;
		}

		s.BeginReceive(sco.buffer, 0, ConnectionObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), sco);

		if(incomingDataCallback != null) {
			incomingDataCallback(sco, data);
		}
	}

    public void BeginSend(ConnectionObject co, String message) {
        BeginSend(co, message, null);
    }

    public void BeginSend(ConnectionObject co, String message, ServerOnSendCallback cb) {
		byte[] byteData = Encoding.ASCII.GetBytes(message);

		if(byteData.Length > ConnectionObject.BufferSize) {
			Debug.LogWarning("Message length is larger then max buffer size");
		}

        ServerSendObject sso = new ServerSendObject(co, cb);

        co.socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), sso);
	}

	private void SendCallback(IAsyncResult ar) {
        ServerSendObject sso = (ServerSendObject)ar.AsyncState;
        ConnectionObject co = sso.co;
		int bytesSent = co.socket.EndSend(ar);

        sso.cb?.Invoke(co);
    }

	public void CloseAll() {
		foreach(ConnectionObject sco in connections.ToArray()) {
			CloseConnection(sco);
		} 
	}

    bool CheckIfSocketIsConnected(Socket s)
    {
        if(!s.Connected)
        {
            return false;
        }

        bool part1 = s.Poll(1000, SelectMode.SelectRead);
        bool part2 = (s.Available == 0);

        if (part1 && part2)
        {
            return false;
        } else
        {
            return true;
        }
    }

	public void CloseConnection(ConnectionObject co) {
		if(co != null && co.socket != null) {
			if(CheckIfSocketIsConnected(co.socket)) {
				co.socket.Shutdown(SocketShutdown.Send);
			}

			co.socket.Close();
		}

        int removeIndex = -1;
        for(int i = 0; i < connections.Count; i++)
        {
            if(connections[i].serverPlayerID == co.serverPlayerID)
            {
                removeIndex = i;
                break;
            }
        }

        if(removeIndex > -1)
        {
            connections.RemoveAt(removeIndex);
        }
        else
        {
            Debug.LogWarning("Could not remove connection object");
        }
    }

	public List<ConnectionObject> GetConnectionsList() {
		return connections;
	}
}

public class ServerConnectionListener {

	private bool listening;

	private int port;
	private int maxBacklogConnections;

	private Socket listener;
	private IPEndPoint localEndPoint;
	private ServerConnectionObjectCallback onNewConnectionCallback;

	public ServerConnectionListener(int port, int maxBacklogConnections, ServerConnectionObjectCallback onNewConnectionCallback) {  
		this.port = port;
		this.maxBacklogConnections = maxBacklogConnections; 
		localEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), this.port);
		this.onNewConnectionCallback = onNewConnectionCallback;
		listening = false;

		listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);        
	}

	public void StartListening() {

		try {
			
			listener.Bind(localEndPoint);
			listener.Listen(maxBacklogConnections);

			listening = true;

		} catch (Exception e) {
			Debug.LogWarning(e.ToString());

			Debug.LogWarning("Connection listener could not start");
		}

		if(listening) {
			Listen();
		}
	}

	public void Listen() {
		if(listening) {
			Debug.Log("Connection Listener waiting for client connection...");
			listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
		}
	}

	public void AcceptCallback(IAsyncResult ar) {
		Socket listener = (Socket) ar.AsyncState;
		Socket handler = listener.EndAccept(ar);
		handler.NoDelay = true;
		ConnectionObject sco = new ConnectionObject(handler);

		Debug.Log("Listener made connection");

		if(onNewConnectionCallback != null) {
			onNewConnectionCallback(sco);
		}

		Listen();
	}

	public void Close() {
		if(listening) {
			listener.Close();
			Debug.Log("Connection Listener Closed");
		}
	}
}

public class ServerSendObject
{
    public ConnectionObject co;
    public ServerOnSendCallback cb;

    public ServerSendObject (ConnectionObject co, ServerOnSendCallback cb)
    {
        this.co = co;
        this.cb = cb;
    }
}
