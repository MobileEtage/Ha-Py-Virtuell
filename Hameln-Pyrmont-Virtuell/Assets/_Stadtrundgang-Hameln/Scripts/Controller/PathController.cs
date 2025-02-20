using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

using SimpleJSON;
using TMPro;

public class PathController : MonoBehaviour
{
	public GameObject saveButton;
	public GameObject pathOptions;
	public GameObject markerContent;
	public Transform movingDummy;
	public ImageTarget marker;
	
	[Space(10)]
	
	public List<Vector3> pathPoints = new List<Vector3>();
	public float moveRotationSpeed = 1.0f;
	public float moveSpeed = 1.0f;

	private JSONNode dataJson;
	private int nextPathPoint = 1;
	private bool isEnabled = false;
	private bool isSaving = false;

	public static PathController Instance;
	private void Awake()
	{
		Instance = this;		
	}

	void Update()
	{
		if( !isEnabled ) return;
		HandleAutoMove( movingDummy );
		
		if( marker.isTracking ){
			saveButton.SetActive(true);	
		}else{
			saveButton.SetActive(false);
		}
	}
	
	public void Init(){
		
		isEnabled = true;
		pathOptions.SetActive(true);
		//markerContent.SetActive(true);
	}
	
	public void Reset(){
		
		isEnabled = false;
		pathOptions.SetActive(false);
		markerContent.SetActive(false);
		
		pathPoints.Clear();
		nextPathPoint = 1;
	}

	public void Save(){
				
		if( isSaving ) return;
		isSaving = true;
		StartCoroutine( SaveCoroutine() );
	}
	
	public IEnumerator SaveCoroutine(){
		
		dataJson = JSONNode.Parse("{}");

		List<Vector3> myPathPoints = new List<Vector3>();
		for( int i = 0; i < MeasureController.Instance.lineChain.lines.Count; i++ ){
			
			if( MeasureController.Instance.lineChain.lines[i].startPoint != null ){
				myPathPoints.Add( MeasureController.Instance.lineChain.lines[i].startPoint.transform.position );
			}
			if( MeasureController.Instance.lineChain.lines[i].endPoint != null ){
				myPathPoints.Add( MeasureController.Instance.lineChain.lines[i].endPoint.transform.position );
			}
		}
		
		for( int i = 0; i < myPathPoints.Count; i++ ){
					
			JSONNode dataNode = JSONNode.Parse("{}");	
			Vector3 localPosition = marker.transform.InverseTransformPoint( myPathPoints[i] );
			dataNode["xPos"] = localPosition.x.ToString();
			dataNode["yPos"] = localPosition.y.ToString();
			dataNode["zPos"] = localPosition.z.ToString();
			dataJson["pathPoints"].Add(dataNode);
		}

		string savePath = Application.persistentDataPath + "/pathData.json";
		File.WriteAllText(savePath, dataJson.ToString());
		
		saveButton.GetComponentInChildren<TextMeshProUGUI>().text = "Saved!";
		yield return new WaitForSeconds(2.0f);
		saveButton.GetComponentInChildren<TextMeshProUGUI>().text = "Save";
		
		isSaving = false;
	}
	
	public void CreatePath(){
		
		pathPoints.Clear();
		
		for( int i = 0; i < dataJson["pathPoints"].Count; i++ ){

			float xPos = GetFloatValueFromJsonNode(dataJson["pathPoints"][i]["xPos"]);
			float yPos = GetFloatValueFromJsonNode(dataJson["pathPoints"][i]["yPos"]);
			float zPos = GetFloatValueFromJsonNode(dataJson["pathPoints"][i]["zPos"]);
			
			Vector3 globalPosition = marker.transform.TransformPoint( new Vector3( xPos, yPos, zPos ) );

			pathPoints.Add(globalPosition);

		}
		
		if( pathPoints.Count > 0 ){
			movingDummy.position = pathPoints[0];
			nextPathPoint = 1;	
			markerContent.SetActive(true);
		}
	}
	
	public float GetFloatValueFromJsonNode( JSONNode node ){
		
		string valueString = node.Value.Replace(',', '.');
		
		float val = 0;
		//bool parsed = float.TryParse( valueString, out val );
		bool parsed = float.TryParse( valueString, 	NumberStyles.Float, CultureInfo.InvariantCulture, out val );
	
		if( parsed ){
			return val;
		}
		
		return node.AsFloat;
	}
	
	public void Run(){
		
		if( marker.isTracking ){
			
			if( dataJson == null ){
			
				string savePath = Application.persistentDataPath + "/pathData.json";
				if( File.Exists(savePath) ){
					dataJson = JSONNode.Parse( File.ReadAllText(savePath) );
					CreatePath();
					return;
				}
			}
			else{
				
				CreatePath();
				return;
			}
		}
		
		print("Run " + MeasureController.Instance.lineChain.lines.Count);
		
		pathPoints.Clear();
		for( int i = 0; i < MeasureController.Instance.lineChain.lines.Count; i++ ){
			
			if( MeasureController.Instance.lineChain.lines[i].startPoint != null ){
				pathPoints.Add( MeasureController.Instance.lineChain.lines[i].startPoint.transform.position );
			}
			if( MeasureController.Instance.lineChain.lines[i].endPoint != null ){
				pathPoints.Add( MeasureController.Instance.lineChain.lines[i].endPoint.transform.position );
			}
		}
		
		if( pathPoints.Count > 0 ){
			movingDummy.position = pathPoints[0];
			nextPathPoint = 1;		
			markerContent.SetActive(true);
		}
	}
	
	public void HandleAutoMove( Transform movingObject ){

		if( pathPoints.Count <= 0 ){
			
			if( !movingObject.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("IdleSatBreathe"))
			{
				movingObject.GetComponent<Animator>().Play("IdleSatBreathe");
			}
			return;
		}
				
		if( nextPathPoint < pathPoints.Count && nextPathPoint >= 0 ){
			
			if( !movingObject.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Walk"))
			{
				movingObject.GetComponent<Animator>().Play("Walk");
			}
			
			/******** Set rotation ********/
			Vector3 targetPoint = pathPoints[nextPathPoint];
			//if( nextPathPoint+1 < pathPoints.Count ){
			//	targetPoint = pathPoints[nextPathPoint+1];
			//}
			
			//var direction = targetPosition - movingObject.position;
			var direction = targetPoint - movingObject.position;

			if (direction != Vector3.zero)
			{
				//direction.y = 0;
				direction = direction.normalized;
				var rotation = Quaternion.LookRotation(direction);
				//movingObject.rotation = Quaternion.Slerp(
				//	movingObject.rotation, rotation, moveRotationSpeed * Time.deltaTime);
				movingObject.rotation = Quaternion.Lerp(
					movingObject.rotation, rotation, moveRotationSpeed * Time.deltaTime);
			}			
						
			/******** Set position ********/
			Vector3 targetPosition = pathPoints[nextPathPoint];
			
			movingObject.transform.position = Vector3.MoveTowards(
				movingObject.position, targetPosition, moveSpeed * Time.deltaTime);
			

			/******** Update target point ********/
			float dist = Vector3.Distance(movingObject.position, targetPosition);
			if( dist < 0.1f ){
				
				if( nextPathPoint+1 < pathPoints.Count ){
					
					dist = Vector3.Distance(
						pathPoints[nextPathPoint], 
						pathPoints[nextPathPoint+1]);
						
					// If pathpoints are to close we skip one point
					if( dist < 0.25f ){
						
						nextPathPoint += 2;
					}
					else{
						nextPathPoint++;
					}
				}
				else{
					nextPathPoint++;
				}
			}
		}
		else{
			
			if( !movingObject.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("IdleSatBreathe"))
			{
				movingObject.GetComponent<Animator>().CrossFade("IdleSatBreathe", 0.25f);
			}
		}
	}
}
