using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public delegate void EndTurnCallback (CmdState cmd);

public class PlayerController : MonoBehaviour {

	public int playerID = 0;

    public ClientController clientController;

	private GameObject selectedObject;
	private GameObject hoverObject;

	public List<ShipController> shipControllers;
	public List<ShipCommand> shipCommands;

	public UIController uiController;

	public EndTurnCallback endTurnCallback;

	private bool canUpdateCmds = false;

    private float maxTurnTime = 0.0f;
    private float currentTurnTime = 0.0f;
    private bool enableTurnTimer = false;
    private bool loadUINeeded = false;

    public float defaultShipVisualHeight = 0.0f;
    public float visualShipViaulHeightOffset = 1.5f;

    void Start () {
        clientController = GetComponent<ClientController>();
	}

	void Update () {

        UpdateShipVisualHeight();

        if (enableTurnTimer)
        {
            currentTurnTime += Time.deltaTime;
            uiController.UpdateTurnTimerText(maxTurnTime -  currentTurnTime);

            if (currentTurnTime > maxTurnTime)
            {
                EndTurn();
            }
        }
    }

    private void UpdateShipVisualHeight()
    {
        float calcHeightDiff(float dist)
        {
            return (-1f / (1f + Mathf.Pow((float)Math.E, -1.5f * (0.4f * dist - 3f)))) + 1f;
        }

        foreach (ShipController m in shipControllers)
        {
            float height = defaultShipVisualHeight;
            foreach (ShipController n in shipControllers)
            {
                if (m != n)
                {
                    float sign = m.shipID > n.shipID ? 1 : -1;

                    if(m != null && n != null)
                    {
                        height += sign * visualShipViaulHeightOffset * calcHeightDiff(Vector3.Distance(m.transform.position, n.transform.position));
                    }
                }
            }

            m.UpdateVisualShipLevel(height);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ShowMainMenu()
    {
        uiController.EnableMainMenuUI(true, null);
        uiController.DisableGameUI();
    }

    void EnableLoadUI()
    {
        if(loadUINeeded)
        {
            uiController.EnableLoadUI(true, "Connecting to server...");
        }
    }

    public void ConnectToGame()
    {
        if(clientController.state == ClientState.IDLE)
        {
            uiController.EnableMainMenuUI(false, EnableLoadUI);
            clientController.Connect();
            loadUINeeded = true;
        }
    }

    public void OnFailedToConnect()
    {
        ShowMainMenu();
        uiController.EnableLoadUI(false, "");
        loadUINeeded = false;
    }

    public void OnConnected()
    {
        uiController.EnableLoadUI(false, "");
        loadUINeeded = false;
    }

    public void SetTurnTimer(float maxTurnTime)
    {
        this.maxTurnTime = maxTurnTime;
        enableTurnTimer = false;
        currentTurnTime = 0.0f;
    }

    public void BeginTurnTimer()
    {
        enableTurnTimer = true;
        currentTurnTime = 0.0f;

        uiController.EnableEndUI(true);
        uiController.EnableTurnTimerText(true);
        uiController.UpdateTurnTimerText(maxTurnTime - currentTurnTime);
    }

    public void NewTurn(List<ShipController> shipControllers) {
		this.shipControllers = shipControllers;
		PopulateShipCommands();
		canUpdateCmds = true;

        BeginTurnTimer();
	}

	private void PopulateShipCommands() {
		shipCommands = new List<ShipCommand>();

		foreach(ShipController ship in shipControllers) {
			shipCommands.Add(new ShipCommand(ship));
			ship.UpdateMovementPath();
			ship.UpdateActionVisual();
		}
	}

	public void EndTurn() {
		// Send ship commands to server

		if(!canUpdateCmds) {
			return;
		}

        uiController.EnableEndUI(false);

        enableTurnTimer = false;
        uiController.EnableTurnTimerText(false);

        List<ShipCmdState> myCommands = new List<ShipCmdState>();

		foreach(ShipCommand c in shipCommands) {
			if(GetShipController(c.shipID).shipOwner == playerID) {
				myCommands.Add(c.GetShipCmdState());
			}
		}

		CmdState cmd = new CmdState(myCommands);
		canUpdateCmds = false;
		if(endTurnCallback != null) {
			endTurnCallback(cmd);
		} else {
			Debug.LogWarning("End turn callback was null...");
		}
	}

	public ShipController GetShipController(int shipID) {
		return shipControllers.Find(x => x.shipID == shipID);
	}

	public ShipCommand GetShipCommand(int shipID) {
		return shipCommands.Find(x => x.shipID == shipID);
	}

	public ShipCommand GetShipCommand(ShipController sc) {
		return GetShipCommand(sc.shipID);
	}

    public GameObject GetSelectedObject() {
        return selectedObject;
    }

    public void UpdateInfoUI()
    {
        if(selectedObject != null) {
			ShipController shipController = selectedObject.GetComponent<ShipController>();

			if (shipController != null ) {
				uiController.UpdateInfoUI(shipController.shipName, shipController.health.ToString(), shipController.rocketCount.ToString(), shipController.energy.ToString());
			}
		}
    }

	public void SelectObject(GameObject obj) {
		DeselectObject();

		selectedObject = obj;

		if(selectedObject != null) {
			ShipController shipController = selectedObject.GetComponent<ShipController>();

			if (shipController != null ) {
                UpdateInfoUI();
                shipController.Select();
                uiController.EnableInfoUI(true);
				if(shipController.shipOwner == playerID) {
                    uiController.EnableCommandUI(true);
					UpdateCommandUI();
				}
			}
		}
	}

	public void HoverObject(GameObject obj) {
		hoverObject = obj;
	}

	public void DeselectObject() {

		if(selectedObject != null) {
			ShipController shipController = selectedObject.GetComponent<ShipController>();

			if (shipController != null ) {
				uiController.UpdateInfoUI("-", "-", "-", "-");
                // HIDE INFO UI
                uiController.EnableInfoUI(false);
				shipController.Deselect();
				if(shipController.shipOwner == playerID) {
                    // HIDE COMMAND UI
                    uiController.EnableCommandUI(false);
				}
			}
		}

		selectedObject = null;	
	}

	public void UpdateCommandUI () {
		if(selectedObject != null) {
			ShipController shipController = selectedObject.GetComponent<ShipController>();

			if (shipController != null && shipController.shipOwner == playerID) {
				ShipCommand shipCommand = GetShipCommand(shipController);
				uiController.UpdateCommandUI(shipCommand.shipMove.name, GlobalShipActions.GetGlobalActionIndex(shipCommand.shipAction));
			}
		}
	}

	public void SetSelectedShipAction (int actionID) {
		if(selectedObject != null  && canUpdateCmds) {
			ShipController shipController = selectedObject.GetComponent<ShipController>();

			if (shipController != null && shipController.shipOwner == playerID) {
				ShipCommand shipCommand = GetShipCommand(shipController);

				List<ShipAction> shipActions = new List<ShipAction>(GlobalShipActions.ShipActions());

				if (shipActions.Count > actionID && 0 <= actionID) {
					shipCommand.shipAction = shipActions[actionID];
					shipController.UpdateActionVisual();
				}
			}
		}

		UpdateCommandUI();
	}

	public void SetSelectedShipMove (int moveID) {
		if(selectedObject != null && canUpdateCmds) {
			ShipController shipController = selectedObject.GetComponent<ShipController>();

			if (shipController != null && shipController.shipOwner == playerID) {
				ShipCommand shipCommand = GetShipCommand(shipController);

				List<ShipMove> shipMoves = new List<ShipMove>(GlobalShipMoves.ShipMoves());

				if (shipMoves.Count > moveID && 0 <= moveID) {
					shipCommand.shipMove = shipMoves[moveID];
					shipController.UpdateMovementPath(); 
				}
			}
		}

		UpdateCommandUI();
	}
		
	public void IncreaseSelectedShipAction () {
		if(selectedObject != null) {
			ShipController shipController = selectedObject.GetComponent<ShipController>();

			if (shipController != null && shipController.shipOwner == playerID) {
				ShipCommand shipCommand = GetShipCommand(shipController);

				List<ShipAction> shipActions = new List<ShipAction>(GlobalShipActions.ShipActions());

				int currentActionId = shipActions.FindIndex(x => x.name == shipCommand.shipAction.name);

				int newActionId = currentActionId + 1;

				if(shipActions.Count <= newActionId) {
					newActionId = 0;
				}

				SetSelectedShipAction(newActionId);
			}
		}
	}

	public void DecreaseSelectedShipAction () {
		if(selectedObject != null) {
			ShipController shipController = selectedObject.GetComponent<ShipController>();

			if (shipController != null && shipController.shipOwner == playerID) {
				ShipCommand shipCommand = GetShipCommand(shipController);

				List<ShipAction> shipActions = new List<ShipAction>(GlobalShipActions.ShipActions());

				int currentActionId = shipActions.FindIndex(x => x.name == shipCommand.shipAction.name);

				int newActionId = currentActionId - 1;

				if(newActionId < 0) {
					newActionId = shipActions.Count - 1;
				}

				SetSelectedShipAction(newActionId);
			}
		}
	}

	public void IncreaseSelectedShipMove () {
		if(selectedObject != null) {
			ShipController shipController = selectedObject.GetComponent<ShipController>();

			if (shipController != null && shipController.shipOwner == playerID) {
				ShipCommand shipCommand = GetShipCommand(shipController);

				List<ShipMove> shipMoves = new List<ShipMove>(GlobalShipMoves.ShipMoves());

				int currentMoveId = shipMoves.FindIndex(x => x.name == shipCommand.shipMove.name);

				int newMoveId = currentMoveId + 1;

				if(shipMoves.Count <= newMoveId) {
					newMoveId = 0;
				}

				SetSelectedShipMove(newMoveId);
			}
		}
	}

	public void DecreaseSelectedShipMove () {
		if(selectedObject != null) {
			ShipController shipController = selectedObject.GetComponent<ShipController>();

			if (shipController != null && shipController.shipOwner == playerID) {
				ShipCommand shipCommand = GetShipCommand(shipController);

				List<ShipMove> shipMoves = new List<ShipMove>(GlobalShipMoves.ShipMoves());

				int currentMoveId = shipMoves.FindIndex(x => x.name == shipCommand.shipMove.name);

				int newMoveId = currentMoveId - 1;

				if(newMoveId < 0) {
					newMoveId = shipMoves.Count -1;
				}

				SetSelectedShipMove(newMoveId);
			}
		}
	}
}
