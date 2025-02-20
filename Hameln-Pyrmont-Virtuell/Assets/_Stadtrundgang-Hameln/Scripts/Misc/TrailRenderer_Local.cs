using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class TrailRenderer_Local : MonoBehaviour {
 
	public Transform objToFollow;
	public LineRenderer myLine;
	public int maxPositions = 3;
	public float addPointInterval = 0.02f;

	private Vector3 lastPosition;
	private float distIncrement = 0.1f;
	private bool limitTrailLength = true; 
	private float addPointTime= 0.0f;
 
	void Start() {
		
		if( myLine == null )myLine = GetComponent<LineRenderer>();
		myLine.useWorldSpace = false;
		Reset();
	}
 
	void Reset() {
		myLine.positionCount = 0;
		AddPoint(objToFollow.localPosition);
	}
 
	void AddPoint(Vector3 newPoint) {

		myLine.positionCount += 1;
		myLine.SetPosition(myLine.positionCount - 1, newPoint);
 
		if (limitTrailLength && myLine.positionCount > maxPositions) {
			TruncatePositions(maxPositions);
		}
		lastPosition = newPoint;
	}
 
 
	void TruncatePositions(int newLength) {

		Vector3[] tempList = new Vector3[newLength];
		int nExtraItems = myLine.positionCount - newLength;
		for (int i=0; i<newLength; i++) {
			tempList[i] = myLine.GetPosition(i + nExtraItems);
		}
 
		myLine.positionCount = newLength;
		myLine.SetPositions(tempList);
	}
 
 
	void Update () {
		
		Vector3 curPosition = objToFollow.localPosition;
		
		/*
		// Check to see if object has moved far enough
		if (Vector3.Distance(curPosition, lastPosition) > distIncrement) {
			AddPoint(curPosition);
           
		}
		*/
		
		addPointTime += Time.deltaTime;
		if( addPointTime > addPointInterval ){
			addPointTime = 0;
			AddPoint(curPosition);
		}
	}
}
 