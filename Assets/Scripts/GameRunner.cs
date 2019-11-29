using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Threading;

public class GameRunner : MonoBehaviour {

	List<GameObject> gamePieces;
	List<ShipController> shipControllers;
	List<RocketController> rocketControllers;
    public List<TextAsset> standardVoxelSpaceShipModels = new List<TextAsset>();
	[SerializeField]
	List<ShipCommand> shipCommands;

	public GameObject shipPrefab;


    bool gameOver = false;
    int winner = -1;

    int turn = 0;
	int shipIDTracker = 0;
    int playerID = -1;

    public float squareBoundXPos = 24f;
    public float squareBoundXNeg = -32f;
    public float squareBoundZPos = 24f;
    public float squareBoundZNeg = -32;

    float turnTimer = 30f;
	float simDelta = 0.01f;

	float simTime = 0.0f;

	bool serverMode = false;

	float rocketToRocketCollisionDistance = 0.5f;
	float rocketToShipCollisionDistance = 1.5f;
	float shipToShipCollisionDistance = 1.5f;

	public GameRunner() {
		Reset(false);
	}

    public void Reset(int playerID)
    {
        RemoveGameObjects();

        gamePieces = new List<GameObject>();
        shipControllers = new List<ShipController>();
        rocketControllers = new List<RocketController>();
        shipCommands = new List<ShipCommand>();
        this.serverMode = false;
        turn = 0;
        shipIDTracker = 0;
        gameOver = false;
        winner = -1;
        this.playerID = playerID;
    }

    public void Reset(bool serverMode) {

        RemoveGameObjects();

        gamePieces = new List<GameObject>();
		shipControllers = new List<ShipController>();
		rocketControllers = new List<RocketController>();
		shipCommands = new List<ShipCommand>();
		this.serverMode = serverMode;
		turn = 0;
		shipIDTracker = 0;
        playerID = -1;
        gameOver = false;
        winner = -1;
    }

    private void RemoveGameObjects()
    {
        if (gamePieces != null)
        {
            foreach(GameObject g in gamePieces.ToArray())
            {
                Destroy(g);
            }
        }

        if (shipControllers != null)
        {
            foreach (ShipController s in shipControllers.ToArray())
            {
                Destroy(s.gameObject);
            }
        }

        if (rocketControllers != null)
        {
            foreach (RocketController r in rocketControllers.ToArray())
            {
                Destroy(r.gameObject);
            }
        }
    }

	private Vector3 GetOpenSpawn() {

        // TODO
		// FIX THIS!
		Vector3 pos = new Vector3(-15, 3, 0);
		bool solved = false;

		while(!solved) {
			solved = true;
			for(int i = 0; i < shipControllers.Count; ++i) {
				if (Vector3.Distance(shipControllers[i].transform.position, pos) < 25.0f) {
					pos.x = pos.x + 5.0f;
					solved = false;
					break;
				}
			}
		}

		return pos;
	}

	public void AddPlayer (int playerID) {
		// Create player. Create player spawn. Create 3 ships for player at players spawn with owner set to player
		Vector3 spawn = GetOpenSpawn();

		for(int i = 0; i < 3; ++i) {
			Vector3 shipSpawnPos = new Vector3(spawn.x, spawn.y, spawn.z + i * 3.0f);
			// Create ship at spawn
			AddShip(playerID, shipSpawnPos);
		}
	}

	public void RemovePlayer () {
		// TODO
	}

	private ShipController AddShip(int ownerID, Vector3 spawnPosition) {
		return AddShip(ownerID, spawnPosition, shipIDTracker++);
	}

	private ShipController AddShip(int ownerID, Vector3 spawnPosition, int shipID) {
		GameObject sObj = (GameObject)Instantiate(shipPrefab, spawnPosition, new Quaternion());

		ShipController sc = sObj.GetComponent<ShipController>();
        Chunk spaceShipVisualChunk = sc.visualHolder.GetComponentInChildren<Chunk>();

        // TODO
        // Update to set function
        spaceShipVisualChunk.standardVoxelModel = standardVoxelSpaceShipModels[ownerID % standardVoxelSpaceShipModels.Count];
        spaceShipVisualChunk.LoadChunk();
        spaceShipVisualChunk.GenerateChunk();

		if(serverMode) {

            foreach (MeshCollider c in sObj.GetComponentsInChildren<MeshCollider>())
            {
                c.enabled = false;
            }
		}

		shipControllers.Add(sc);
		sc.shipOwner = ownerID;
		sc.shipID = shipID;

		if(shipIDTracker <= shipID) {
			shipIDTracker = shipID + 1;
		}

        if(playerID > -1 && sc.shipOwner != playerID)
        {
            //sc.GetComponent<Renderer>().material.color = Color.red;
        }

        if(serverMode)
        {
            foreach(Renderer r in sc.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }
        }

		gamePieces.Add(sObj);

		return sc;
	}

	public void SetGameState(GameState state) {

		List<ShipState> ss = state.ships;

		// CHECK IF SHIP EXISTS
		// IF NOT CREATE SHIP
		// UPDATE SHIP STATE

		foreach(ShipState s in ss) {
			ShipController sc = GetShipController(s.shipID);

			if(sc == null) {
				// CREATE SHIP

				sc = AddShip(s.owner, s.position, s.shipID);
			}

			sc.SetShipState(s);
		}

        foreach(ShipController sc in shipControllers.ToArray())
        {
            bool exists = false;
            foreach(ShipState s in ss)
            {
                if(s.shipID == sc.shipID)
                {
                    exists = true;
                    break;
                }
            }

            if(!exists)
            {
                ExplodeShip(sc);
            }
        }

		turn = state.turn;
		turnTimer = state.turnTimer;

		// SET CMDS
		// TODO
	}

	public void SetCommands (List<ShipCmdState> cmds) {
		shipCommands = new List<ShipCommand>();

		foreach(ShipCmdState cmd in cmds) {
			shipCommands.Add(new ShipCommand(cmd));
		}
	}

	public bool AddPlayersCommands(CmdState cmd, int playerID) {
		bool valid = true;

		foreach(ShipCmdState c in cmd.cmds) {
			// TODO
			// CHECK IF SHIP EXISTS
			ShipController sc = GetShipController(c.shipID);

			if(sc.shipOwner == playerID) {
				// TODO
				// CHECK IF VALID SHIP CMD

				// CHECK IF CMDS FOR SHIP ALREADY EXIST
				if (GetShipCommand(sc.shipID) != null) {
					valid = false;
					Debug.LogWarning("Command already set for this ship");
				}

				// SHIP ALIVE?
				// SHIP HAVE ENOUGH ENERGY?
				// CAN SHIP PERFORM ACTION?
				// CAN SHIP PERFORM MOVE?
				shipCommands.Add(new ShipCommand(c));
			} else {
				valid = false;

				Debug.LogWarning("Invalid ship owner");
			}
		}

		return valid;
	}

	public bool CheckIfAllCommandsAreIn() {
		foreach(ShipController sc in shipControllers) {
			ShipCommand c = GetShipCommand(sc.shipID);

			if(c == null) {
				return false;
			}
		}

		return true;
	}

	public void NewTurn() {
		shipCommands = new List<ShipCommand>();
	}

	public bool StepThroughSim () {

		bool finished = true;

		if(simTime == 0.0f) {
			// Setup ship commands

			foreach(ShipController sc in shipControllers) {
				ShipCommand cmd = GetShipCommand(sc.shipID);
				ShipAction action = cmd.shipAction;

                sc.BeginSimPhase();

                // Validate ships have enough energy
                // Subtract energies
                switch (action.type) {
				case ShipActionType.ENERGY:
					break;

				case ShipActionType.SHIELD:
					if(sc.energy >= 3) {
						sc.energy += -3;
					} else {
						action.complete = true;
					}
					break;

				case ShipActionType.SHOOT:
					if(sc.energy >= 2) {
						sc.energy += -2;
                        
                        sc.SetupSimArcVisual();
					} else {
						action.complete = true;
					}
					break;
				}

                sc.SetShipMove(cmd.shipMove);
            }
        }

		simTime += simDelta;

		// Move ships
		foreach(ShipController sc in shipControllers) {
			bool doneMove = sc.MoveShip(simDelta);

			if(!doneMove) {
				finished = false;
			}
		}

		// TODO
		// Check if ships collide

		// Check if ships can Action
			// if shoot check if enemy ship in sight
				// if so spawn rocket
			// if energy add energy
			// if shield activate shield


		foreach(ShipController sc in shipControllers) {
			ShipCommand cmd = GetShipCommand(sc.shipID);
			ShipAction action = cmd.shipAction;

			if(!action.complete) {
				switch(action.type) {
				case ShipActionType.ENERGY:
					sc.BoostEnergy(3);
					action.complete = true;
					break;

				case ShipActionType.SHIELD:
					sc.ActivateShield();
					action.complete = true;
					break;

				case ShipActionType.SHOOT:

					ShipController target = sc.GetClosestShipInRangeOfRocket(shipControllers);
					if(target != null) {
						RocketController r = sc.FireRocket(target);

						if(r != null) {
							action.complete = true;
							rocketControllers.Add(r);
							gamePieces.Add(r.gameObject);
						}
					}

					break;
				}
			}
		}

		// Move rockets
		// Check if rocket hit anything
		// If other ship damgage other ship
		// If other rocket explode other rocket
		// If so explode rocket
		foreach(RocketController rocket in rocketControllers.ToArray()) {
			bool explode = rocket.MoveTowardsTarget(simDelta);

			foreach(GameObject gp in gamePieces.ToArray()) {
				{
					ShipController otherShip = gp.GetComponent<ShipController>();

					if(otherShip != null) {
						if(Vector3.Distance(rocket.transform.position, otherShip.transform.position) < rocketToShipCollisionDistance) {
							if(rocket.orginShip != otherShip) {
								otherShip.TakeDamage(1, false);
								explode = true;
							}
						}
					}
				}
				{
					RocketController otherRocket = gp.GetComponent<RocketController>();

					if(otherRocket != null) {
						if(Vector3.Distance(rocket.transform.position, otherRocket.transform.position) < rocketToRocketCollisionDistance) {
							if(rocket != otherRocket) {
								ExplodeRocket(otherRocket);
								explode = true;
							}
						}
					}
				}
			}
				
			if(explode) {
				ExplodeRocket(rocket);
			}
		}

		// Check if rockets are still in play
		if(rocketControllers.Count > 0) {
			finished = false;
		}
			
		if(finished) {

			foreach(ShipController sc in shipControllers) {

                // Check if ship is out of bounds
                // Take damage if ship is out of bounds

                if(sc.transform.position.x > squareBoundXPos || sc.transform.position.x < squareBoundXNeg || sc.transform.position.z > squareBoundZPos || sc.transform.position.z < squareBoundZNeg)
                {
                    sc.TakeDamage(1, true);
                }

                sc.EndSimPhase();
			}

			turn++;
			simTime = 0.0f;
		}

        List<int> activePlayerIDs = new List<int>();
        foreach (ShipController sc in shipControllers.ToArray())
        {
            if (sc.IsAlive())
            {
                int shipOwner = sc.shipOwner;
                if (!activePlayerIDs.Contains(shipOwner))
                {
                    activePlayerIDs.Add(shipOwner);
                }
            }
            else
            {
                // Remove ship from game
                ExplodeShip(sc);
            }
        }

        // Check if player has won
        if (activePlayerIDs.Count < 2)
        {
            if(activePlayerIDs.Count == 1)
            {
                winner = activePlayerIDs[0];
            }
            
            if(turn - 1 != 0 && finished)
            {
                gameOver = true;
            }
        }

        return finished;
	}

    public bool IsGameFinished()
    {
        return gameOver;
    }

    public int GetWinner()
    {
        return winner;
    }

	private void ExplodeRocket(RocketController rocket) {
        if(!serverMode)
        {
            rocket.Explode();
        }

        rocketControllers.Remove(rocket);
		gamePieces.Remove(rocket.gameObject);
		Destroy(rocket.gameObject);
	}

    private void ExplodeShip(ShipController sc)
    {
        shipControllers.Remove(sc);
        gamePieces.Remove(sc.gameObject);
        sc.ExplodeShip(serverMode);
    }

	public List<ShipController> GetShipControllers() {
		return shipControllers;
	}

	public ShipController GetShipController (int shipID) {
		if(shipControllers.Exists(x => x.shipID == shipID)) {
			return shipControllers.Find(x => x.shipID == shipID);
		} else {
			return null;
		}
	}

	public ShipCommand GetShipCommand (int shipID) {
		if(shipCommands.Exists(x => x.shipID == shipID)) {
			return shipCommands.Find(x => x.shipID == shipID);
		} else {
			return null;
		}
	}

	public GameState GetGameState() {
		GameState gs = new GameState();
		gs.turn = turn;
		gs.turnTimer = 45.0f;
		gs.ships = new List<ShipState>();
		gs.cmds = new List<ShipCmdState>();
        gs.winner = GetWinner();

		foreach(ShipCommand cmd in shipCommands) {
			gs.cmds.Add(cmd.GetShipCmdState());
		}

		foreach(ShipController sc in shipControllers) {
			gs.ships.Add(ShipControllerToShipState(sc));
		}

		return gs;
	}

	private ShipState ShipControllerToShipState(ShipController sc) {
		ShipState ss = new ShipState();

		ss.owner = sc.shipOwner;
		ss.shipID = sc.shipID;
		ss.health = sc.health;
		ss.maxHealth = sc.maxHealth;
		ss.energy = sc.energy;
		ss.maxEnergy = sc.maxEnergy;
		ss.position = sc.transform.position;
		ss.rotation = sc.transform.eulerAngles;

		return ss;
	} 

	public int GetTurn () {
		return turn;
	}
}

[Serializable]
public class GameState {
	public int turn;
	public float turnTimer;
	public List<ShipState> ships;
	public List<ShipCmdState> cmds;
    public int winner = -1;
}

[Serializable]
public class ShipState {
	public int owner;
	public int shipID;
	public int health;
	public int energy;
	public int maxHealth;
	public int maxEnergy;
	public Vector3 position;
	public Vector3 rotation;
}

[Serializable]
public class ShipCmdState {
	public int moveID;
	public int actionID;
	public int shipID;

	public ShipCmdState(int moveID, int actionID, int shipID) {
		this.moveID = moveID;
		this.actionID = actionID;
		this.shipID = shipID;
	}
}

[Serializable]
public class CmdState { 
	public List<ShipCmdState> cmds;

	public CmdState (List<ShipCmdState> cmds) {
		this.cmds = cmds;
	}
}