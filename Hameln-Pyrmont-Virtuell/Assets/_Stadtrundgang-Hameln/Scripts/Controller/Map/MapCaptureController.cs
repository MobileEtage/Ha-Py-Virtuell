using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;
using Mapbox.Unity.Location;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using ARLocation;

public class MapCaptureController : MonoBehaviour
{
	public bool isEnabled = false;
	public bool interactionEnabled = false;
	public bool shouldSaveMapPosition = false;
	public bool loadPreviousPath = false;
	public int interpolateSteps = 10;
	public GameObject dummyMarker;
	public GameObject helperMarker;

	[Space(10)]
	
	public MapController mapController;
	public LineRenderer pathLine;
	public GameObject capturePathButton;
	public float minTravelDistance = 3.0f;
	public float currentDistance = 0;
	public string pathFileToLoad = "geoPositions_16-42-31_14-10-2021.json";
	
	private List<Vector2d> geoPositions = new List<Vector2d>();
	private List<Vector3> worldPositions = new List<Vector3>();
	private Vector3 lastPosition = new Vector3(-1000, -1000, -1000);
	private float capturePositionTimer = 0;
	private float capturePositionInterval = 5;
	private JSONNode dataJson;
	private string savePath = "";
	private bool isLoading = false;
	private bool isCapturingPath = false;
		
	public static MapCaptureController instance;
	void Awake(){
		
		instance = this;		
	}
	
	void LateUpdate()
	{
		if( !isEnabled ) return;
		
		# if UNITY_EDITOR
		if( Input.GetKeyDown("l") ){
			LoadPath(pathFileToLoad);
		}
		#endif
		
		CapturePosition();
		UpdatePathLine();
		
		if( interactionEnabled ){
			
			dummyMarker.SetActive(true);
			helperMarker.SetActive(true);
			Interact();
		}
	}
	
	public void Init(){

		if(loadPreviousPath){
			
			LoadPath(pathFileToLoad);
			savePath = pathFileToLoad;
		}
		else{

			dataJson = JSONNode.Parse("{\"geoPositions\":[]}");
			savePath = 
			Application.persistentDataPath + "/" + "geoPositions"
			+ DateTime.Now.ToString("_HH-mm-ss_dd-MM-yyyy") + ".json";		
		}
		
		isEnabled = true;
	}
	
	public void Reset(){
		
		dummyMarker.SetActive(false);
		helperMarker.SetActive(false);
		geoPositions.Clear();
		worldPositions.Clear();
		
		isEnabled = false;
		isCapturingPath = false;
		interactionEnabled = false;
		shouldSaveMapPosition = false;
		
		capturePathButton.GetComponentInChildren<TextMeshProUGUI>().text = 
			isCapturingPath ? "Stop track": "Track path";
	}
	
	public void Interact(){
		
		if( Input.GetMouseButtonDown(0) ){
			
			Ray ray = mapController.mapCamera.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			LayerMask mapLayer = LayerMask.GetMask("CustomMap");
			if( Physics.Raycast( ray, out hit, 1000, mapLayer ) ){
				
				dummyMarker.transform.position = hit.point;
				Vector2d geoPosition = mapController.abstractMap.WorldToGeoPosition( dummyMarker.transform.position );
				print(geoPosition.y.ToString("F10") );
				print(geoPosition.x.ToString("F10") );
			}
		}
		
		if( Input.GetMouseButtonDown(1)){
		
			if( shouldSaveMapPosition ){
		
				if( isLoading ) return;
				isLoading = true;
				StartCoroutine( SavePositionCoroutine() );
			}
		}
	}
	
	public IEnumerator SavePositionCoroutine(){
		
		if( lastPosition.x == -1000 ){
			SavePosition (dummyMarker.transform.position);
		}
		else{
				
			lastPosition = mapController.abstractMap.GeoToWorldPosition( geoPositions[geoPositions.Count-1] );

			Vector3 dir = dummyMarker.transform.position - lastPosition;
			Vector3 posPrevious = lastPosition;

			for( int i = 1; i < interpolateSteps; i++ ){
						
				Vector3 pos = lastPosition + dir*((float)i/(float)interpolateSteps);

				Vector2d p2 = mapController.abstractMap.WorldToGeoPosition( posPrevious );
				Vector2d p1 = mapController.abstractMap.WorldToGeoPosition( pos );
				float dist = ToolsController.instance.CalculateDistance( 
					(float)p1.x, (float)p2.x, (float)p1.y, (float)p2.y);
							
				//print(dist);
				if( dist > 0.5f ){
							
					print("Dist to next pos is " + dist);
					
					//Vector2d geoPosition = mapController.abstractMap.WorldToGeoPosition( pos );
					//geoPositions.Add(new Vector2d(geoPosition.x, geoPosition.y));
					//worldPositions.Add(pos);
					helperMarker.transform.position = pos;
					
					yield return new WaitForEndOfFrame();
					
					if( shouldSaveMapPosition ){
						
						//GameObject obj = Instantiate(helperMarker, mapController.transform);
						//obj.transform.position = pos;
						//obj.transform.localScale = helperMarker.transform.localScale;
						SavePosition( pos );
					}
					posPrevious = pos;
				}
			}
			SavePosition (dummyMarker.transform.position);
		}
				
		lastPosition = dummyMarker.transform.position;
		File.WriteAllText( savePath, dataJson.ToString() );
		//print("Saved");
		
		yield return new WaitForSeconds(1.0f);
		isLoading = false;
	}
	
	public void SavePosition( Vector3 worldPosition ){
		
		Vector2d geoPosition = mapController.abstractMap.WorldToGeoPosition( worldPosition );
		geoPositions.Add(new Vector2d(geoPosition.x, geoPosition.y));
		worldPositions.Add(worldPosition);
		
		JSONNode posNode = 
			JSONNode.Parse(
			"{\"latitude\":" + geoPosition.x.ToString("F10").Replace(",", ".") + 
			",\"longitude\":" + geoPosition.y.ToString("F10").Replace(",", ".") + "}");
					
		dataJson["geoPositions"].Add(posNode);
	}
	
	public void ToggleCapturePath(){
			
		isCapturingPath = !isCapturingPath;
		
		if( isCapturingPath ){
			
			geoPositions.Clear();
			dataJson = JSONNode.Parse("{\"geoPositions\":[]}");
			savePath = 
				Application.persistentDataPath + "/" + "geoPositions"
				+ DateTime.Now.ToString("_HH-mm-ss_dd-MM-yyyy") + ".json";
			
		}
		
		capturePathButton.GetComponentInChildren<TextMeshProUGUI>().text = 
			isCapturingPath ? "Stop track": "Track path";
	}

	public void LoadPath( string file ){
		
		file = Application.persistentDataPath + "/" + file;
		if( File.Exists(file) ){
		
			print("loading path");
			
			geoPositions.Clear();
			dataJson = JSONNode.Parse( File.ReadAllText(file) );
			for( int i = 0; i < dataJson["geoPositions"].Count; i++ ){
				
				double latitude = 
					ToolsController.instance.GetDoubleValueFromJsonNode(dataJson["geoPositions"][i]["latitude"]);
				double longitude = 
					ToolsController.instance.GetDoubleValueFromJsonNode(dataJson["geoPositions"][i]["longitude"]);
				geoPositions.Add( new Vector2d(latitude, longitude) );
			}
		}
	}
	
	public void CapturePosition(){
		
		if( !isCapturingPath ) return;		
			
		if( geoPositions.Count <= 0 ){
			AddCurrentPosition();
		}
		else{
			
			Vector2d currentGeoPosition = mapController.GetCurrentLocation();
			
			currentDistance = ToolsController.instance.CalculateDistance(
				(float)currentGeoPosition.x, (float)geoPositions[geoPositions.Count-1].x, 
				(float)currentGeoPosition.y, (float)geoPositions[geoPositions.Count-1].y
			);
			if( currentDistance > minTravelDistance ){
				AddCurrentPosition();
			}
		}
		
		/*
		capturePositionTimer += Time.deltaTime;
		if( capturePositionTimer > capturePositionInterval ){
			
		capturePositionTimer = 0;
		Vector2 pos = GetCurrentLocation();
		geoPositions.Add(pos);
		}
		*/		
	}
	
	public void AddCurrentPosition(){
		
		print("AddCurrentPosition, distance to last position " + currentDistance);
		
		Vector2d pos = mapController.GetCurrentLocation();

		geoPositions.Add(pos);
		
		JSONNode posNode = JSONNode.Parse("{\"latitude\":" + pos.x.ToString("F10").Replace(",", ".") + ",\"longitude\":" + pos.y.ToString("F10").Replace(",", ".") + "}");
		dataJson["geoPositions"].Add(posNode);
		File.WriteAllText( savePath, dataJson.ToString() );
	}
	
	public void UpdatePathLine(){
		
		/*
		pathLine.positionCount = worldPositions.Count;
		Vector3 [] positions = new Vector3[ worldPositions.Count ];
		for( int i = 0; i < worldPositions.Count; i++ ){
			positions[i] = worldPositions[i];
		}
		*/
		
		pathLine.positionCount = geoPositions.Count;
		Vector3 [] positions = new Vector3[ geoPositions.Count ];
		for( int i = 0; i < geoPositions.Count; i++ ){
			
			Vector3 pos = mapController.abstractMap.GeoToWorldPosition( 
				new Vector2d(
				geoPositions[i].x, 
				geoPositions[i].y
				), true);
				
			positions[i] = pos;
		}
		
		
		pathLine.SetPositions(positions);
	}
	
	public void UploadPath(){
		
		if( isLoading ) return;		
		if( dataJson == null ){
		
			InfoController.instance.ShowMessage("Pfaddatei nicht vorhanden");
			return;
		}
		
		isLoading = true;
		StartCoroutine( UploadPathCoroutine() );
	}
	
	public IEnumerator UploadPathCoroutine(){
				
		InfoController.instance.ShowLoadingMessage("Pfaddatei wird hochgeladen...");

		yield return StartCoroutine(
			UploadFileCoroutine( savePath, "https://app-etagen.die-etagen.de/BadIburg/Data/Upload/upload.php", (bool success, string respond) => 
			{
				if( success ){
					InfoController.instance.DisableLoadingMessage();
					InfoController.instance.ShowMessage("Pfaddatei erfolgreich hochgeladen");
				}else{
					InfoController.instance.DisableLoadingMessage();
					InfoController.instance.ShowMessage("Pfaddatei konnte nicht hochgeladen werden");
				}
			})
		);
	
		isLoading = false;
	}
	
	public IEnumerator UploadFileCoroutine( string filePath, string phpScriptURL, Action<bool, string> Callback )
	{
		WWWForm form = new WWWForm ();
		byte[] bytes = File.ReadAllBytes(filePath);
		form.AddField ("dataName", Path.GetFileName(filePath));
		form.AddBinaryData ("data", bytes);

		UnityWebRequest www = UnityWebRequest.Post(phpScriptURL, form);
		www.timeout = 30;
		www.chunkedTransfer = false;
		
		yield return www.SendWebRequest();

		if (!string.IsNullOrEmpty (www.error)) {
			Debug.LogError ("Error uploading file: " + www.error);
			Callback(false, www.error);
		} else {
			print ("Response: " + www.downloadHandler.text);
			Callback(true, "Upload erfolgreich");
		}
	}
}
