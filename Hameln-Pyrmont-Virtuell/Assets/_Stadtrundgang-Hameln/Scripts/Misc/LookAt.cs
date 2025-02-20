using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAt : MonoBehaviour
{

	public Transform target;
	public bool fixX = true;
	public bool fixZ = true;
	public Vector3 offset = Vector3.zero;

	void Awake()
	{

		if ( target == null && Camera.main != null )
		{
			target = Camera.main.transform;
		}
	}
	void LateUpdate()
	{

		if ( target == null ) return;

		Vector3 rot = this.transform.eulerAngles;
		this.transform.LookAt( target );

		if ( fixX && fixZ )
		{
			this.transform.eulerAngles = new Vector3( 0, this.transform.eulerAngles.y + 180, 0 ) + offset;
		}
		else if ( fixX )
		{
			this.transform.eulerAngles = new Vector3( 0, this.transform.eulerAngles.y + 180, rot.z ) + offset;
		}
		else if ( fixZ )
		{
			this.transform.eulerAngles = new Vector3( rot.x, this.transform.eulerAngles.y + 180, 0 ) + offset;
		}
	}
}
