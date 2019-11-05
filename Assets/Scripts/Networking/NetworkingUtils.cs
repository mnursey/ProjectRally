using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

[Serializable]
public class ConnectionObject {
	public Socket socket = null;
	public const int BufferSize = 2048;
	public byte[] buffer = new byte[BufferSize];
	public StringBuilder stringBuilder = new StringBuilder();
	public int serverPlayerID = -1;
    public volatile bool socketConnected = false;

	public ConnectionObject(Socket socket, int serverPlayerID) {
		this.socket = socket;
		this.serverPlayerID = serverPlayerID;
	}

	public ConnectionObject(Socket socket) {
		this.socket = socket;
	}
}

public static class NetworkingMessageTranslator {

	private static string ToJson(object obj) {
		return JsonUtility.ToJson(obj);
	}

	public static string GenerateDisconnectMessage() {
		NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.DISCONNECT);
		return ToJson(msg);
	}

	public static string GenerateGameStateMessage(GameState gs) {
		NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.GAME_STATE);
		msg.content = ToJson(gs);
		return ToJson(msg);
	}

	public static string GeneratePlayerCommandsMessage(CmdState cmd) {
		NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.CMDS);
		msg.content = ToJson(cmd);
		return ToJson(msg);
	}

	public static string GenerateClientJoinMessage() {
		NetworkingMessage msg = new NetworkingMessage(NetworkingMessageType.CLIENT_JOIN);
		return ToJson(msg);
	}

	public static string GenerateServerAcceptJoinMessage(int playerID) {

		NetworkingMessageType msgType = NetworkingMessageType.SERVER_JOIN_RESPONSE;

		NetworkingMessage msg = new NetworkingMessage(msgType, playerID.ToString());
		return ToJson(msg);
	}

	public static NetworkingMessage ParseMessage(string json) {
		return JsonUtility.FromJson<NetworkingMessage>(json);
	}

	public static NetworkingMessageStatus ParseInfo(string json) {
		return JsonUtility.FromJson<NetworkingMessageStatus>(json);
	}

	public static GameState ParseGameState(string json) {
		return JsonUtility.FromJson<GameState>(json);
	}

	public static CmdState ParseShipCommands(string json) {
		return JsonUtility.FromJson<CmdState>(json);
	}
}

[Serializable]
public enum NetworkingMessageStatus {ACCEPT, DECLINE, WAIT};

[Serializable]
public enum NetworkingMessageType {CLIENT_JOIN, SERVER_JOIN_RESPONSE, DISCONNECT, GAME_STATE, CMDS};

[Serializable]
public class NetworkingMessage {
	public NetworkingMessageType type;
	public string content;

	public NetworkingMessage () {

	}

	public NetworkingMessage (NetworkingMessageType type) {
		this.type = type;
	}

	public NetworkingMessage (NetworkingMessageType type, string content) {
		this.type = type;
		this.content = content;
	}
}