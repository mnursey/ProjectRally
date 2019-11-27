using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class RocketController : MonoBehaviour {

	private List<Vector3> fireVectors;
	private Transform target;
    private Transform targetVisual;
    private float originalHeight = 0.0f;

    public GameObject rocketVisualHolder;

	private float actionPercent = 0.0f;
	public float actionSpeed;
	public ShipController orginShip;

    public GameObject explosionPrefab;

	void Start () {
			
	}
	
	void Update () {
	
	}


	public Transform GetTarget() {
		return target;
	}

	public bool MoveTowardsTarget(float simDelta) {
		if(actionPercent < 1.0f) {
			// Move Rocket Toward Target
			List<Vector3> targetPath = new List<Vector3>();
			targetPath.AddRange(fireVectors);

            if(target != null)
            {
                targetPath.Add(target.position);
            }
            else
            {
                targetPath.Add(this.transform.position + (2.0f * this.transform.forward));
            }

            if (targetPath != null && targetPath.Count > 0) {
				actionPercent = Mathf.Min(actionPercent + actionSpeed * simDelta, 1.0f);

				transform.position = BezierCurve.Curve(targetPath, actionPercent);
				Vector3 deltaPosition = BezierCurve.Curve(targetPath, Mathf.Min(actionPercent + BezierCurve.Delta(), 1.0f));

                float heightVisual = targetVisual != null ? targetVisual.position.y : rocketVisualHolder.transform.position.y;
                rocketVisualHolder.transform.position = new Vector3(rocketVisualHolder.transform.position.x, Mathf.Lerp(originalHeight, heightVisual, actionPercent), rocketVisualHolder.transform.position.z);
                deltaPosition = new Vector3(deltaPosition.x, Mathf.Lerp(originalHeight, heightVisual, actionPercent + BezierCurve.Delta()), deltaPosition.z);
                rocketVisualHolder.transform.LookAt(deltaPosition);

                if (actionPercent == 1.0f) {					 
					return true;
				}
			}
		}

		return false;
	}

	public void SetCourse(List<Vector3> fireVectors, Transform target, Transform targetVisual, ShipController orginShip) {
		this.fireVectors = fireVectors;
		this.target = target;
		this.orginShip = orginShip;
        this.originalHeight = orginShip.visualHolder.transform.position.y;
        this.targetVisual = targetVisual;
	}

	public void Explode() {
        Instantiate(explosionPrefab, this.transform.position, Quaternion.identity);
    }
}
