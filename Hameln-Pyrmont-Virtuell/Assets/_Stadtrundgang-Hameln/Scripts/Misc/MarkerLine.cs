using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerLine : MonoBehaviour
{
	public LineRenderer line;
	public GameObject startPosition;
	public GameObject endPosition;

	void Start()
    {
        
    }

    void Update()
    {
		line.SetPosition( 0, startPosition.transform.position );
		line.SetPosition( 1, endPosition.transform.position );
	}
}
