using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour {

	public float _rotationSpeed = 60.0f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		float rot = Input.GetAxis("CamRotate") * _rotationSpeed * Time.deltaTime;
		transform.Rotate(0, -rot, 0);
	}
}
