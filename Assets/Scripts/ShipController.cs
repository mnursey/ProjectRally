using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;

public class ShipController : MonoBehaviour {

	public int shipOwner = -1;
	public int shipID;
	public ShipStateEnum state;

	private float actionPercent;
	public float actionSpeed;
	public GameObject actionObject;

	private float movePercent;
	public float moveSpeed;
	List<Vector3> pathPoints;


	public GameObject RocketPrefab;
	public Transform shipGun;
	public Transform rocketAimPoint;
	public GameObject rocket;
	public PlayerController playerController;

	public int maxHealth = 3;
	public int maxEnergy = 8;

	public string shipName = "";
	public int health = 3;
	public int rocketCount = 3;
	public int energy = 5;
	bool selected = false;

	private bool shieldActive = false;
    private bool simMode = false;
    private bool hasExploded = false;
    public GameObject explosionPrefab;

    public float aimArc;
	public float rocketMinDistance;
	public float rocketMaxDistance;

	public VisualCircleController selectionCircleController;
	public PathSelectionController pathSelectionController;
	public ArcViewController arcViewController;
	public ShieldVisualController shieldVisualController;
	public VisualTriangleController energyVisualController;

    public ParticleSystem shieldEffect;
    public ParticleSystem energyEffect;

    public GameObject visualHolder;

    bool clicked = false;

	void Start () {
		state = ShipStateEnum.IDLE;
		movePercent = 0.0f;

	}

	void Awake () {
		playerController = FindObjectOfType<PlayerController>();

        foreach(Chunk c in GetComponentsInChildren<Chunk>())
        {
            c.onClickCallback = SetClicked;
        }
	}

    void Update()
    {
        CheckDeselect();

        if(clicked)
        {
            Clicked();
            clicked = false;
        }
    }

    public void SetClicked()
    {
        clicked = true;
    }

    public void SetShipMove(ShipMove move) {
		movePercent = 0.0f;

		pathPoints = new List<Vector3>();

		pathPoints.Add(transform.position);

		foreach(Vector2 w in move.waypoints) {
			Vector3 point = new Vector3(w.x, 0, w.y);
			Vector3 adjustWaypoint = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) * point + transform.position;
			pathPoints.Add(adjustWaypoint);
		}

        if (playerController != null)
        {
            ShipCommand c = playerController.GetShipCommand(this.shipID);
            c.shipMove = move;
            UpdateMovementPath();
        }

        pathSelectionController.ShowPath();

        if (playerController != null && playerController.GetSelectedObject() == this.gameObject)
        {
            playerController.UpdateInfoUI();
        }
    }

	public bool MoveShip(float delta) {
		bool results = false;

		if(movePercent < 1.0f) {
			movePercent = Mathf.Min(movePercent + moveSpeed * delta, 1.0f);

			transform.position = BezierCurve.Curve(pathPoints, movePercent);
			Vector3 deltaPosition = BezierCurve.Curve(pathPoints, Mathf.Min(movePercent + BezierCurve.Delta(), 1.0f));

			transform.LookAt(deltaPosition);

		} else {
			results = true;
		}

		return results;
	}

	public bool CanTakeDamage() {
		return !shieldActive;
	}

	public void TakeDamage(int damage, bool ignoreImune) {
		if(CanTakeDamage() || ignoreImune) {
			health -= damage;

            //Update UI
            if (playerController != null && playerController.GetSelectedObject() == this.gameObject)
            {
                playerController.UpdateInfoUI();
            }

            // Show damage anim
        } else {
			// Show immune anim
		}
	}

    public void ExplodeShip(bool serverMode)
    {
        if(!hasExploded)
        {
            hasExploded = true;

            // Explode
            if (!serverMode)
            {
                Instantiate(explosionPrefab, this.transform.position, Quaternion.identity);
            }

            // Hide ship
            foreach(MeshCollider c in GetComponentsInChildren<MeshCollider>())
            {
                c.enabled = false;
            }

            // if selected ship... deselect
            if (playerController.GetSelectedObject() == this.gameObject)
            {
                playerController.DeselectObject();
            }

            // Destory ship
            Destroy(this.gameObject);
        }
    }

	public bool IsAlive() {
		return health > 0 ? true : false;
	}

	public ShipController GetClosestShipInRangeOfRocket(List<ShipController> shipControllers) {
		ShipController closetShip = null;
		float closestShipDistance = 0.0f;

		foreach(ShipController sc in shipControllers) {

			if(sc.shipOwner != shipOwner && sc.shipID != shipID) {
				Vector3 pos = sc.transform.position;
				Vector2 pos2DRelativeToShip = new Vector2(pos.x - transform.position.x, pos.z - transform.position.z);
                Vector2 tf2 = new Vector2(transform.forward.x, transform.forward.z);

				float angle = Vector2.Angle(pos2DRelativeToShip, tf2);
				float distance = Vector2.Distance(pos2DRelativeToShip, Vector2.zero);

				if(angle <= aimArc / 2.0f && distance <= rocketMaxDistance && distance >= rocketMinDistance) {
					if(closestShipDistance > distance || closetShip == null) {
						closetShip = sc;
						closestShipDistance = distance;
					}
				}
			}
		}

        return closetShip;
	}

    public void SetupSimArcVisual()
    {
        arcViewController.CreateArc(aimArc, rocketMaxDistance, rocketMinDistance);
        arcViewController.ShowArc();
    }

	public RocketController FireRocket(ShipController sc) {
		// Show fire anim

		List<Vector3> rocketPath = new List<Vector3>{
			this.transform.position,
			this.rocketAimPoint.position,
		};

		rocket = Instantiate(RocketPrefab, shipGun.position, transform.rotation) as GameObject;
		RocketController r = rocket.GetComponent<RocketController>();
		r.SetCourse(rocketPath, sc.transform, sc.visualHolder.transform, this);

        if (playerController != null && playerController.GetSelectedObject() == this.gameObject)
        {
            playerController.UpdateInfoUI();
        }

        return r;
	}

	public void ActivateShield() {
		shieldActive = true;
        // Show shield anim

        shieldVisualController.EnableShieldVisual();
        shieldEffect.Play();

        if (playerController != null && playerController.GetSelectedObject() == this.gameObject)
        {
            playerController.UpdateInfoUI();
        }
    }

	public void BoostEnergy(int gain) {
		energy = Math.Min(energy + gain, maxEnergy);

        // Show energy animation

        energyVisualController.EnableTriangle();
        energyEffect.Play();

        if (playerController != null && playerController.GetSelectedObject() == this.gameObject)
        {
            playerController.UpdateInfoUI();
        }
    }

    public void BeginSimPhase()
    {
        simMode = true;
    }

	public void EndSimPhase() {
        simMode = false;
        shieldActive = false;

        // Disable shield anim

        // Disable fire anim

        // Disable energy boost anim

        pathSelectionController.DisablePath();
        arcViewController.DisableArc();
        shieldVisualController.DisableShieldVisual();
        energyVisualController.DisableTriangle();

        shieldEffect.Stop();
        energyEffect.Stop();

        if (playerController != null && playerController.GetSelectedObject() == this.gameObject)
        {
            Select();
        }
    }

	void CheckDeselect() {
		if(selected && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) {
			playerController.DeselectObject();
			selected = false;
		}
	}

    public void Clicked()
    {
        if(playerController != null)
        {
            selected = playerController.SelectObject(this.gameObject);
        }
    }

    public void Select() {
		selectionCircleController.EnableCircle();
		pathSelectionController.ShowPath();

		ShipAction shipAction = GetShipAction();

		if(shipAction.type == ShipActionType.SHOOT) {
			arcViewController.ShowArc();
		}

		if(shipAction.type == ShipActionType.SHIELD) {
			shieldVisualController.EnableShieldVisual();
		}

		if(shipAction.type == ShipActionType.ENERGY) {
			energyVisualController.EnableTriangle();
		}
	}

	public void Deselect() {

        if(!simMode)
        {
            pathSelectionController.DisablePath();
            arcViewController.DisableArc();
            shieldVisualController.DisableShieldVisual();
            energyVisualController.DisableTriangle();
        }

        selectionCircleController.DisableCircle();

        selected = false;
	}

	public ShipAction GetShipAction() {
		ShipCommand sc = playerController.GetShipCommand(this);

		return sc.shipAction;
	}

	public ShipMove GetShipMove() {
		ShipCommand sc = playerController.GetShipCommand(this);

		return sc.shipMove;
	}

	public void UpdateMovementPath () {
		List<Vector3> path = new List<Vector3>();

		ShipMove sm = GetShipMove();
		path.Add(transform.position + visualHolder.transform.localPosition);

		foreach(Vector2 w in sm.waypoints) {
			Vector3 point = new Vector3(w.x, 0, w.y);
			Vector3 adjustWaypoint = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) * point + transform.position + visualHolder.transform.localPosition;
			path.Add(adjustWaypoint);
		}

		pathSelectionController.SetPath(path);
	}

	public void UpdateActionVisual() {
		ShipAction shipAction = GetShipAction();

		if(shipAction.type == ShipActionType.SHOOT) {
			arcViewController.CreateArc(aimArc, rocketMaxDistance, rocketMinDistance);

			if(selected) {
				arcViewController.ShowArc();
			} else {
				arcViewController.DisableArc();
			}

		} else {
			arcViewController.DisableArc();

		}

		if(shipAction.type == ShipActionType.SHIELD) {
			if(selected) {
				shieldVisualController.EnableShieldVisual();
			} else {
				shieldVisualController.DisableShieldVisual();
			}

		} else {
			shieldVisualController.DisableShieldVisual();

		}

		if(shipAction.type == ShipActionType.ENERGY) {
			if(selected) {
				energyVisualController.EnableTriangle();
			} else {
				energyVisualController.DisableTriangle();
			}

		} else {
			energyVisualController.DisableTriangle();

		}
	}

	public void SetShipState(ShipState s) {
		
		if(s.shipID != shipID) {
			Debug.LogWarning("ShipState and ShipController have conflicting IDs... was this intended?");
		}

		health = s.health;
		shipOwner = s.owner;
		energy = s.energy;
		maxEnergy = s.maxEnergy;
		maxHealth = s.maxHealth;
		transform.position = s.position;
		transform.rotation = Quaternion.Euler(s.rotation);
	}

    public void UpdateVisualShipLevel(float height)
    {
        if(visualHolder != null && visualHolder.transform != null)
        {
            Vector3 pos = visualHolder.transform.localPosition;
            pos.Set(pos.x, height, pos.z);
            visualHolder.transform.localPosition = pos;

            pathSelectionController.UpdateVisualHeight(visualHolder.transform.position.y);
        }
    }
}
