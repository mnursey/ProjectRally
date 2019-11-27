using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PathSelectionController : MonoBehaviour {

	LineRenderer lr;
	public int segments;
	public float width;
    List<Vector3> savedDisplayPaths = new List<Vector3>();

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

        savedDisplayPaths = displayPath;

        lr.positionCount = displayPath.Count;
		lr.SetPositions(displayPath.ToArray());
	}

    public void UpdateVisualHeight(float height)
    {
        for(int i = 0; i < savedDisplayPaths.Count; i++)
        {
            savedDisplayPaths[i] = new Vector3(savedDisplayPaths[i].x, height, savedDisplayPaths[i].z);
        }

        lr.SetPositions(savedDisplayPaths.ToArray());
    }

    public void ShowPath() {
		lr.enabled = true;
	}

	public void DisablePath() {
		lr.enabled = false;
	}
}
