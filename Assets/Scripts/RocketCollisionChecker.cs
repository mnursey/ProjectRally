using UnityEngine;
using System.Collections;

public class RocketCollisionChecker : MonoBehaviour {

	private bool hit = false;
	private Collider other;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTrigger(Collider other) {
		if(!hit) {
			this.hit = true;	
			this.other = other;
		} 
	}

	public bool Hit() {
		return hit;
	}

	public Collider HitWith() {
		return other;
	}
}
