using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PathSelectionController : MonoBehaviour {

	LineRenderer lr;
	public int segments;
	public float width;

	void Start () {

	}

	void Awake() {
		lr = GetComponent<LineRenderer>();
		lr.enabled = false;
	}

	public void SetPath(List<Vector3> path) {
		lr.useWorldSpace = true;
        lr.startWidth = width;
        lr.endWidth = width;

		List<Vector3> displayPath = new List<Vector3>();

		for(float movePercent = 0.0f; movePercent <= 1.0f; movePercent += 1.0f / segments) {
			if(movePercent > 1.0f) {
				movePercent = 1.0f;
			}

			displayPath.Add(BezierCurve.Curve(path, movePercent));
		}

        lr.positionCount = displayPath.Count;
		lr.SetPositions(displayPath.ToArray());
	}

	public void ShowPath() {
		lr.enabled = true;
	}

	public void DisablePath() {
		lr.enabled = false;
	}
}
