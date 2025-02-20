using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediaCaptureController : MonoBehaviour
{
    public GameObject footer;
    public GameObject videoButton;
    public GameObject photoButton;
    public GameObject switchCameraButton;
    public GameObject switchCameraButtonRoot;

    private bool isLoading = false;

    public static MediaCaptureController instance;
    void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        UpdateUI();    
    }

    public void UpdateUI()
    {
        if (ShouldEnableFooterOptions())
        {
            footer.SetActive(true);
        }
        else
        {
            footer.SetActive(false);
        }

        if (ShouldEnableSwitchCameraButton())
        {
            switchCameraButtonRoot.SetActive(true);
        }
        else
        {
            switchCameraButtonRoot.SetActive(false);
        }
    }

    public bool ShouldEnableFooterOptions()
    {
        if (SiteController.instance.currentSite != null)
        {
            if (SiteController.instance.currentSite.siteID == "ARObjectSite") { if (ARObjectController.instance != null) { return ARObjectController.instance.arUI.activeInHierarchy; } }
            if (SiteController.instance.currentSite.siteID == "ARPhotoSite") { if (ARPhotoController.instance != null) { return ARPhotoController.instance.arUI.activeInHierarchy; } }
            if (SiteController.instance.currentSite.siteID == "GuideVideoSite") { if (ARGuideController.instance != null) { return ARGuideController.instance.arUI.activeInHierarchy; } }
            if (SiteController.instance.currentSite.siteID == "ViewingPigeonsSite") { if (ViewingPigeonsController.instance != null) { return ViewingPigeonsController.instance.arContent.activeInHierarchy; } }
            if (SiteController.instance.currentSite.siteID == "AvatarGuideSite") { if (AvatarGuideController.instance != null) { return AvatarGuideController.instance.arUI.activeInHierarchy; } }
            if (SiteController.instance.currentSite.siteID == "RomanSoldierSite") { if (ModelController.instance != null) { return ModelController.instance.arUI.activeInHierarchy; } }
			if ( SiteController.instance.currentSite.siteID == "FaceTrackingSite" ) { if ( ModelController.instance != null ) { return ModelController.instance.arUI.activeInHierarchy; } }
			if ( SiteController.instance.currentSite.siteID == "RitterSite" ) { if ( AvatarGuideController.instance != null ) { return AvatarGuideController.instance.arUI.activeInHierarchy; } }
			if ( SiteController.instance.currentSite.siteID == "ARAvatarSite" ) { if ( ARAvatarController.instance != null ) { return ARAvatarController.instance.arObjectRoot.activeInHierarchy; } }
		}

		return false;
    }

    public bool ShouldEnableSwitchCameraButton()
    {
        if (SiteController.instance.currentSite != null)
        {
            if (SiteController.instance.currentSite.siteID == "ARObjectSite") { if (ARObjectController.instance != null) { return ARObjectController.instance.arUI.activeInHierarchy; } }
            if (SiteController.instance.currentSite.siteID == "ARPhotoSite") { if (ARPhotoController.instance != null) { return ARPhotoController.instance.arUI.activeInHierarchy; } }
            if (SiteController.instance.currentSite.siteID == "GuideVideoSite") { if (ARGuideController.instance != null) { return ARGuideController.instance.arUI.activeInHierarchy; } }
        }
        return false;
    }

    public void SwitchCamera()
    {
        if (SiteController.instance.currentSite != null)
        {
            if (SiteController.instance.currentSite.siteID == "ARObjectSite") { if (ARObjectController.instance != null) { ARObjectController.instance.SwitchFrontBackFacing(); } }
            if (SiteController.instance.currentSite.siteID == "ARPhotoSite") { if (ARPhotoController.instance != null) { ARPhotoController.instance.SwitchFrontBackFacing(); } }
            if (SiteController.instance.currentSite.siteID == "GuideVideoSite") { if (ARGuideController.instance != null) { ARGuideController.instance.SwitchFrontBackFacing(); } }
        }
    }

    public void EnableFooter()
    {
        footer.SetActive(true);
    }

    public void Reset()
    {
        PhotoCaptureController.instance.Reset();
        VideoCaptureController.instance.Reset();
        switchCameraButton.SetActive(true);
    }
}
