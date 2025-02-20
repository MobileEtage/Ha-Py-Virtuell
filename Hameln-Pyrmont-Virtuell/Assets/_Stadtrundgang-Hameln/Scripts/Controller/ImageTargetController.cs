using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

using SimpleJSON;
using TMPro;

/// This component listens for images detected by the <c>XRImageTrackingSubsystem</c>
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTargetController : MonoBehaviour
{
	public ARSession arSession;
	public GameObject scanFrame;
	private ARTrackedImageManager m_TrackedImageManager;
	public List<ImageTarget> imageTargets = new List<ImageTarget>();

    public TextMeshProUGUI infoLabel;

	public static ImageTargetController Instance;
	void Awake()
	{
		Instance = this;
		
		m_TrackedImageManager = FindObjectOfType<ARTrackedImageManager>();
		
		ImageTarget[] imageTargetsTmp = transform.GetComponentsInChildren<ImageTarget>(true);
		foreach( ImageTarget imageTarget in imageTargetsTmp ){
			imageTargets.Add( imageTarget );
		}
		
		//print( "ImageTargets " + imageTargets.Count );
	}

	void Start(){
				
		for( int i = 0; i < imageTargets.Count; i++ ){
			ShowHideRenderer( imageTargets[i].gameObject, false );
		}
	}
	
	void Update(){
	
		if( IsTracking() ){
			
			if( scanFrame != null ) scanFrame.SetActive(false);
		}
		else{
			
			if( scanFrame != null ) scanFrame.SetActive(true);
		}

        UpdateTrackingInfo();

    }
	
	void OnEnable()
	{
		m_TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
	}

	void OnDisable()
	{
		m_TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
	}

	void UpdateInfo(ARTrackedImage trackedImage)
	{

		if (trackedImage.trackingState != TrackingState.None)
		{
			string info = string.Format(
				"{0}\ntrackingState: {1}\nGUID: {2}\nReference size: {3} cm\nDetected size: {4} cm",
				trackedImage.referenceImage.name,
				trackedImage.trackingState,
				trackedImage.referenceImage.guid,
				trackedImage.referenceImage.size * 100f,
				trackedImage.size * 100f);
				
			print(info);
			
		}
		else
		{
		
		}

	}
	
	void ShowHideRenderer( GameObject obj, bool enable ){
		
		Renderer[] rend = obj.GetComponentsInChildren<Renderer>(true);
		for( int i = 0; i < rend.Length; i++ ){
			rend[i].enabled = enable;
		}
		
		Canvas[] can = obj.GetComponentsInChildren<Canvas>(true);
		for( int i = 0; i < can.Length; i++ ){
			can[i].enabled = enable;
		}
	}
	
	
	void InitImageTarget( ARTrackedImage trackedImage ){
		
		print("InitImageTarget");
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			
			if( trackedImage.referenceImage.name == imageTargets[i].id ){	
				
				print("Initialized " + trackedImage.name);
				imageTargets[i].Init(trackedImage);

				break;
			}
		}
	}
	
	void UpdateImageTarget( ARTrackedImage trackedImage ){
				
		for( int i = 0; i < imageTargets.Count; i++ ){
				
			//print( imageTargets[i].markerName + " " + 
			//	trackedImage.referenceImage.name + 
			//(trackedImage.referenceImage.name == imageTargets[i].markerName) );
			
			if( trackedImage.referenceImage.name == imageTargets[i].id ){
								
				if (trackedImage.trackingState == TrackingState.None )
				{
					imageTargets[i].OnTrackingNone();
				}
				else if( trackedImage.trackingState == TrackingState.Limited )
				{
					imageTargets[i].OnTrackingLimited();
				}
				else
				{
					imageTargets[i].OnTracking();
				}
				break;
			}
		}
	}

	void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
	{
		//print("OnTrackedImagesChanged");
		
		foreach (var trackedImage in eventArgs.added)
		{
			InitImageTarget( trackedImage );			
		}

		foreach (var trackedImage in eventArgs.updated){
			
			UpdateImageTarget( trackedImage );
			//UpdateInfo( trackedImage );
		}
	}
	
	public bool IsTracking(){
				
		for( int i = 0; i < imageTargets.Count; i++ ){
			
			if( imageTargets[i].isTracking ) return true;
		}
		return false;
	}
	
	public bool IsFullTrackingOtherImageTarget( ImageTarget imageTraget ){
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			
			if( imageTargets[i] == imageTraget ) continue;
			if( imageTargets[i].trackingState == TrackingState.Tracking ) return true;
		}
		return false;
	}
	
	public void DisableTrackingOtherImageTargets( ImageTarget imageTraget ){
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			
			if( imageTargets[i] == imageTraget ) continue;
			imageTargets[i].Disable();
		}
	}
	
	public string GetTrackedImageTragetID(){
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			if( imageTargets[i].isTracking ) return imageTargets[i].id;
		}
		return "";
	}
	
	public bool IsTrackingMarker( string markerId ){
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			if( imageTargets[i].isTracking && imageTargets[i].id == markerId ) return true;
		}
		return false;
	}
	
	public ImageTarget GetImageTarget( string markerId ){
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			if( imageTargets[i].id == markerId ) return imageTargets[i];
		}
		return null;
	}
	
	public ImageTarget GetTrackedImageTarget( string markerId ){
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			
			if( 
				markerId == imageTargets[i].id && 
				imageTargets[i].trackingState == TrackingState.Tracking 
			){
				return imageTargets[i];
			}
		}
		return null;
	}
	
	public ImageTarget GetTrackedImageTarget( List<string> markerIds ){
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			
			if( !imageTargets[i].gameObject.activeInHierarchy ) continue;
			
			if( 
				markerIds.Contains(imageTargets[i].id) && 
				imageTargets[i].trackingState == TrackingState.Tracking 
			){
				return imageTargets[i];
			}
		}
		return null;
	}

	public List<ImageTarget> GetImageTargets( List<string> markerIds ){
		
		List<ImageTarget> imageTargetsTmp = new List<ImageTarget>();
		for( int i = 0; i < imageTargets.Count; i++ ){
			if( markerIds.Contains(imageTargets[i].id) ){
				imageTargetsTmp.Add( imageTargets[i] );
			}
		}
		return imageTargetsTmp;
	}
	
	public void DisableImageTargetTracking(){
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			imageTargets[i].trackingState = TrackingState.None;
		}
	}
	
	public void EnableImageTargetTracking( List<string> markerIds ){
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			if( markerIds.Contains(imageTargets[i].id) ){
				imageTargets[i].trackingState = TrackingState.Tracking;
				break;
			}
		}
	}
	
	public void EnableImageTargetTrackingForEditorTest( List<string> markerIds ){
		
		if( !ARController.instance.skipScanMarkerEditor ) return;
		
		for( int i = 0; i < imageTargets.Count; i++ ){
			if( markerIds.Contains(imageTargets[i].id) ){
				imageTargets[i].trackingState = TrackingState.Tracking;
				break;
			}
		}
	}
	
	public void PlaceObjectToMarker( 
		GameObject obj,
		List<string> markerIds, 
		Vector3 targetPosition,  
		Vector3 rotation 
	){
		bool isTracking = false;
		for( int i = 0; i < imageTargets.Count; i++ ){
			
			if( markerIds.Contains(imageTargets[i].id) &&
				imageTargets[i].trackingState == TrackingState.Tracking
			){
				isTracking = true;
				PlaceObjectToMarker(
					obj, imageTargets[i].transform, targetPosition, rotation);
				break;
			}
		}
		
		if( !isTracking ){
			
			for( int i = 0; i < imageTargets.Count; i++ ){
				
				if( markerIds.Contains(imageTargets[i].id) ){
					PlaceObjectToMarker(
						obj, imageTargets[i].transform, targetPosition, rotation);		
					break;
				}
			}
		}
	}
	
	public void PlaceObjectToMarker( 
		GameObject obj,
		Transform marker, 
		Vector3 targetPosition,
		Vector3 rotation 
	){
		if( obj == null || marker == null ) return;
		
		Vector3 currentMarkerRotation = marker.eulerAngles;
		marker.eulerAngles = new Vector3( 0, currentMarkerRotation.y, currentMarkerRotation.z );
		
		Vector3 rightVector = marker.right;
		rightVector.y = 0;
		
		Vector3 upVector = marker.up;
		upVector.x = 0;
		upVector.z = 0;
		//upVector = new Vector3(0,1,0);

		Vector3 forwardVector = marker.forward;
		forwardVector.y = 0;

		marker.eulerAngles = currentMarkerRotation;
		
		Vector3 pos = marker.position 
			+ rightVector.normalized*targetPosition.x 
			+ upVector.normalized*targetPosition.y
			+ forwardVector.normalized*targetPosition.z;
			
		Transform myParent = obj.transform.parent;
		obj.transform.SetParent(marker);
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localEulerAngles = rotation;
		obj.transform.SetParent(myParent);
		
		obj.transform.position = pos;	
	}
	
	public void SetTrackingStateNone(){

		for( int i = 0; i < imageTargets.Count; i++ ){

            imageTargets[i].isTracking = false;
            imageTargets[i].trackingState = TrackingState.None;
        }
    }

    public Vector3 GetImageTargetPosition(string id)
    {
        for (int i = 0; i < imageTargets.Count; i++)
        {
            if (imageTargets[i].id == id) return imageTargets[i].transform.position;
        }
        return Vector3.zero;
    }

    public void UpdateTrackingInfo()
    {
        if (infoLabel == null) return;

        string infoText = "";
        for (int i = 0; i < imageTargets.Count; i++)
        {
            infoText += imageTargets[i].id + " ";
            if (imageTargets[i].trackingState == TrackingState.None) { infoText += "None\n"; }
            else if (imageTargets[i].trackingState == TrackingState.Limited) { infoText += "Limited\n"; }
            else if (imageTargets[i].trackingState == TrackingState.Tracking) { infoText += "Tracking\n"; }
        }

        infoLabel.text = infoText;
    }
}
