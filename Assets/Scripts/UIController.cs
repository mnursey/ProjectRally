using UnityEngine;
using System.Collections;
using UnityEngine.UI; 
using System;
using System.Collections.Generic;
using TMPro;

public class UIController : MonoBehaviour {

	public Text infoShipName;
	public Text infoShipHealth;
	public Text infoShipRockets;
	public Text infoShipEnergy;

	public Image EnergyActionSelect;
	public Image RocketActionSelect;
	public Image ShieldActionSelect;
	public Text directionText;
    public Text winText;
    public Text turnTimerText;

    public GameObject commandUI;
    public GameObject infoUI;
    public GameObject endUI;
    public GameObject menuUI;
    public GameObject loadUI;

    public Color selectedColor = Color.yellow;
	private Color defaultColor;

	public PlayerController playerController;
    public bool clickedUIThisFrame = false;

	// Use this for initialization
	void Start () {
        // TODO fix this
		// This is hacky
		defaultColor = EnergyActionSelect.color;
	}
	
	void FixedUpdate () {
        clickedUIThisFrame = false;
    }

    public void UpdateCommandUI(string directionName, int selectedAction) {
		// This should match Ship Utils
		IDictionary<int, Image> actionIdButtonMap = new Dictionary<int, Image>(){
			{(int)GlobelShipActionsEnums.BasicEnergy, EnergyActionSelect},
			{(int)GlobelShipActionsEnums.BasicRocket, RocketActionSelect},
			{(int)GlobelShipActionsEnums.BasicShield, ShieldActionSelect}
		};

		foreach (Image b in actionIdButtonMap.Values) {
			if(b != actionIdButtonMap[selectedAction]) {
				b.color = defaultColor;
				UpdatePanelColor(b, defaultColor);
			}
		}

		UpdatePanelColor(actionIdButtonMap[selectedAction], selectedColor);
		actionIdButtonMap[selectedAction].color = selectedColor;
		directionText.text = directionName;
	}

	private void UpdatePanelColor(Image b, Color c) {
        Color cHat = new Color(c.r, c.g, c.b, b.color.a);
        b.color = cHat;
	}

	public void UpdateInfoUI(string shipName, string shipHealth, string shipRockets, string shipEnergy) {
		infoShipName.text = shipName;
		infoShipHealth.text = shipHealth;
		infoShipRockets.text = shipRockets;
		infoShipEnergy.text = shipEnergy;
	}

	public void LeftMoveButtonDown () {
		playerController.DecreaseSelectedShipMove();
        clickedUIThisFrame = true;

    }

	public void RightMoveButtonDown () {
		playerController.IncreaseSelectedShipMove();
        clickedUIThisFrame = true;
    }

    public void SelectEnergy() {
		playerController.SetSelectedShipAction((int)GlobelShipActionsEnums.BasicEnergy);
        clickedUIThisFrame = true;
    }

    public void SelectRocket() {
		playerController.SetSelectedShipAction((int)GlobelShipActionsEnums.BasicRocket);
        clickedUIThisFrame = true;
    }

    public void SelectShield() {
		playerController.SetSelectedShipAction((int)GlobelShipActionsEnums.BasicShield);
        clickedUIThisFrame = true;
    }

    public void EnableWinText(bool enable)
    {
        winText.enabled = enable;
    }

    public void EnableLoadUI(bool enable, string text)
    {
        loadUI.SetActive(enable);
        loadUI.GetComponentInChildren<TextMeshProUGUI>().text = text;
    }

    public void EnableWinText(bool enable, bool won) { 

        if(won)
        {
            winText.text = "Solid Performance";
        } else
        {
            winText.text = "Disgraceful ";
        }

        winText.enabled = enable;
    }

    public void UpdateTurnTimerText(float time)
    {
        string txt = ((int)time) + "s";

        if(time < 10.0f)
        {
            txt += "!";

            if(time < 5.0f)
            {
                txt += "!";
            }

            if (time <= 3.0f)
            {
                txt += "!";
            }
        }

        turnTimerText.text = txt;
    }

    public void DisableGameUI()
    {
        EnableTurnTimerText(false);
        EnableInfoUI(false);
        EnableCommandUI(false);
        EnableEndUI(false);
        EnableWinText(false);
    }

    public void EnableTurnTimerText(bool enable)
    {
        turnTimerText.enabled = enable;
    }

    public void EnableInfoUI(bool enable)
    {
        //Debug.Log("Info " + enable.ToString());

        if (enable)
        {
            infoUI.SetActive(enable);
            infoUI.GetComponent<Animator>().SetTrigger("Enter");
            infoUI.GetComponent<Animator>().ResetTrigger("Exit");
        }
        else
        {
            infoUI.GetComponent<Animator>().SetTrigger("Exit");
            infoUI.GetComponent<Animator>().ResetTrigger("Enter");
        }
    }

    public void EnableCommandUI(bool enable)
    {
        //Debug.Log("Cmd " + enable.ToString());

        if (enable)
        {
            commandUI.SetActive(enable);
            commandUI.GetComponent<Animator>().SetTrigger("Enter");
            commandUI.GetComponent<Animator>().ResetTrigger("Exit");
        }
        else
        {
            commandUI.GetComponent<Animator>().SetTrigger("Exit");
            commandUI.GetComponent<Animator>().ResetTrigger("Enter");
        }
    }

    public void EnableEndUI(bool enable)
    {
        if (enable)
        {
            endUI.SetActive(enable);
            endUI.GetComponent<Animator>().SetTrigger("Enter");
            endUI.GetComponent<Animator>().ResetTrigger("Exit");
        }
        else
        {
            endUI.GetComponent<Animator>().SetTrigger("Exit");
            endUI.GetComponent<Animator>().ResetTrigger("Enter");
        }
    }

    public void EnableMainMenuUI(bool enable, AnimCallback cb)
    {
        menuUI.GetComponent<AnimationUtils>().cb = cb;

        if(enable)
        {
            menuUI.SetActive(enable);
        } else
        {
            menuUI.GetComponent<Animator>().SetTrigger("Exit");
        }
    }

    public void FindGame()
    {
        playerController.ConnectToGame();
        clickedUIThisFrame = true;
    }

    public void QuitGame()
    {
        playerController.QuitGame();
        clickedUIThisFrame = true;
    }

    public void EndTurn()
    {
        playerController.EndTurn();
        clickedUIThisFrame = true;
    }
}
