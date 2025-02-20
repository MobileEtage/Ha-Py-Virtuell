using UnityEngine;

public class FaceCamera : MonoBehaviour {

	void LateUpdate () {
        transform.LookAt(Camera.main.transform);
        transform.Rotate(Vector3.up, 180);
	}
}
