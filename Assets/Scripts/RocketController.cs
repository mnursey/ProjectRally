using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class RocketController : MonoBehaviour {

	private List<Vector3> fireVectors;
	private Transform target;

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

				transform.LookAt(deltaPosition, Vector3.right);

				if (actionPercent == 1.0f) {					 
					return true;
				}
			}
		}

		return false;
	}

	public void SetCourse(List<Vector3> fireVectors, Transform target, ShipController orginShip) {
		this.fireVectors = fireVectors;
		this.target = target;
		this.orginShip = orginShip;
	}

	public void Explode() {
        Instantiate(explosionPrefab, this.transform.position, Quaternion.identity);
    }
}
