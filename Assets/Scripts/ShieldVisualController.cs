using UnityEngine;
using System.Collections;

public class ShieldVisualController : MonoBehaviour {

	public VisualCircleController lower;
	public VisualCircleController middle;
	public VisualCircleController upper;

	public void Start() {

	}

	public void EnableShieldVisual() {
		//lower.Select();
		middle.EnableCircle();
		//upper.Select();
	}

	public void DisableShieldVisual() {
		//lower.Deselect();
		middle.DisableCircle();
		//upper.Deselect();
	}

}
