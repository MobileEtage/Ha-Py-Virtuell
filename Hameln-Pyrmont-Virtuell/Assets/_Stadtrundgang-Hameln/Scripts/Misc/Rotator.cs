using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
	public float Speed = 10.0f;
	public Vector3 Axis = Vector3.up;

	private float angle;

	void Update()
	{
		angle += Speed * Time.deltaTime;
		transform.localRotation = Quaternion.AngleAxis(angle, Axis);
	}
}
