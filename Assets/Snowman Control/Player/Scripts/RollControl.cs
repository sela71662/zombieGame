using UnityEngine;
using System.Collections;

public class RollControl : MonoBehaviour {

	public GameObject head;

	// Jump Setup
	private float jumpForce = 80f;
	private Vector3 jumpVector = new Vector3(0f, 1f, 0f);
	// Speed Setup
	private float moveSpeed = 50f;
	// RigidBody Max Angular Velocity Setup
	private float normalMaxVelocity = 24f;
	// Head Tilt Speed
	private float headSpeed = 7f;

	private bool canJump;

	private Rigidbody rb;
	private Vector3 torque;
	private Vector3 headAngle;
	private float torqueX;
	private float torqueZ;
	private float lookX;
	private float lookZ;
	private float lookDown;


	void Start () {
		rb = GetComponent<Rigidbody> ();
		rb.maxAngularVelocity = normalMaxVelocity;
		canJump = true;
	}

	void Update () {
		head.transform.position = transform.position;
	}

	void FixedUpdate () {
		// Body torque for movement
		torqueZ = -Mathf.Round (Input.GetAxis ("Horizontal") * 100f);
		torqueX = Mathf.Round (Input.GetAxis ("Vertical") * 100f);

		// Move Roll
		torque.Set (torqueX, 0.0f, torqueZ);
		torque = Camera.main.transform.TransformDirection (torque);
		rb.AddTorque (torque * moveSpeed);

		// Jump
		if (canJump && Input.GetButton("Jump")) {
			canJump = false;
			rb.AddForce (jumpVector * jumpForce, ForceMode.Impulse);
		}

		// Head move & rotation
		if (rb.linearVelocity.magnitude > 0f && canJump) {
			lookX = rb.linearVelocity.x;
			lookZ = rb.linearVelocity.z;
			// Set Head Angle when move & Death pose when die
			lookDown = -Mathf.Floor (rb.linearVelocity.magnitude / 2f);
			if (lookDown < -4f) {
				headAngle = Vector3.Slerp (headAngle, new Vector3(lookX, lookDown, lookZ), 3f * Time.deltaTime);
			} else {
				headAngle.Set (lookX, 0f, lookZ);
			}
			if (headAngle.normalized.magnitude == 0f) {
				headAngle.Set (0f, 0f, 1f);
			}
			Quaternion newRotation = Quaternion.LookRotation (headAngle);
			head.transform.rotation = Quaternion.Slerp (head.transform.rotation, newRotation, headSpeed * Time.deltaTime);
		}

	} // END of FIXED UPDATE

	// Grounded Check
	void OnCollisionEnter(Collision collision) {
		if (collision.gameObject.CompareTag ("Ground"))
			canJump = true;
	}

}
