using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class VisualTriangleController : MonoBehaviour {

	LineRenderer lr;
	public float radius;
	public float width;

	void Awake () {

		lr = GetComponent<LineRenderer>();
		lr.enabled = false;
		DrawTriangle();
	}
		
	private void DrawTriangle() {
		lr.useWorldSpace = false;
        lr.startWidth = width;
        lr.endWidth = width;

		List<Vector3> positions = new List<Vector3>();

		positions.Add(new Vector3(0f, 0f, 1f) * radius);
		positions.Add(new Vector3(0.8f, 0f, -0.5f) * radius);
		positions.Add(new Vector3(-0.8f, 0f, -0.5f) * radius);
		positions.Add(new Vector3(0f, 0f, 1f) * radius);


        lr.positionCount = positions.Count;
		lr.SetPositions(positions.ToArray());
	}

	public void EnableTriangle() {
		lr.enabled = true;
	}

	public void DisableTriangle() {
		lr.enabled = false;
	}
}
