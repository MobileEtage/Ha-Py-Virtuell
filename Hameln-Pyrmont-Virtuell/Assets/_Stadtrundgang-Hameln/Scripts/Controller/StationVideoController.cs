using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using Unity.AI.Navigation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

public class StationVideoController : MonoBehaviour
{
	private List<string> markerIds = new List<string>()
	{ "universal", "deadwood", "mushrooms", "mammals" };
	private ImageTarget currentTrackedImageTarget;
	
	private bool disableAR = true;
	public GameObject scanFrame;
	public GameObject infoContent;
	public GameObject videoSite;
	private Transform mainCamera;

	[Space(10)]

	private string stationId = "";
	private string markerId = "";
	private bool isEnabled = false;
	private bool markerScanned = false;
	private bool interactionEnabled = false;
	private Vector3 camPosition = new Vector3(-1000, -1000, -1000);

	private float stationTimer = 0;
	private float abortTime = 30;
	
	public static StationVideoController instance;
	void Awake(){
		instance = this;
	}

	void Start(){
		
	}
	
	void Update()
	{
		if( !isEnabled ) return;

		if( interactionEnabled && markerScanned ){
			
			Interact();
			stationTimer += Time.deltaTime;
		}
		
		currentTrackedImageTarget = ImageTargetController.Instance.GetTrackedImageTarget(markerIds);
		if( currentTrackedImageTarget != null ){
	    	
			this.markerId = currentTrackedImageTarget.id;
			
			if( !markerScanned ){
		    		
				markerScanned = true;
				scanFrame.SetActive(false);
				StartCoroutine("LoadContentCoroutine");
			}
			else{
					
				ImageTargetController.Instance.PlaceObjectToMarker(
					VideoController.instance.videoCanvas, currentTrackedImageTarget.transform, 
					new Vector3(0,0,0),
					new Vector3(0,0,0)
				);
			}
		}
	}
    
	public void Init(){

		mainCamera = TestController.instance.mainCamera.transform;

		#if UNITY_EDITOR
		camPosition = mainCamera.transform.position;
		mainCamera.transform.position = new Vector3(0, 1.7f, -1);
		mainCamera.transform.eulerAngles = new Vector3(60,0,0);
		#endif
	}
	
	public IEnumerator InitCoroutine(string stationId){

		print("StationVideoController InitCoroutine " + stationId);
		
		Init();
		this.stationId = stationId;
		
		yield return StartCoroutine( SiteController.instance.SwitchToSiteCoroutine("StationVideoSite") );
		//TutorialController.instance.InitTutorial(stationId);
		yield return StartCoroutine( SiteController.instance.ActivateSiteCoroutine("TutorialSite") );
	}
	
	public void EnableMarkerScan(){

		isEnabled = true;
		scanFrame.SetActive(true);
		
		#if UNITY_EDITOR		
		ImageTargetController.Instance.EnableImageTargetTrackingForEditorTest(markerIds);
		#endif
	}
	
	public void Reset(){
		
		StopAllCoroutines();

		isEnabled = false;
		markerScanned = false;
		interactionEnabled = false;
		scanFrame.SetActive(false);
		infoContent.SetActive(false);

		VideoController.instance.videoCanvas.SetActive(false);
		//VideoController.instance.videoPlayer.Stop();

		stationTimer = 0;
		
		#if UNITY_EDITOR
		if( camPosition.x != -1000 ){ 
			mainCamera.transform.position = camPosition; 
			mainCamera.transform.eulerAngles = Vector3.zero;
		}
		ImageTargetController.Instance.DisableImageTargetTracking();
		#endif		
		
	}
	
		
	public IEnumerator ShowInfoCoroutine(){

		infoContent.SetActive(true);
		yield return new WaitForSeconds(8.0f);
		infoContent.SetActive(false);
	}
	
	public IEnumerator LoadContentCoroutine(){
		
		yield return new WaitForEndOfFrame();	
		
		ImageTargetController.Instance.PlaceObjectToMarker(
			VideoController.instance.videoCanvas, currentTrackedImageTarget.transform, 
			new Vector3(0,0,0),
			new Vector3(0,0,0)
		);
		
		//VideoController.instance.currentVideoTarget.videoURL = "https://app-etagen.die-etagen.de/BadIburg/Videos/Dummy-Videoausspielung_BadIburgAR-2.mp4";
		
		//VideoController.instance.currentVideoTarget.videoURL = 
		//	StationController.instance.GetVideoURL(stationId, markerId);
		//videoSite.GetComponentInChildren<VideoTarget>(true).videoURL = 
		//	StationController.instance.GetVideoURL(stationId, markerId);
		
		VideoController.instance.currentVideoTarget.isLoaded = false;
		VideoController.instance.videoCanvas.SetActive(true);
		VideoController.instance.PlayVideo();
		if( disableAR ){ 
			
			ARController.instance.StopAndResetARSession();
			EnableFullscreen(); 
		}
		
		//string stationId = TestController.instance.GetCurrentStationId();
		MapController.instance.MarkStationFinished(stationId);
		
		StartCoroutine( ShowInfoCoroutine() );
		interactionEnabled = true;
	}
	

	public void Interact(){
				
		if( InfoController.instance.commitAbortDialog.activeInHierarchy ) return;
		if( videoSite.activeInHierarchy ) return;
		
		if ( Input.GetMouseButtonUp(0) )
		{
			RaycastHit[] hits;
			Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
			hits = Physics.RaycastAll(ray, 100);
			
			for( int i = 0; i < hits.Length; i++ ){
				
				if( hits[i].transform == VideoController.instance.currentVideoTarget.transform ){

					infoContent.SetActive(false);
					VideoController.instance.videoSite.SetActive(true);
					VideoController.instance.mainUIVideoTarget.targetImage.gameObject.SetActive(true);
					VideoController.instance.mainUIVideoTarget.videoNavigationFooter.SetActive(true);
					
					if( !VideoController.instance.mainUIVideoTarget.isFullscreen ){
						VideoController.instance.SwitchFullscreen(VideoController.instance.mainUIVideoTarget);
					}
					
					return;
				}
			}
		}
	}

	public void EnableFullscreen(){
		
		if( !InfoController.instance.commitAbortDialog.activeInHierarchy ){
			
			infoContent.SetActive(false);
			VideoController.instance.videoSite.SetActive(true);
			VideoController.instance.mainUIVideoTarget.targetImage.gameObject.SetActive(true);
			VideoController.instance.mainUIVideoTarget.videoNavigationFooter.SetActive(true);
					
			if( !VideoController.instance.mainUIVideoTarget.isFullscreen ){
				VideoController.instance.SwitchFullscreen(VideoController.instance.mainUIVideoTarget);
			}
		}
	}
	
	public void DisableFullscreen(){
		
		if( disableAR ){
			Abort();
		}
		else{
			videoSite.SetActive(false);
		}
	}
	
	public void Abort(){
		
		InfoController.instance.ShowCommitAbortDialog("Möchtest Du die Station beenden?", OnAbort );		
	}
	
	public void OnAbort(){
				
		videoSite.SetActive(false);
		isEnabled = false;
	}
}
