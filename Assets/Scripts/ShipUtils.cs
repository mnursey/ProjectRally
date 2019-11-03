using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public enum ShipStateEnum {IDLE, MOVING, ACTION};

public static class BezierCurve {
	public static Vector3 Curve(List<Vector3> points, float t){
		
		if(points.Count == 1){
			return points[0];
		} else {
			
			List<Vector3> temp = new List<Vector3>(points);
			temp.RemoveAt(temp.Count - 1);
			Vector3 P1 = Curve(temp, t);

			temp = new List<Vector3>(points);
			temp.RemoveAt(0);
			Vector3 P2 = Curve(temp, t);

			Vector3 P = ((1.0f - t) * P1) + (t * P2);

			return P;
		}
	}

	public static float Delta() {
		return 0.0001f;
	}
}

public static class GlobalShipMoves {

	public static ShipMove[] ShipMoves() {
		ShipMove[] moves = new ShipMove[]{
			new ShipMove("Forward",		new Vector2[]{new Vector2(0, 8)}),
			new ShipMove("Soft Right", 	new Vector2[]{new Vector2(0, 3), new Vector2(4, 7)}),
			new ShipMove("Hard Right", 	new Vector2[]{new Vector2(0, 4), new Vector2(6, 4)}),
			new ShipMove("Hard Left",	new Vector2[]{new Vector2(0, 4), new Vector2(-6, 4)}),
			new ShipMove("Soft Left",	new Vector2[]{new Vector2(0, 3), new Vector2(-4, 7)}),
		};


		return moves;
	}

	public static int GetGlobalMoveIndex(ShipMove move) {
		List<ShipMove> moves = new List<ShipMove>(ShipMoves());
		return moves.FindIndex(x => x.name == move.name);
	}
}

[Serializable]
public class ShipMove {
	public String name;
	public List<Vector2> waypoints;

	public ShipMove(String name, Vector2[] positions){
		this.name = name;
		this.waypoints = new List<Vector2>(positions);
	}

	// TODO
	public bool ValidateMove(){
		return true;
	}
}

[Serializable]
public enum ShipActionType {SHOOT, SHIELD, ENERGY};

// This should match GlobalShipActions ShipActions
public enum GlobelShipActionsEnums {BasicEnergy, BasicRocket, BasicShield}

public static class GlobalShipActions {
	public static ShipAction[] ShipActions(){
		ShipAction[] actions = new ShipAction[] {
			new ShipAction("Basic Energy", ShipActionType.ENERGY),
			new ShipAction("Basic Rocket", ShipActionType.SHOOT),
			new ShipAction("Basic Shield", ShipActionType.SHIELD),
		};

		return actions;
	}

	public static int GetGlobalActionIndex(ShipAction action) {
		List<ShipAction> actions = new List<ShipAction>(ShipActions());
		return actions.FindIndex(x => x.name == action.name);
	}
}

[Serializable]
public class ShipAction {
	public String name;
	public ShipActionType type;
	public bool complete = false;

	public ShipAction(String name, ShipActionType type){
		this.name = name;
		this.type = type;
	}
}

[Serializable]
public class ShipCommand {

	public ShipMove shipMove;
	public ShipAction shipAction;
	public int shipID;

	public ShipCommand (ShipController sc) {
		this.shipID = sc.shipID;
		shipMove = GlobalShipMoves.ShipMoves()[0];
		shipAction = GlobalShipActions.ShipActions()[0];
	}

	public ShipCommand (int shipID) {
		this.shipID = shipID;
		shipMove = GlobalShipMoves.ShipMoves()[0];
		shipAction = GlobalShipActions.ShipActions()[0];
	}

	public ShipCommand(int shipID, int moveID, int actionID) {
		this.shipID = shipID;
		shipMove = GlobalShipMoves.ShipMoves()[moveID];
		shipAction = GlobalShipActions.ShipActions()[actionID];
	}

	public ShipCommand(ShipCmdState cmd) {
		this.shipID = cmd.shipID;
		shipMove = GlobalShipMoves.ShipMoves()[cmd.moveID];
		shipAction = GlobalShipActions.ShipActions()[cmd.actionID];
	}

	public ShipCmdState GetShipCmdState() {

		int moveID = GlobalShipMoves.GetGlobalMoveIndex(shipMove);
		int actionID = GlobalShipActions.GetGlobalActionIndex(shipAction);

		return new ShipCmdState(moveID, actionID, shipID);
	}
}