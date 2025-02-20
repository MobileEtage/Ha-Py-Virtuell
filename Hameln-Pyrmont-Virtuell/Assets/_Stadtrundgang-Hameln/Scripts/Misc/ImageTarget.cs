using System;
using System.Collections;
using UnityEngine;
using SimpleJSON;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

public class ImageTarget : MonoBehaviour
{
	[HideInInspector] public bool tracked = false;
	[HideInInspector] public bool wasTracked = false;
	[HideInInspector] public GameObject markerHelper;

	[Header("Main params")]
	public string id = "";
	public bool useExtendedTracking = false;
	public bool addMarkerHelper = false;
	
	[Header("Do not change")]
	public bool isTracking = false;
	public bool shouldHideChildren = true;
	public TrackingState trackingState;
	
	private float helperRendererScale = 0.1f;
	private bool markerInitialized = false;

	private ARTrackedImage trackedImage;
	private Renderer helperRenderer;
	
	void Start(){
		
		if( useExtendedTracking ){
			
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.transform.SetParent(this.transform);
			
			//sphere.transform.localPosition = new Vector3(0, 0, 0);
			sphere.transform.position = this.transform.position;		
			sphere.transform.localScale = Vector3.one * helperRendererScale;
			
			helperRenderer = sphere.GetComponent<Renderer>();
			helperRenderer.material = new Material(Shader.Find("Transparent/Diffuse"));
			helperRenderer.material.color = new Color(1,1,1,0.0f);
		
		}
		
		ShowHideRenderer( gameObject, false );
		
		if(addMarkerHelper){
			markerHelper = new GameObject("MarkerHelper");
			markerHelper.transform.SetParent(ImageTargetController.Instance.transform);
		}
	}
	
	void Update(){
						
		if( isTracking && !tracked ){	
			
			//print("ImageTarget tracked, id " + id);
			tracked = true;
			
			wasTracked = true;
		}
		else{
			
			if( !isTracking && tracked ){
				
				tracked = false;
			}
		}
		
		UpdateImageTarget();
		
		#if UNITY_IOS
		if( useExtendedTracking ){
			UpdateTrackingStateExtended();
		}else{
			UpdateTrackingState();
		}
		#else
			UpdateTrackingState();
		#endif
		
		//print( markerName + " " + trackingState.ToString() );
	}
	
	public void UpdateImageTarget(){
		
		if( trackedImage == null ) return;
		
		transform.position = trackedImage.transform.position;
		transform.eulerAngles = trackedImage.transform.eulerAngles;
		transform.localScale = Vector3.one * trackedImage.size.x;
	}
	

	public void Init( ARTrackedImage trackedImage ){
				
		//if( markerInitialized ) return;
		//markerInitialized = true;
		
		this.trackedImage = trackedImage;
	}
	
	// Notes:
	// On iOS we can use extended tracking.
	// Basic function: The first found marker which has TrackingState "Tracking" will be displayed
	// If it changes to TrackingState "Limited" it will still be visible as long the helperRenderer is visible in the camera's view
	// If an other marker becomes TrackingState "Tracking" all other marker with TrackingState "Limited" will become disabled
	//
	// Keep in mind: Extended tracking will also show the content of the marker even if we move the marker away
	// The device keeps to try markerless tracking
	// Use "UpdateTrackingState" if you don't want to use extended tracking
	public void UpdateTrackingStateExtended(){		
		
		if (trackingState == TrackingState.None )
		{
			ShowHideRenderer( gameObject, false );
			isTracking = false;
		}
		else if( trackingState == TrackingState.Limited )
		{
			if( helperRenderer.isVisible ){
			
				if( !ImageTargetController.Instance.IsFullTrackingOtherImageTarget( this ) ){
				
					ImageTargetController.Instance.DisableTrackingOtherImageTargets( this );
					if( !isTracking ){
						ShowHideRenderer( gameObject, true );
						isTracking = true;
					}
				}
			}
			else{
			
				ShowHideRenderer( gameObject, false );
				isTracking = false;
			}
		}
		else
		{
			if( !ImageTargetController.Instance.IsFullTrackingOtherImageTarget( this ) ){
				
				ImageTargetController.Instance.DisableTrackingOtherImageTargets( this );
				if( !isTracking ){
					ShowHideRenderer( gameObject, true );
					isTracking = true;
				}
			}
		}
	}
	
	// Notes:
	// On Android we get TrackingState "Tracking" even if we move the marker out of the display. 
	// The device continues extended tracking as long we do not move the device. 
	// To get TrackingState "Limited" we need to move the device away from the markers origin position
	public void UpdateTrackingState(){		
		
		if (trackingState == TrackingState.None )
		{
			//print("UpdateTrackingState TrackingState.None");
			ShowHideRenderer( gameObject, false );
			isTracking = false;
		}
		else if( trackingState == TrackingState.Limited )
		{
			//print("UpdateTrackingState TrackingState.Limited");
			ShowHideRenderer( gameObject, false );
			isTracking = false;			
		}
		else
		{
			if( markerHelper ) markerHelper.transform.position = this.transform.position;
			if( markerHelper ) markerHelper.transform.eulerAngles = this.transform.eulerAngles;
			
			//print("UpdateTrackingState TrackingState.Tracking 1");
			if( !ImageTargetController.Instance.IsFullTrackingOtherImageTarget( this ) ){
				
				ImageTargetController.Instance.DisableTrackingOtherImageTargets( this );
				if( !isTracking ){
					
					ShowHideRenderer( gameObject, true );
					isTracking = true;
				}
			}
		}
	}
	
	public void OnTrackingNone(){

		trackingState = TrackingState.None;
	}
	
	public void OnTrackingLimited(){

		trackingState = TrackingState.Limited;
	}
	
	public void OnTracking(){

		trackingState = TrackingState.Tracking;
	}
	
	void ShowHideRenderer( GameObject obj, bool enable ){
		
		bool shouldEnable = enable;
		if( !shouldHideChildren ) shouldEnable = true;
		
		Renderer[] rend = obj.GetComponentsInChildren<Renderer>(true);
		for( int i = 0; i < rend.Length; i++ ){
			
			if( rend[i] == helperRenderer ) continue;
			rend[i].enabled = shouldEnable;
		}
		
		Canvas[] can = obj.GetComponentsInChildren<Canvas>(true);
		for( int i = 0; i < can.Length; i++ ){
			can[i].enabled = shouldEnable;
		}
	}
	
	public void Disable(){
		
		trackingState = TrackingState.None;
		ShowHideRenderer( gameObject, false );

		isTracking = false;		
	}
	
	public void ParentObject( GameObject myObject ){
							
		myObject.transform.SetParent( transform );
		myObject.transform.localPosition = Vector3.zero;
		myObject.transform.localEulerAngles = Vector3.zero;
		myObject.transform.localScale = Vector3.one * 1;		
	}
}
