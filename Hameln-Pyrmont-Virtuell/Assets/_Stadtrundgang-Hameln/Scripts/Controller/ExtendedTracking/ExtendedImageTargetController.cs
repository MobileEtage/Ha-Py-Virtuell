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

public class ExtendedImageTargetController : MonoBehaviour
{
	private bool usePositionsFromResources = false;

	public GameObject uploadButton;
	public GameObject saveButton;
	public GameObject deleteButton;
	public GameObject addButton;
	public GameObject options;
	
	[Space(10)]

	public Transform mainCamera;
	public LayerMask collisionLayerMaskModel;
	public float minDistanceBeforeVisible = 3.0f;
	private bool shouldShowPlanes = true;

	[Space(10)]
	
	public string markerPrefix = "";
	public List<ImageTarget> imageTargets = new List<ImageTarget>();
	public List<GameObject> imageTargetObjects = new List<GameObject>();
	private List<string> changedIDs = new List<string>();
	
	private GameObject markerHelper;
	private GameObject currentSelectedObject;
	private JSONNode dataJson;
	private string syncedMarker = "";
	private bool isLoading = false;
	private bool isSaving = false;
	private bool isUploading = false;
	private bool isEnabled = false;
	private bool scanCompleted = false;
	
	private int tapCount = 0;
	private float tapTime = 0;
	private string currentTrackedMarkerName = "";
	
	public static ExtendedImageTargetController instance;
	void Awake()
	{
		instance = this; 
		markerHelper = new GameObject("MarkerHelper");
		markerHelper.transform.SetParent(this.transform);
    }
	
	void Update(){
				
		HandleUpdate();
	}
	
	public void Init(){
		
		isEnabled = true;
		options.SetActive(true);
		
		ARController.instance.UpdateScanPlanesType( 
			AdminUIController.instance.planesToggle.isOn ? 
			ARController.ScanPlanesType.DefaultEdit : ARController.ScanPlanesType.Invisible 
		);
		ARController.instance.ShowScanAnimation();
	}
	
	public void Reset(){
		
		StopAllCoroutines();
		
		isEnabled = false;	
		scanCompleted = false;
		isLoading = false;
		isSaving = false;
		isUploading = false;
		syncedMarker = "";
		currentTrackedMarkerName = "";
		changedIDs.Clear();
		dataJson = null;

		DeleteObjects();
		options.SetActive(false);
		addButton.SetActive(false);
		saveButton.SetActive(false);
		uploadButton.SetActive(false);
		deleteButton.SetActive(false);
		saveButton.GetComponentInChildren<TextMeshProUGUI>().text = "Save";
	}
	
	public void HandleUpdate(){
		
		if( !isEnabled ) return;
			
		if( ARController.instance.isScanning ){
			return;
		}
		else{
			//addButton.SetActive(true);
		}
		
		if( currentSelectedObject ){
			deleteButton.SetActive(true);	
		}else{
			deleteButton.SetActive(false);
		}

		// Show/Hide saveButton
		if( IsTracking() && imageTargetObjects.Count > 0 ){
			
			if( EditStationController.instance.scanningState > 5 ){
				saveButton.SetActive(true);	
			}
			
			if( EditStationController.instance.scanningState == 6 ){
				EditStationController.instance.scanInfoLabel.text = "Marker wurde gescannt!";
			}
			
		}else{
			
			saveButton.SetActive(false);
			
			if( EditStationController.instance.scanningState == 6 ){
				EditStationController.instance.scanInfoLabel.text = LanguageController.GetTranslation("Scanne den Marker!");
			}
		}
		
		// Show/Hide uploadButton
		if( !isUploading && dataJson != null && dataJson[currentTrackedMarkerName] != null ){
			
			if( EditStationController.instance.scanningState > 5 ){
				uploadButton.SetActive(true);	
			}
			
		}else{
			
			uploadButton.SetActive(false);
		}
		
		MoveObject();
		RotateObject();
		SelectObject();
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			if( imageTargets[i].trackingState == TrackingState.Tracking ){
				if( markerPrefix == "testing" ){
					AlignObjects(imageTargets[i]);
				}
			}
		}
	}

	public bool IsTracking(){
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			if( imageTargets[i].trackingState == TrackingState.Tracking ){
				return true;
			}
		}
		return false;
	}
	
	public void AddObject(){
		
		string objID = Guid.NewGuid().ToString();
		string savePath = "Models/Cylinder";

		if( savePath == "" ) return;
		//print("AddObject, objID " + objID);
		
		PlaceObject();
		
		GameObject prefab = Resources.Load( savePath, typeof(GameObject)) as GameObject;
					
		if( prefab != null ){
					
			GameObject obj = Instantiate(prefab);
			obj.name = prefab.name;
			obj.transform.SetParent(mainCamera);
			obj.SetActive(true);
			
			obj.transform.position = mainCamera.position + mainCamera.forward*1.5f + new Vector3(0, -0.5f, 0);
			obj.transform.eulerAngles = Vector3.zero;
		
			obj.GetComponent<ImageTargetObject>().id = objID;
			obj.GetComponent<ImageTargetObject>().savePath = savePath;
			
			obj.transform.SetParent(null);
			currentSelectedObject = obj;			
			imageTargetObjects.Add(obj);
			
			if( EditStationController.instance.scanningState == 3 ){
				EditStationController.instance.ContinueScanStep();
			}
			
			if( isEnabled && markerPrefix != "acorn"){
				GameObject child = ToolsController.instance.FindGameObjectByName(obj, "Cylinder");
				if( child != null ){ child.SetActive(true); }
			}
		}
	}
	
	public void PlaceObject(){
		
		if( currentSelectedObject == null ) return;
				
		if( !imageTargetObjects.Contains(currentSelectedObject) ){
			imageTargetObjects.Add(currentSelectedObject);
		}
		
		currentSelectedObject.transform.SetParent(null);
		currentSelectedObject = null;
	}
	
	public void SelectObject(){
		
		if ( Input.GetMouseButtonDown(0) ){ 
		
			RaycastHit hit; 
			Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition); 
			if ( Physics.Raycast (ray, out hit)) {
		
				if( hit.transform.CompareTag("Placeable") ){
					
					currentSelectedObject = hit.transform.gameObject;
					
					if( currentSelectedObject.GetComponent<ImageTargetObject>().id != "" &&
						!changedIDs.Contains( currentSelectedObject.GetComponent<ImageTargetObject>().id )
					){
						changedIDs.Add(currentSelectedObject.GetComponent<ImageTargetObject>().id);
					}
				}
			}
		}
	}
	
	public void FindObject(){
		
		for( int i = 0; i < imageTargetObjects.Count; i++ ){
			
			float dist = Vector3.Distance( imageTargetObjects[i].transform.position, mainCamera.position );
			
			if( dist > minDistanceBeforeVisible ){
				
				imageTargetObjects[i].GetComponent<ImageTargetObject>().isVisible = false;
			}
			else{
				
				imageTargetObjects[i].GetComponent<ImageTargetObject>().isVisible = true;
			}					
		}
	}
	
	public void DeleteObject(){
		
		if( currentSelectedObject == null ) return;
		
		if( imageTargetObjects.Contains(currentSelectedObject) ){
			
			imageTargetObjects.Remove(currentSelectedObject);
			
			if( currentSelectedObject.GetComponent<ImageTargetObject>().id != "" &&
				!changedIDs.Contains( currentSelectedObject.GetComponent<ImageTargetObject>().id )
			){
				changedIDs.Add(currentSelectedObject.GetComponent<ImageTargetObject>().id);
			}
		}
		
		Destroy(currentSelectedObject);
		currentSelectedObject = null;
	}
	
	public void DeleteObjects(){
		
		currentSelectedObject = null;
		
		for( int i = 0; i < imageTargetObjects.Count; i++ ){	
			Destroy( imageTargetObjects[i] );
		}
		imageTargetObjects.Clear();
	}
	
	public void SaveObjects(){
				
		if( isSaving ) return;
		isSaving = true;
		StartCoroutine( SaveCoroutine() );
	}
	
	public IEnumerator SaveCoroutine(){
		
		Save();
		
		string savePath = Application.persistentDataPath + "/" + markerPrefix + "_markerData.json";
		if( File.Exists(savePath) ){

			saveButton.GetComponentInChildren<TextMeshProUGUI>().text = "Saved";
			InfoController.instance.ShowInfo("Daten erfolgreich gespeichert!", 3.0f, true, true);
			
		}else{
			InfoController.instance.ShowInfo("Fehler beim Speichern der Daten", 3.0f, true, true);			
		}
		
		yield return new WaitForSeconds(2.0f);
		saveButton.GetComponentInChildren<TextMeshProUGUI>().text = "Save";
		
		if( EditStationController.instance.scanningState == 6 ){
			EditStationController.instance.ContinueScanStep();
		}
		isSaving = false;
	}
	
	public void Save(){
		
		dataJson = JSONNode.Parse("{}");

		for( int i = 0; i < imageTargets.Count; i++ ){
					
			if( !imageTargets[i].wasTracked ) continue;
			if( !imageTargets[i].isTracking ) continue;
			
			markerHelper.transform.position = imageTargets[i].markerHelper.transform.position;
			markerHelper.transform.eulerAngles = imageTargets[i].markerHelper.transform.eulerAngles;
			
			for( int j = 0; j < imageTargetObjects.Count; j++ ){
			
				JSONNode dataNode = JSONNode.Parse("{}");			
				imageTargetObjects[j].transform.SetParent(markerHelper.transform);
				dataNode["xPos"] = imageTargetObjects[j].transform.localPosition.x.ToString();
				dataNode["yPos"] = imageTargetObjects[j].transform.localPosition.y.ToString();
				dataNode["zPos"] = imageTargetObjects[j].transform.localPosition.z.ToString();
				dataNode["xRot"] = imageTargetObjects[j].transform.localEulerAngles.x.ToString();
				dataNode["yRot"] = imageTargetObjects[j].transform.localEulerAngles.y.ToString();
				dataNode["zRot"] = imageTargetObjects[j].transform.localEulerAngles.z.ToString();
				dataNode["id"] = imageTargetObjects[j].GetComponent<ImageTargetObject>().id;
				dataNode["savePath"] = imageTargetObjects[j].GetComponent<ImageTargetObject>().savePath;
			
				dataJson[imageTargets[i].id]["objects"].Add(dataNode);
				
				imageTargetObjects[j].transform.SetParent(null);
			}
			
			currentTrackedMarkerName = imageTargets[i].id;
		}

		string savePath = Application.persistentDataPath + "/" + markerPrefix + "_markerData.json";
		File.WriteAllText(savePath, dataJson.ToString());		
	}
	
	public void Save( ImageTarget imageTarget ){
		
		//dataJson = JSONNode.Parse("{}");

		for( int i = 0; i < imageTargets.Count; i++ ){
					
			if( !imageTargets[i].wasTracked ) continue;
			if( imageTargets[i] != imageTarget ) continue;
			
			markerHelper.transform.position = imageTargets[i].markerHelper.transform.position;
			markerHelper.transform.eulerAngles = imageTargets[i].markerHelper.transform.eulerAngles;
			
			JSONNode objectsNode = JSONNode.Parse("{\"objects\":[]}");		
			dataJson[imageTargets[i].id] = objectsNode;
			
			for( int j = 0; j < imageTargetObjects.Count; j++ ){
			
				JSONNode dataNode = JSONNode.Parse("{}");			
				imageTargetObjects[j].transform.SetParent(markerHelper.transform);
				dataNode["xPos"] = imageTargetObjects[j].transform.localPosition.x.ToString();
				dataNode["yPos"] = imageTargetObjects[j].transform.localPosition.y.ToString();
				dataNode["zPos"] = imageTargetObjects[j].transform.localPosition.z.ToString();
				dataNode["xRot"] = imageTargetObjects[j].transform.localEulerAngles.x.ToString();
				dataNode["yRot"] = imageTargetObjects[j].transform.localEulerAngles.y.ToString();
				dataNode["zRot"] = imageTargetObjects[j].transform.localEulerAngles.z.ToString();
				dataNode["id"] = imageTargetObjects[j].GetComponent<ImageTargetObject>().id;
				dataNode["savePath"] = imageTargetObjects[j].GetComponent<ImageTargetObject>().savePath;
				
				dataJson[imageTargets[i].id]["objects"].Add(dataNode);
				
				imageTargetObjects[j].transform.SetParent(null);
			}
		}

		string savePath = Application.persistentDataPath + "/" + markerPrefix + "_markerData.json";
		File.WriteAllText(savePath, dataJson.ToString());		
	}
	
	public void AlignObjects( ImageTarget imageTarget ){
		
		if( isLoading ) return;
		isLoading = true;
		StartCoroutine( AlignObjectsToMarkerCoroutine(imageTarget) );
	}
	
	public IEnumerator AlignObjectsToMarkerCoroutine(ImageTarget imageTarget){
		
		yield return new WaitForSeconds( 0.1f );
		
		AlignObjectsToMarker(imageTarget);
		
		yield return new WaitForSeconds( 0.5f );
		isLoading = false;
	}
	
	public void LoadMarkerJson(){
		
		if( dataJson == null ){
			
			if( usePositionsFromResources ){
				
				string path = markerPrefix + "_markerData";
				TextAsset textAsset = Resources.Load<TextAsset>(path);
				if( textAsset != null ){
					dataJson = JSONNode.Parse(textAsset.text);
				}
				else{
					
					string savePath = Application.persistentDataPath + "/" + markerPrefix + "_markerData.json";
					if( File.Exists(savePath) ){
						dataJson = JSONNode.Parse( File.ReadAllText(savePath) );
					}
					else{
						dataJson = JSONNode.Parse("{}");
					}
				}
			}
			else{
				
				string savePath = Application.persistentDataPath + "/" + markerPrefix + "_markerData.json";
				if( File.Exists(savePath) ){
					dataJson = JSONNode.Parse( File.ReadAllText(savePath) );
				}
				else{
					
					string path = markerPrefix + "_markerData";
					TextAsset textAsset = Resources.Load<TextAsset>(path);
					if( textAsset != null ){
						dataJson = JSONNode.Parse(textAsset.text);
					}
					else{
						dataJson = JSONNode.Parse("{}");						
					}
				}
			}
		}
	}
	
	public void AlignObjectsToMarker( ImageTarget imageTarget ){
		
		LoadMarkerJson();
		
		markerHelper.transform.position = imageTarget.transform.position;
		markerHelper.transform.eulerAngles = imageTarget.transform.eulerAngles;
	
		string markerId = imageTarget.id;
		if( markerId == "universal" && dataJson[markerId] == null ){ 
			if( markerPrefix == "reactor" ){
				markerId = "reactor";
			}else if( markerPrefix == "acorn" ){
				markerId = "acorn";
			}
		}
		if( dataJson[markerId] == null && dataJson["universal"] != null ){ 
			markerId = "universal";
		}
		
		if( dataJson[markerId] != null ){
			
			syncedMarker = markerId;
			
			for( int i = 0; i < dataJson[markerId]["objects"].Count; i++ ){
			
				if( changedIDs.Contains(dataJson[markerId]["objects"][i]["id"].Value) ) continue;

				GameObject obj = GetObjectWithID( 
					dataJson[markerId]["objects"][i]["id"].Value,
					dataJson[markerId]["objects"][i]["savePath"].Value
					);

				if( obj != null ){
				
					obj.transform.SetParent(markerHelper.transform);
				
					float xPos = GetFloatValueFromJsonNode(dataJson[markerId]["objects"][i]["xPos"]);
					float yPos = GetFloatValueFromJsonNode(dataJson[markerId]["objects"][i]["yPos"]);
					float zPos = GetFloatValueFromJsonNode(dataJson[markerId]["objects"][i]["zPos"]);
					obj.transform.localPosition = new Vector3( xPos, yPos, zPos );
				
					float xRot = GetFloatValueFromJsonNode(dataJson[markerId]["objects"][i]["xRot"]);
					float yRot = GetFloatValueFromJsonNode(dataJson[markerId]["objects"][i]["yRot"]);
					float zRot = GetFloatValueFromJsonNode(dataJson[markerId]["objects"][i]["zRot"]);
					
					obj.transform.localEulerAngles = new Vector3( xRot, yRot, zRot );
					
					obj.transform.SetParent(null);
					FixCurvedLabels(obj);
				}		
			}
		}		
	}
	
	public void FixCurvedLabels( GameObject obj ){
		
		ntw.CurvedTextMeshPro.TextProOnACircle[] curvedLabels = obj.GetComponentsInChildren<ntw.CurvedTextMeshPro.TextProOnACircle>();
		for( int i = 0; i<curvedLabels.Length; i++){
			curvedLabels[i].m_forceUpdate = true;
		}
	}
	
	public GameObject GetObjectWithID( string id, string savePath ){
				
		for( int i = 0; i < imageTargetObjects.Count; i++ ){
			
			if( imageTargetObjects[i].GetComponent<ImageTargetObject>().id == id ){
				return imageTargetObjects[i];
			}
		}
		
		GameObject prefab = Resources.Load( savePath, typeof(GameObject)) as GameObject;
		if( prefab != null ){
			
			GameObject obj = Instantiate(prefab);
			obj.name = prefab.name;
			obj.SetActive(true);
			obj.GetComponent<ImageTargetObject>().id = id;
			obj.GetComponent<ImageTargetObject>().savePath = savePath;
			imageTargetObjects.Add(obj);
			
			UpdateObjectForStation( obj );
			return obj;
		}
		
		return null;
	}
	
	public void UpdateObjectForStation( GameObject obj ){

	}
	
	public float GetFloatValueFromJsonNode( JSONNode node ){
		
		string valueString = node.Value.Replace(',', '.');
		
		float val = 0;
		bool parsed = float.TryParse( valueString, 	NumberStyles.Float, CultureInfo.InvariantCulture, out val );
	
		if( parsed ){
			return val;
		}
		
		return node.AsFloat;
	}
	
	public void RotateObject(){
		
		if( currentSelectedObject == null ) return;
		
		if (Input.touchCount == 2)
		{
			var touch1 = Input.GetTouch(0);
			var touch2 = Input.GetTouch(1);

			Vector2 prevPos1 = touch1.position - touch1.deltaPosition;
			Vector2 prevPos2 = touch2.position - touch2.deltaPosition;
			Vector2 prevDir = prevPos2 - prevPos1;
			Vector2 currDir = touch2.position - touch1.position;

			float angle = 0;
			if (prevPos1.y < prevPos2.y)
			{
				if (prevDir.x < currDir.x)
				{
					angle = Vector2.Angle(prevDir, currDir);
				}
				else
				{
					angle = -Vector2.Angle(prevDir, currDir);
				}
			}
			else
			{
				if (prevDir.x < currDir.x)
				{
					angle = -Vector2.Angle(prevDir, currDir);
				}
				else
				{
					angle = Vector2.Angle(prevDir, currDir);
				}
			}
			currentSelectedObject.transform.Rotate(0, angle, 0);
		}
	}
	
	public void MoveObject(){
		
		if( currentSelectedObject == null ) return;
		
		if ( Input.GetMouseButton(0) && !ToolsController.instance.IsPointerOverUIObject() && !HitPlaceable() )
		{
			RaycastHit[] hits;
			Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
			hits = Physics.RaycastAll(ray, 100, collisionLayerMaskModel);

			if (hits.Length > 0)
			{
				currentSelectedObject.transform.position = hits[0].point;
			}
		}
	}
	
	bool HitPlaceable()
	{
		RaycastHit[] hits;
		Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
		hits = Physics.RaycastAll(ray, 100);

		for( int i = 0; i < hits.Length; i++ ){
			if (hits[i].transform.CompareTag("Placeable") && hits[i].transform.gameObject != currentSelectedObject)
			{
				return true;
			}
		}
		
		return false;
	}
	
	public void UploadObjects(){
				
		if( isUploading ) return;
		isUploading = true;
		StartCoroutine( UploadObjectsCoroutine() );
	}
	
	public IEnumerator UploadObjectsCoroutine(){
		
		if( dataJson == null ){
			
			InfoController.instance.ShowInfo("Keine Daten zum Hochladen vorhanden", 5.0f, true, true);
			isUploading = false;
			yield break;
		}
		
		JSONNode uploadData = JSONNode.Parse("{}");
		
		uploadData["stationId"] = TestController.instance.GetCurrentStationId();

		// Todo: use correct stationId, make sure stationId exists in Drupal
		if( TestController.instance.GetCurrentStationId() == "acornEdit" ){
			
			//uploadData["stationId"] = "7";
			uploadData["stationId"] = "acorn";
			
		}else if( TestController.instance.GetCurrentStationId() == "reactorEdit" ){
			
			//uploadData["stationId"] = "21";
			uploadData["stationId"] = "reactor";
		}
		
		for( int i = 0; i < dataJson[currentTrackedMarkerName]["objects"].Count; i++ ){
			
			JSONNode dataNode = JSONNode.Parse("{}");

			dataNode["objectId"] = dataJson[currentTrackedMarkerName]["objects"][i]["id"].Value;			
			dataNode["title"] = dataJson[currentTrackedMarkerName]["objects"][i]["id"].Value;
			
			dataNode["posX"] = dataJson[currentTrackedMarkerName]["objects"][i]["xPos"].Value.Replace(",", ".");
			dataNode["posY"] = dataJson[currentTrackedMarkerName]["objects"][i]["yPos"].Value.Replace(",", ".");
			dataNode["posZ"] = dataJson[currentTrackedMarkerName]["objects"][i]["zPos"].Value.Replace(",", ".");
			dataNode["rotX"] = dataJson[currentTrackedMarkerName]["objects"][i]["xRot"].Value.Replace(",", ".");
			dataNode["rotY"] = dataJson[currentTrackedMarkerName]["objects"][i]["yRot"].Value.Replace(",", ".");
			dataNode["rotZ"] = dataJson[currentTrackedMarkerName]["objects"][i]["zRot"].Value.Replace(",", ".");
			
			uploadData["objects"].Add(dataNode);
		}
		
		print( "Data to upload: " + uploadData.ToString() );

		bool isSuccess = false;
		string callbackInfo = "";

		/*
		// Upload single object
		bool oneUploadFailed = false;

		for( int i = 0; i < uploadData["objects"].Count; i++ ){
			
			print("Uploading data: " + uploadData["objects"][i].ToString());
		
			
			yield return StartCoroutine(
				ServerBackendController.instance.UploadObjectsCoroutine(uploadData["objects"][i], (bool success, string data) => {		
					isSuccess = success;
					if( !isSuccess ){
						oneUploadFailed = true;
						callbackInfo = data;
					}
				})
			);

		}
		
		if( !oneUploadFailed ){
			InfoController.instance.ShowInfo("Daten erfolgreich hochgeladen!", 5.0f, true, true);
		}else{
			InfoController.instance.ShowInfo("Fehler beim Hochladen der Daten " + callbackInfo, 5.0f, true, true);
		}
		*/
		
		// Upload all objects ?!
		
		/*
		yield return StartCoroutine(
			ServerBackendController.instance.UploadObjectsCoroutine(uploadData, (bool success, string data) => {		
				isSuccess = success;
				callbackInfo = data;
			})
		);
		
		if( isSuccess ){
			InfoController.instance.ShowInfo("Daten erfolgreich hochgeladen!", 3.0f, true, true);
		}else{
			InfoController.instance.ShowInfo("Fehler beim Hochladen der Daten " + callbackInfo, 3.0f, true, true);
		}
		*/
		
		isUploading = false;
	}
}
