using UnityEngine;
using System.Collections;

public class PlayerCamera : MonoBehaviour {

	private Transform PlayerBody;
	private Vector3 offset;
	private Vector3 dempVector;

	void Awake () {
		PlayerBody = GameObject.FindGameObjectWithTag ("Player").transform;
		offset = transform.position - PlayerBody.position;
	}

	void LateUpdate () {
		// Moving Camera Structure with Player
		transform.position = Vector3.SmoothDamp(transform.position, PlayerBody.position + offset, ref dempVector, 0.05f);
	}

}