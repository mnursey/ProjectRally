using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ArcViewController : MonoBehaviour {

	LineRenderer lr;
	public int segments;
	public float width;

	void Start () {
		
	}

	void Awake() {
		lr = GetComponent<LineRenderer>();
		lr.enabled = false;
	}

	public void CreateArc(float theta, float radiusMax, float radiusMin) {
		lr.useWorldSpace = false;
        lr.startWidth = width;
        lr.endWidth = width;

		List<Vector3> bigArc = GetArc(theta, radiusMax);
		List<Vector3> smallArc = GetArc(theta, radiusMin);

		smallArc.Reverse();

		List<Vector3> displayPath = new List<Vector3>();
		displayPath.AddRange(bigArc);
		displayPath.AddRange(smallArc);
		displayPath.Add(bigArc[0]);

        lr.positionCount = displayPath.Count;
		lr.SetPositions(displayPath.ToArray());
	}

	private List<Vector3> GetArc(float theta, float radius) {
		List<Vector3> displayPath = new List<Vector3>();
		float startAngle = -theta / 2;
		float endAngle = theta / 2;

		for(float movePercent = 0.0f; movePercent <= 1.0f; movePercent += 1.0f / segments) {
			if(movePercent > 1.0f) {
				movePercent = 1.0f;
			}

			float a = Mathf.Lerp(startAngle, endAngle, movePercent);
			float x = Mathf.Sin(Mathf.Deg2Rad * a) * radius;
			float y = Mathf.Cos(Mathf.Deg2Rad * a) * radius;

			displayPath.Add(new Vector3(x, 0, y));
		}

		return displayPath;
	}

	public void ShowArc() {
		lr.enabled = true;
	}

	public void DisableArc() {
		lr.enabled = false;
	}
}
