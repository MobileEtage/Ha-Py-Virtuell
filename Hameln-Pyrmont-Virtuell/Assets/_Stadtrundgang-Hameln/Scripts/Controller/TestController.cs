using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using TMPro;
using SimpleJSON;

public class TestController : MonoBehaviour
{
	public ARSession arSession;
	public GameObject mainCamera;
	public GameObject scanFrameRoot;

	[Space(10)]

    public bool moveEnabled = false;
    public int tapCount = 0;
    public float tapTime = 0;

    private bool isLoading = false;
	private bool isScanningStationMarker = false;
	private string currentStationID = "";
	private string currentTrackedMarkerId = "";
	
	private Vector3 offset = Vector3.zero;
	private bool selected = false;
    private GameObject currentObject;

    public static TestController instance;
	void Awake(){

		instance = this;
	}
	
	void Start()
	{
	}
	
	void Update(){

        /*
		#if UNITY_EDITOR
		if( Input.GetKey(KeyCode.UpArrow) ){
			mainCamera.transform.position += new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z)*Time.deltaTime;
		}
		else if( Input.GetKey(KeyCode.DownArrow) ){
			mainCamera.transform.position -= new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z)*Time.deltaTime;
		}
	    #endif
        */

        if (SiteController.instance != null && SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "ImprintSite") return;
		DetectTap();
    }

    public void DetectTap()
	{
		tapTime += Time.deltaTime;
		if(tapTime > 1.0f) { tapCount = 0; }
	}

	public void Tap(string id)
	{
		tapTime = 0;
		tapCount++;
		if (tapCount >= 5)
		{
			if (id == "TestAudiothek") { TestAudiothek(); }
			else if (id == "TestAvatarGuide") { TestAvatarGuide(); }
			else if ( id == "TestFeatures" ) { TestFeatures(); }
			else if ( id == "UnlockAdmin" ) { UnlockAdmin(); }
			tapCount = 0;
		}
	}

	public void TestAudiothek()
	{
		if (isLoading) return;
		isLoading = true;
		StartCoroutine(TestAudiothekCoroutine());
	}

	public IEnumerator TestAudiothekCoroutine()
	{
		if (AudiothekController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("AudiothekSite")); }
		yield return StartCoroutine(AudiothekController.instance.InitCoroutine());
		yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("AudiothekSite"));

		isLoading = false;
	}

	public void TestAvatarGuide()
	{
		if (isLoading) return;
		isLoading = true;
		StartCoroutine(TestAvatarGuideCoroutine());
	}

	public IEnumerator TestAvatarGuideCoroutine()
	{
		if (AvatarGuideController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("AvatarGuideSite")); }
		yield return StartCoroutine(AvatarGuideController.instance.InitCoroutine());
		yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("AvatarGuideSite"));

		isLoading = false;
	}

	public void TestFeatures()
	{
		if (isLoading) return;
		isLoading = true;
		StartCoroutine(TestFeaturesCoroutine());
	}

	public IEnumerator TestFeaturesCoroutine()
	{
		if (TestFeaturesController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("TestFeaturesSite")); }
		yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("TestFeaturesSite"));

		isLoading = false;
	}

	public void UnlockAdmin()
	{
		AdminUIController.instance.ShowAdminPasswordMenu();
	}

	public void TestARFeature( string id, string markerId ){
		
		if( isLoading ) return;
		isLoading = true;
		
		currentTrackedMarkerId = markerId;
		StartCoroutine( CheckRequiredPermissionsCoroutine(id) );
	}
	
	public void TestARFeature( string id ){
		
		if( isLoading ) return;
		isLoading = true;
		
		currentTrackedMarkerId = id;
		StartCoroutine( CheckRequiredPermissionsCoroutine(id) );
	}
	
	public IEnumerator CheckRequiredPermissionsCoroutine( string id ){
		
		bool cameraRequired = false;
		bool gpsRequired = false;
		bool microphoneRequired = false;
		bool hasPermissionCamera = false;
		bool hasPermissionGPS = false;
		bool hasPermissionMicrophone = false;

		cameraRequired = true;
		yield return StartCoroutine(
			PermissionController.instance.ValidatePermissionsCameraCoroutine("ar", (bool success) => {		
				hasPermissionCamera = success;
			})
		);
		
		/*
		if( AdminUIController.instance.NeedsPermission(id, "cameraPermissionRequired")){
			
			cameraRequired = true;
			yield return StartCoroutine(
				PermissionController.instance.ValidatePermissionsCameraCoroutine("ar", (bool success) => {		
					hasPermissionCamera = success;
				})
			);
		}
		
		if( AdminUIController.instance.NeedsPermission(id, "gpsPermissionRequired") ){
			
			gpsRequired = true;
			yield return StartCoroutine(
				PermissionController.instance.ValidatePermissionsGPSCoroutine((bool success) => {		
					hasPermissionGPS = success;
				})
			);
		}
		
		if( AdminUIController.instance.NeedsPermission(id, "microphonPermissionRequired") ){
			
			//microphoneRequired = true;
			yield return StartCoroutine(
				PermissionController.instance.ValidatePermissionsMicrophoneCoroutine((bool success) => {
					hasPermissionMicrophone = success;
				})
			);
						
			if( !hasPermissionMicrophone ){
				
				float timer = 5.0f;
				while( timer > 0 && 
				(
					InfoController.instance.info.activeInHierarchy || 
					InfoController.instance.commitAbortDialog.activeInHierarchy
				)
				) {
					
					timer -= Time.deltaTime;
					yield return new WaitForEndOfFrame();
				}
				InfoController.instance.commitAbortDialog.SetActive(false);
			}
		}
		*/
		
		bool allOkay = true;
		if( cameraRequired && !hasPermissionCamera ){ allOkay = false; }
		if( gpsRequired && !hasPermissionGPS ){ allOkay = false; }
		if( microphoneRequired && !hasPermissionMicrophone ){ allOkay = false; }
		
		if( allOkay ){

			ARController.instance.UpdateScanPlanesType(ARController.ScanPlanesType.Wireframe);
			yield return StartCoroutine( ScanCoroutine(id) );
			
			/*
			if( AdminUIController.instance.ShouldScan(id) ){
	
				//ARController.instance.UpdateScanPlanesType(ARController.ScanPlanesType.UserAnimation);
				ARController.instance.UpdateScanPlanesType(ARController.ScanPlanesType.Wireframe);
				yield return StartCoroutine( ScanCoroutine(id) );
				
			}else{
				
				ARController.instance.UpdateScanPlanesType(ARController.ScanPlanesType.Invisible);
				yield return StartCoroutine( TestARFeatureCoroutine(id) );
			}
			*/
		}
		else{
			isLoading = false;
		}
	}
	
	public IEnumerator ScanCoroutine( string id ){

		//InfoController.instance.blocker.SetActive(true);

		ARController.instance.InitARFoundation();
		
		//InfoController.instance.loadingCircle.SetActive(true);
		//yield return new WaitForSeconds(1.5f);
		//InfoController.instance.loadingCircle.SetActive(false);

		OffCanvasController.instance.CloseMenu();
		yield return StartCoroutine( SiteController.instance.SwitchToSiteCoroutine("ScanSite") );
		
		ARController.instance.ShowScanAnimation();
		while( ARController.instance.isScanning ){
			yield return new WaitForEndOfFrame();
		}
		yield return new WaitForSeconds(1.0f);
		
		ARController.instance.ShowHidePlanes(false);
		yield return StartCoroutine( TestARFeatureCoroutine(id, false) );
		
		//InfoController.instance.blocker.SetActive(false);
		isLoading = false;
	}
	
	public void TestARFeatureDirectly( string id ){
		
		if( isLoading ) return;
		isLoading = true;
		StartCoroutine( TestARFeatureCoroutine(id) );
	}
	
	public IEnumerator TestARFeatureCoroutine( string id, bool shouldSwitchSite = true ){

        print("TestARFeatureCoroutine");

        InfoController.instance.blocker.SetActive(true);
		
		currentStationID = id;
		string targetSite = "ScanSite";
		//LightController.instance.SetLightSettings(id, currentTrackedMarkerId);
		
		switch(id){
			
		case "measure":
			MeasureController.Instance.measureOptions.SetActive(true);
			MeasureController.Instance.EnableDisableMeasurement(true);
			break;
		case "path":
			MeasureController.Instance.EnableDisableMeasurement(true);
			PathController.Instance.Init();
			break;
		case "extendedTracking":
			ExtendedImageTargetController.instance.markerPrefix = "testing";
			ExtendedImageTargetController.instance.Init();
			break;
		case "map":
			//MapCaptureController.instance.Init();
			MapController.instance.Init();
			targetSite = "MapSite";
			break;
		default: break;
		
		}
		
		OffCanvasController.instance.CloseMenu();
		if( shouldSwitchSite ){
		
			if( targetSite == "ScanSite" ||
				targetSite == "TutorialSite"
			){
				ARController.instance.InitARFoundation();
			}
			
			InfoController.instance.loadingCircle.SetActive(true);
			yield return new WaitForSeconds(1.5f);
			InfoController.instance.loadingCircle.SetActive(false);
	
			yield return StartCoroutine( SiteController.instance.SwitchToSiteCoroutine(targetSite) );
		}
		
		if( currentStationID != "map" ){
			MapController.instance.Reset();				
		}
		
		InfoController.instance.blocker.SetActive(false);
		isLoading = false;
	}

	public void PlaceObject( GameObject obj ){
		
		Vector2 touchPosition = new Vector2( Screen.width *0.5f, Screen.height*0.5f );
		Vector3 hitPosition = mainCamera.transform.position + mainCamera.transform.forward*2;
		if ( ARController.instance != null && ARController.instance.RaycastHit( touchPosition, out hitPosition ) )
		{
			
		}
		else{

			Ray rayTemp = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition); 
			hitPosition = rayTemp.origin + rayTemp.direction*2.0f;		
		}
		
		obj.transform.position = hitPosition;
		Vector3 rot = obj.transform.eulerAngles;
		obj.transform.LookAt(mainCamera.transform);
		obj.transform.eulerAngles = new Vector3( rot.x, obj.transform.eulerAngles.y+180, rot.z );
	}
	
	public void MoveObject( GameObject obj ){
		
		if( Input.touchCount == 1 || Application.isEditor ){
			
			if( Input.GetMouseButtonDown(0) && !ToolsController.instance.IsPointerOverUIObject() ){
				
				Vector2 touchPosition = ToolsController.instance.GetTouchPosition();
				Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
				RaycastHit[] hits;
				hits = Physics.RaycastAll(ray, 100);
				
				for (int i = 0; i < hits.Length; i++)
				{
					if( hits[i].transform.gameObject == currentObject ){
						
						selected = false;
						moveEnabled = true;
						break;
					}
				}
			}
			else if( Input.GetMouseButton(0) && moveEnabled ){
				
				Vector2 touchPosition = ToolsController.instance.GetTouchPosition();
				Vector3 hitPosition = mainCamera.transform.position + mainCamera.transform.forward*2;
				if ( ARController.instance != null && ARController.instance.RaycastHit( touchPosition, out hitPosition ) )
				{
					if( !selected ){
						
						selected = true;
						offset = obj.transform.position-hitPosition;
					}
					
					obj.transform.position = hitPosition+offset;
					
					//Vector3 rot = obj.transform.eulerAngles;
					//obj.transform.LookAt(mainCamera.transform);
					//obj.transform.eulerAngles = new Vector3( rot.x, obj.transform.eulerAngles.y, rot.z );
				}
				else{
					
					#if UNITY_EDITOR
			
					Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(touchPosition);
					RaycastHit[] hits;
					hits = Physics.RaycastAll(ray, 100);
				
					for (int i = 0; i < hits.Length; i++)
					{
						if( hits[i].transform.gameObject == ARController.instance.singlePlane ){
							
							obj.transform.position = hits[i].point;
							//Vector3 rot = obj.transform.eulerAngles;
							//obj.transform.LookAt(mainCamera.transform);
							//obj.transform.eulerAngles = new Vector3( rot.x, obj.transform.eulerAngles.y, rot.z );
							break;
						}
					}
			
				#endif
				}
			}
			else if( Input.GetMouseButtonUp(0) ){
			
				moveEnabled = false;
			}
		}
		else{
			
			moveEnabled = false;
		}
	}
	
	public void Reset(){

		StopAllCoroutines();
		
		if( ARController.instance.isScanning ){ ARController.instance.HideScanAnimation(); }
		ARController.instance.ShowHidePlanes( false );

		scanFrameRoot.SetActive(false);
		ResetCurrentStation();
		currentStationID = "";
		
		InfoController.instance.blocker.SetActive(false);
		isLoading = false;
	}
	
	public void ResetCurrentStation(){
		
		switch(currentStationID){
	
		default: break;
		
		}

		if (StationVideoController.instance != null) { StationVideoController.instance.Reset(); }
		ExtendedImageTargetController.instance.Reset();
		EditStationController.instance.Reset();
	}
	
	public void BackToAROptions(){

		/*
		Reset();
		
		MapController.instance.Init();
		
		SiteController.instance.DeActivateSite("TutorialSite");
		SiteController.instance.SwitchToSite("MapSite");
		//SiteController.instance.SwitchToSite("DashboardSite");
		
		ARController.instance.StopAndResetARSession();
		*/
	}
	
	public void Abort(){

		print("Abort");
		InfoController.instance.ShowCommitAbortDialog("Möchtest Du die Station beenden?", BackToAROptions );		
	}
	
	public void SwitchToScanStationMarkerSite(){
		
		if( isLoading ) return;
		isLoading = true;
		StartCoroutine( SwitchToScanStationMarkerSiteCoroutine() );
	}
	
	public IEnumerator SwitchToScanStationMarkerSiteCoroutine(){
		
		ARController.instance.InitARFoundation();
			
		InfoController.instance.loadingCircle.SetActive(true);
		yield return new WaitForSeconds(1.5f);
		InfoController.instance.loadingCircle.SetActive(false);
		yield return StartCoroutine( SiteController.instance.SwitchToSiteCoroutine("StationMarkerScanSite") );
		
		MapController.instance.Reset();	
		isScanningStationMarker = true;
		isLoading = false;
	}
	
	public void AbortStationMarkerScan(){
		
		if( isLoading ) return;
		isLoading = true;
		StartCoroutine( AbortStationMarkerScanCoroutine() );
	}
	
	public IEnumerator AbortStationMarkerScanCoroutine(){

		print("AbortStationMarkerScanCoroutine");

		isScanningStationMarker = false;
		MapController.instance.Init();	
		yield return StartCoroutine( SiteController.instance.SwitchToSiteCoroutine("MapSite") );
		ARController.instance.StopAndResetARSession();
		
		isLoading = false;
	}

	public bool IsValidTourMarker( string markerId ){
		
		//return TourController.instance.IsValidTourMarker(markerId);
		return true;
	}

	public void StartStation(){
		
		if( isLoading ) return;
		isLoading = true;
		StartCoroutine( StartStationCoroutine() );
	}
	
	public IEnumerator StartStationCoroutine(){

		yield return StartCoroutine( CheckRequiredPermissionsCoroutine( currentStationID ) );
	}
	
	public void OnCloseStationInfo(){
		
		if( SiteController.instance.currentSite != null &&
			SiteController.instance.currentSite.siteID == "StationMarkerScanSite"
		){
			isScanningStationMarker = true;
		}
	}
	
	public void SetCurrentStationID(string id){
		currentStationID = id;
	}
	
	public string GetCurrentStationId(){
		return currentStationID;
	}
	
	public string GetCurrentMarkerId(){
		return currentTrackedMarkerId;
	}
	
	public string GetStationIdFromMarkerId( string markerId ){
		
		if( markerId == "universal" ){ return "Rosalotta"; }
		return markerId;
	}

}
