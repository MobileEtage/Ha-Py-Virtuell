using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrientationUIController : MonoBehaviour
{
    public CanvasScaler mainCanvasScaler;
    public GameObject menuOptionsRoot;
    public List<GameObject> menuButtonImages = new List<GameObject>();

    [Space(10)]

    public GameObject videoPlayerFooter;
    public GameObject audioVisualizerImage;

    [Space(10)]

    public bool useTestOrientation = false;
    public bool isLandscape = false;
    public DeviceOrientation currentTestDeviceOrientation;
    public DeviceOrientation currentDeviceOrientation;

    public static OrientationUIController instance;
    void Awake()
    {

        instance = this;
    }

    void Start()
    {
        currentDeviceOrientation = DeviceOrientation.Portrait;
    }

    void Update()
    {
        if (ShouldCheckOrientationChanged()) {

            if (Screen.orientation != ScreenOrientation.AutoRotation) { Screen.orientation = ScreenOrientation.AutoRotation; }
            CheckOrientationChange(); 
        }
        else {

            Screen.orientation = ScreenOrientation.Portrait;
            if (currentDeviceOrientation != DeviceOrientation.Portrait && currentDeviceOrientation != DeviceOrientation.PortraitUpsideDown) {

                currentDeviceOrientation = DeviceOrientation.Portrait;
                OnOrientationChanged(false); 
            } 
        }
    }

    public bool ShouldCheckOrientationChanged()
    {
        if( SiteController.instance.currentSite != null)
        {
            if (SiteController.instance.currentSite.siteID == "PanoramaSite") { if (PanoramaController.instance.panoramaUI.activeInHierarchy) return true; }
            if (SiteController.instance.currentSite.siteID == "VideoFeatureSite") { if (VideoFeatureController.instance.videoUI.activeInHierarchy) return true; }
            if (SiteController.instance.currentSite.siteID == "ScanSite") { if (VideoController.instance.videoSite.activeInHierarchy) return true; }
            if (SiteController.instance.currentSite.siteID == "ARSite") return true;
            if (SiteController.instance.currentSite.siteID == "GallerySite") { if (GalleryController.instance.isFullscreen) { return true; } }
            if (SiteController.instance.currentSite.siteID == "InfoSite") { if (PoiInfoController.instance.fullScreenView.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "IntroVideoSite") { return true; }
        }
        return false;
    }

    public void CheckOrientationChange()
    {

        DeviceOrientation deviceOrientation = Input.deviceOrientation;

#if UNITY_EDITOR

        if(Screen.width > Screen.height) { deviceOrientation = DeviceOrientation.LandscapeLeft; }
        else { deviceOrientation = DeviceOrientation.Portrait; }
        if (useTestOrientation) { deviceOrientation = currentTestDeviceOrientation; }

#endif

        //print(currentDeviceOrientation.ToString() + " " + isLandscape + " " + CanvasController.instance.canvas.GetComponent<CanvasScaler>().referenceResolution);
        //if (deviceOrientation == DeviceOrientation.FaceDown || deviceOrientation == DeviceOrientation.FaceUp || deviceOrientation == DeviceOrientation.Unknown){ return; }

        if (Screen.width > Screen.height) deviceOrientation = DeviceOrientation.LandscapeLeft;
        if (Screen.width < Screen.height) deviceOrientation = DeviceOrientation.Portrait;

        if (currentDeviceOrientation == DeviceOrientation.Portrait || currentDeviceOrientation == DeviceOrientation.PortraitUpsideDown)
        {
            if(deviceOrientation != DeviceOrientation.Portrait && deviceOrientation != DeviceOrientation.PortraitUpsideDown)
            {
                currentDeviceOrientation = deviceOrientation;
                OnOrientationChanged(true);
            }
        }
        else if (currentDeviceOrientation == DeviceOrientation.LandscapeLeft || currentDeviceOrientation == DeviceOrientation.LandscapeRight)
        {
            if (deviceOrientation != DeviceOrientation.LandscapeLeft && deviceOrientation != DeviceOrientation.LandscapeRight)
            {
                currentDeviceOrientation = deviceOrientation;
                OnOrientationChanged(false);
            }
        }
    }

    public void OnOrientationChanged(bool isLandscape)
    {
        print("OnOrientationChanged " + isLandscape);
        UpdateUI(isLandscape);
    }

    public void UpdateUI(bool isLandscape)
    {
        this.isLandscape = isLandscape;

        // Change menu rotation
        float menuOptionsRotation = isLandscape ? 90:0;
        menuOptionsRoot.transform.localEulerAngles = new Vector3(0, 0, menuOptionsRotation);
        for (int i = 0; i < menuButtonImages.Count; i++) { menuButtonImages[i].transform.localEulerAngles = new Vector3(0, 0, -menuOptionsRotation); }

        // Change referenceResolution, just switch width and height
        Vector2 referenceResolution = mainCanvasScaler.referenceResolution;
        float w = isLandscape ? Mathf.Max(referenceResolution.x, referenceResolution.y) : Mathf.Min(referenceResolution.x, referenceResolution.y);
        float h = isLandscape ? Mathf.Min(referenceResolution.x, referenceResolution.y) : Mathf.Max(referenceResolution.x, referenceResolution.y);
        mainCanvasScaler.referenceResolution = new Vector2(w, h);

        float s = isLandscape ? 1.175f : 1.0f;
        videoPlayerFooter.transform.localScale = Vector3.one * s;

        float s2 = isLandscape ? 3.0f : 1.0f;
        audioVisualizerImage.transform.localScale = new Vector3(1,s2,1);
    }
}
