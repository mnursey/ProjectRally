using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class VisualCircleController : MonoBehaviour {

	LineRenderer lr;
	public int segments;
	public float radius;
	public float width;

	void Awake () {
	
		lr = GetComponent<LineRenderer>();
		lr.enabled = false;
		DrawCircle();
	}

	private void DrawCircle() {
		lr.useWorldSpace = false;
        lr.startWidth = width;
        lr.endWidth = width;

		List<Vector3> positions = new List<Vector3>();

		for(int i = 0; i <= segments; i++) {
			float rad = Mathf.Deg2Rad * (i * 360f / segments);
			positions.Add(new Vector3(Mathf.Sin(rad) * radius, 0, Mathf.Cos(rad) * radius));
		}

        lr.positionCount = positions.Count;
		lr.SetPositions(positions.ToArray());
	}

	void Update() {
		//DrawCircle();
	}

	public void EnableCircle() {
		lr.enabled = true;
	}

	public void DisableCircle() {
		lr.enabled = false;
	}
}
