using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using MPUIKIT;
using SimpleJSON;

// Script to handle the side menu on ARSite and invoke button functions of the menu

public class ARMenuController : MonoBehaviour
{
    public GameObject menuBar;
	public Transform mainMenuButton;
	public Image mainMenuButtonBackground;
	public Image mainMenuButtonDots;
	public GameObject menuButtonsContent_GridLayout;
    public GameObject menuButtonsContent_VerticalLayout;
    public GameObject menuButtonsContent;
    public GameObject menuHighlightImage;
    public string currentFeature = "";
    public string lastFeature = "";
    public bool isMenuImmediateOpen = false;

    public bool menuIsOpen = false;
    private bool isLoading = false;
    private bool isActivatingMenuBar = false;
    private bool initialized = false;
    private bool menuHighlightImageShowed = false;

    private float menuOpenSpacing = 5;
    private float menuClosedSpacing = -140;

    private Color arMenuButtonActivColor;
	private bool menuButtonInitialized = false;

	public List<string> highlightMenuButtonExecuted = new List<string>();

    public static ARMenuController instance;
    void Awake()
    {
        instance = this;
	}

    void Start()
    {
        arMenuButtonActivColor = Params.arMenuButtonActivColor;
        menuButtonsContent = menuButtonsContent_VerticalLayout;
        StartCoroutine(EnableMaskCoroutine());

		//menuButtonInitialized = PlayerPrefs.GetInt( "menuButtonInitialized", 0 ) == 1;
    }

    void Update()
    {
        if (initialized) { UpdateMenuBarVisibility(); }
    }

    public IEnumerator EnableMaskCoroutine()
    {
        yield return null;
        menuButtonsContent.GetComponentInParent<Mask>(true).enabled = true;
        menuBar.SetActive(false);
        menuBar.GetComponent<CanvasGroup>().alpha = 1;

        initialized = true;
    }

    public void ShowHighlightMenu()
    {
        if (menuHighlightImageShowed) return;
        menuHighlightImageShowed = true;
        StartCoroutine(ShowHighlightMenuCoroutine());
    }

    public IEnumerator ShowHighlightMenuCoroutine()
    {
        if (!menuIsOpen) OpenMenu();
        yield return new WaitForSeconds(0.5f);

        menuHighlightImage.SetActive(true);
        menuHighlightImage.GetComponent<CanvasGroup>().alpha = 0;
        yield return null;
        menuHighlightImage.GetComponent<HighlightMenuImage>().UpdateSize();
        yield return null;

        menuHighlightImage.GetComponent<HighlightMenuImage>().initialized = true;
        menuHighlightImage.GetComponent<HighlightMenuImage>().Blink();
    }

    public void UpdateMenuBarVisibility()
    {
        if (ValidMenuBarSite())
        {
            if (!menuBar.activeInHierarchy && !isActivatingMenuBar)
            {
                isActivatingMenuBar = true;
                StartCoroutine("ShowMenuBarCoroutine");
                //menuBar.SetActive(true); 
            }
        }
        else
        {
            if (menuBar.activeInHierarchy)
            {
                StopCoroutine("ShowMenuBarCoroutine");
                isActivatingMenuBar = false;
                menuBar.SetActive(false);
            }
        }
    }

    public bool ValidMenuBarSite()
    {
        if (SiteController.instance.currentSite != null)
        {
			if ( SiteController.instance.currentSite.siteID == "ARSite" ) { return true; }
			if ( SiteController.instance.currentSite.siteID == "GlassObjectSite" && !GlasObjectController.instance.placementHelper.activeInHierarchy) { return true; }
			if ( SiteController.instance.currentSite.siteID == "ARAvatarSite" ) { return true; }
			if ( SiteController.instance.currentSite.siteID == "GallerySite") { if (!GalleryController.instance.isFullscreen) { return true; } }
            if (SiteController.instance.currentSite.siteID == "AudioSite") { if (AudioController.instance.tutorialContent.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "ViewingPigeonsSite") { if (ViewingPigeonsController.instance != null && ViewingPigeonsController.instance.tutorialContent1.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "QuizSite") { if (QuizController.instance != null && QuizController.instance.tutorialContent.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "DalliKlickSite") { if (DalliKlickController.instance != null && DalliKlickController.instance.tutorialContent.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "InfoSite") { if (!MapFilterController.instance.didClickedOnFilterStation) { return true; } }
            if (SiteController.instance.currentSite.siteID == "SelfieSite") { if (SelfieController.instance.tutorialContent.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "PostcardSite") { if (PostcardController.instance.tutorialContent.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "SelfieGameSite") { if (SelfieGameController.instance.tutorialContent.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "ARPhotoSite") { if (ARPhotoController.instance.tutorialUI.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "TouchGameSite") { if (TouchGameController.instance.tutorialContent.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "GuideVideoSite") { if (ARGuideController.instance.tutorialUI.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "VideoFeatureSite") { if (VideoFeatureController.instance.tutorialUI.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "PanoramaSite") { if (PanoramaController.instance.tutorialUI.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "ARObjectSite") { if (ARObjectController.instance != null && ARObjectController.instance.tutorialUI.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "HearGameSite") { if (HearGameController.instance != null && HearGameController.instance.tutorialContent.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "SynagogeSite") { if (ARObjectController.instance != null && ARObjectController.instance.tutorialUI.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "AudiothekSite") { if (AudiothekController.instance != null && AudiothekController.instance.tutorialContent.activeInHierarchy) { return true; } }
            if (SiteController.instance.currentSite.siteID == "AvatarGuideSite") { if (AvatarGuideController.instance != null && AvatarGuideController.instance.tutorialUI.activeInHierarchy) { return true; } }
        }
        return false;
    }

    public IEnumerator ShowMenuBarCoroutine()
    {
        yield return new WaitForSeconds(0.3f);
        if (ValidMenuBarSite())
		{
			menuBar.SetActive(true);

			//if ( !menuButtonInitialized ){ mainMenuButton.GetComponent<Animator>().Play( "buttonPulse" ); }
			if ( !highlightMenuButtonExecuted.Contains( MapController.instance.selectedStationId ) ) { mainMenuButton.GetComponent<Animator>().Play( "buttonPulse" ); }
		}
        isActivatingMenuBar = false;
    }

    public void InitMenu(string markerId)
    {
        CloseMenuImmediate();

        // Disable rounded corners and make buttons inactive
        foreach (Transform child in menuButtonsContent.transform)
        {
            if (child.GetSiblingIndex() == 0) continue;

            child.gameObject.SetActive(true);

            MPImage image = child.GetChild(0).GetComponent<MPImage>();
            Rectangle rectangle = child.GetChild(0).GetComponent<MPImage>().Rectangle;
            rectangle.CornerRadius = new Vector4(0, 0, 0, 0);
            image.Rectangle = rectangle;

            child.gameObject.SetActive(false);
        }

        // Get features
        JSONNode stationData = StationController.instance.GetStationDataFromMarkerId(markerId);
        if (stationData == null) return;

        JSONNode featuresJson = stationData["features"];
        if (featuresJson == null || featuresJson.Count == 0)
        {
            mainMenuButton.gameObject.SetActive(false);
            return;
        }
        else
        {
            mainMenuButton.gameObject.SetActive(true);			
        }

        // Activate feature buttons
        for (int i = 0; i < featuresJson.Count; i++)
        {
            GameObject menuButton = ToolsController.instance.FindGameObjectByName( menuButtonsContent, featuresJson[i]["id"].Value);
			if( featuresJson[i]["id"].Value.Contains( "glashuette" ) ) { menuButton = ToolsController.instance.FindGameObjectByName( menuButtonsContent, "glashuette" ); }

            if (menuButton == null) continue;
            menuButton.SetActive(true);

            if (!PermissionController.instance.IsARFoundationSupported())
            {
                if (featuresJson[i]["id"].Value == "ar")
                {
                    if (featuresJson[i]["type"] != null)
                    {
                        if (featuresJson[i]["type"].Value == "arPhoto" ||
                            featuresJson[i]["type"].Value == "pigeons" ||
							featuresJson[i]["type"].Value == "guide" ||							
							featuresJson[i]["type"].Value == "3d"
                            )
                        {
                            menuButton.SetActive(false);
                        }
                    }
                }
				//else if ( featuresJson[i]["id"].Value == "avatarGuide" ){ menuButton.SetActive( false ); }
			}
        }

        Transform firstActiveChild = mainMenuButton;
        Transform lastActiveChild = mainMenuButton;
        foreach (Transform child in menuButtonsContent.transform)
        {
            if (child.GetSiblingIndex() == 0) continue;
            if (firstActiveChild == mainMenuButton && child.gameObject.activeSelf) { firstActiveChild = child; }
            if (child.gameObject.activeSelf) { lastActiveChild = child; }
        }

        // Make first active button with rounded corners
        if (firstActiveChild != mainMenuButton)
        {
            MPImage image = firstActiveChild.transform.GetChild(0).GetComponent<MPImage>();
            Rectangle rectangle = firstActiveChild.transform.GetChild(0).GetComponent<MPImage>().Rectangle;
            rectangle.CornerRadius = new Vector4(25, 25, 0, 0);
            image.Rectangle = rectangle;
        }

        // Make last active button with rounded corners
        if (lastActiveChild != mainMenuButton)
        {
            MPImage image = lastActiveChild.transform.GetChild(0).GetComponent<MPImage>();
            Rectangle rectangle = lastActiveChild.transform.GetChild(0).GetComponent<MPImage>().Rectangle;
            rectangle.CornerRadius = new Vector4(0, 0, 25, 25);
            image.Rectangle = rectangle;
        }

        if (lastActiveChild == firstActiveChild && firstActiveChild != mainMenuButton)
        {
            MPImage image = lastActiveChild.transform.GetChild(0).GetComponent<MPImage>();
            Rectangle rectangle = lastActiveChild.transform.GetChild(0).GetComponent<MPImage>().Rectangle;
            rectangle.CornerRadius = new Vector4(25, 25, 25, 25);
            image.Rectangle = rectangle;
        }
    }

    public void CloseMenuImmediate()
    {
		mainMenuButtonBackground.color = arMenuButtonActivColor;
		mainMenuButtonDots.color = ToolsController.instance.GetColorFromHexString("#FFFFFFFF");

        /*
        MPImage image = mainMenuButton.GetChild(0).GetComponent<MPImage>();
        Rectangle rectangle = mainMenuButton.GetChild(0).GetComponent<MPImage>().Rectangle;
        rectangle.CornerRadius = new Vector4(25, 25, 25, 25);
        image.Rectangle = rectangle;
        */

        if (menuButtonsContent == menuButtonsContent_GridLayout) { menuButtonsContent.GetComponent<GridLayoutGroup>().spacing = new Vector2(0, menuClosedSpacing); }
        else
        {

            //menuButtonsContent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 0, (int)(-menuButtonsContent.GetComponent<RectTransform>().rect.height)); 
            menuButtonsContent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 0, (int)(-1500));
        }

        menuIsOpen = false;
        isMenuImmediateOpen = false;

        EnableDisableButtons(false);
        MarkMenuButton("");
    }

    public void OpenMenu()
    {
        if (menuIsOpen) return;
        OpenCloseMenu();
    }

    public void CloseMenu()
    {
        if (!menuIsOpen) return;
        OpenCloseMenu();
    }

    public void OpenCloseMenu()
    {
		/*
		if ( !menuButtonInitialized )
		{
			menuButtonInitialized = true;
			mainMenuButton.GetComponent<Animator>().Play( "buttonIdle" );
			PlayerPrefs.SetInt( "menuButtonInitialized", 1 );
			PlayerPrefs.Save();
		}
		*/

		if ( !highlightMenuButtonExecuted.Contains( MapController.instance.selectedStationId ) )
		{
			highlightMenuButtonExecuted.Add( MapController.instance.selectedStationId );
			mainMenuButton.GetComponent<Animator>().Play( "buttonIdle" );
		}

		if ( isLoading) return;
        isLoading = true;

        StartCoroutine(OpenCloseMenuCoroutine());
    }

    public IEnumerator OpenCloseMenuCoroutine()
    {
        print("OpenCloseMenuCoroutine " + menuIsOpen);

        AnimationCurve animationCurve = AnimationController.instance.GetAnimationCurveWithID("fastSlow");
        if (animationCurve == null) yield break;

        isMenuImmediateOpen = !isMenuImmediateOpen;
        if (!menuIsOpen)
        {

            /*
		    MPImage image = mainMenuButton.GetChild(0).GetComponent<MPImage>();
		    Rectangle rectangle = mainMenuButton.GetChild(0).GetComponent<MPImage>().Rectangle;
            rectangle.CornerRadius = new Vector4(25, 25, 0, 0);
		    image.Rectangle = rectangle;
            */
        }

        if (menuIsOpen)
        {

            EnableDisableButtons(false);
        }
        else
        {
            menuButtonsContent.GetComponent<CanvasGroup>().alpha = 0;
            menuButtonsContent.GetComponentInParent<Mask>(true).enabled = false;
            menuButtonsContent.GetComponentInParent<Mask>(true).GetComponent<Image>().enabled = false;

            yield return null;
            yield return new WaitForEndOfFrame();

            menuButtonsContent.GetComponentInParent<Mask>(true).enabled = true;
            menuButtonsContent.GetComponent<CanvasGroup>().alpha = 1;
            menuButtonsContent.GetComponentInParent<Mask>(true).GetComponent<Image>().enabled = true;
        }

		mainMenuButtonBackground.color = menuIsOpen ? arMenuButtonActivColor : ToolsController.instance.GetColorFromHexString("#FFFFFFFF");
		mainMenuButtonDots.color = menuIsOpen ? ToolsController.instance.GetColorFromHexString("#FFFFFFFF") : Params.arMenuIconsInActivColor;

        float ySpacing = 0;
        if (menuButtonsContent == menuButtonsContent_VerticalLayout) { ySpacing = menuButtonsContent.GetComponent<VerticalLayoutGroup>().padding.bottom; }
        else { ySpacing = menuButtonsContent.GetComponent<GridLayoutGroup>().spacing.y; }

        float targetSpacing = menuIsOpen ? menuClosedSpacing : menuOpenSpacing;
        if (menuButtonsContent == menuButtonsContent_VerticalLayout) { targetSpacing = menuIsOpen ? -menuButtonsContent.GetComponent<RectTransform>().rect.height : 0; }

        float animationDuration = 0.4f;
        float currentTime = 0;
        while (currentTime < animationDuration)
        {
            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            float spacing = Mathf.LerpUnclamped(ySpacing, targetSpacing, lerpValue);

            if (menuButtonsContent == menuButtonsContent_VerticalLayout) { menuButtonsContent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 0, (int)spacing); }
            else { menuButtonsContent.GetComponent<GridLayoutGroup>().spacing = new Vector2(0, spacing); }

            currentTime += Time.deltaTime;
            yield return null;
        }

        if (menuButtonsContent == menuButtonsContent_VerticalLayout) { menuButtonsContent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 0, (int)targetSpacing); }
        else { menuButtonsContent.GetComponent<GridLayoutGroup>().spacing = new Vector2(0, targetSpacing); }


        menuIsOpen = !menuIsOpen;
        isMenuImmediateOpen = menuIsOpen;

        if (!menuIsOpen)
        {

            //MarkMenuButton("");

            /*
	        MPImage image = mainMenuButton.GetChild(0).GetComponent<MPImage>();
	        Rectangle rectangle = mainMenuButton.GetChild(0).GetComponent<MPImage>().Rectangle;
            rectangle.CornerRadius = new Vector4(25, 25, 25, 25);
	        image.Rectangle = rectangle;
            */
        }

        if (menuIsOpen)
        {

            EnableDisableButtons(true);
        }

        isLoading = false;
    }

    public void EnableDisableButtons(bool activate)
    {
        foreach (Transform child in menuButtonsContent.transform)
        {
            if (child.GetSiblingIndex() == 0) continue;
            child.GetComponent<Button>().enabled = activate;
        }
    }

    public void MarkMenuButton(string buttonName)
    {
        foreach (Transform child in menuButtonsContent.transform)
        {
            if (child.GetSiblingIndex() == 0) continue;
            child.GetChild(0).GetComponent<Image>().color = ToolsController.instance.GetColorFromHexString("#FFFFFFFF");
            child.GetChild(1).GetComponent<Image>().color = Params.arMenuIconsInActivColor;
        }

        foreach (Transform child in menuButtonsContent.transform)
        {
            if (child.GetSiblingIndex() == 0) continue;
            if (child.name == buttonName || (buttonName.Contains("glashuette") && child.name == "glashuette"))
            {
                child.GetChild(0).GetComponent<Image>().color = arMenuButtonActivColor;
                child.GetChild(1).GetComponent<Image>().color = ToolsController.instance.GetColorFromHexString("#FFFFFFFF");
                break;
            }
        }
    }

    public void OpenMenu(string buttonName)
    {
		Debug.Log( "OpenMenu " + buttonName );

		if( buttonName == "glashuette" ) { buttonName = MapController.instance.selectedStationId; }
        if (currentFeature == buttonName) return;

        if (isLoading) return;
        isLoading = true;

        StartCoroutine(OpenStationFeatureCoroutine(buttonName));
    }

	public IEnumerator OpenStationFeatureCoroutine(string buttonName)
	{
		print( "Open menu: " + buttonName );

		if ( !buttonName.Contains( "glashuette" ) )
		{
			bool cameraActive = ARController.instance.arSession.enabled;
			if ( cameraActive ) { InfoController.instance.loadingCircle.SetActive( true ); }
			else { yield return StartCoroutine( InfoController.instance.ShowLoadingScreenShotCoroutine() ); }
		}

        StopFeatures();
        yield return StartCoroutine(ToolsController.instance.CleanMemoryCoroutine());

        lastFeature = currentFeature;
        currentFeature = buttonName;

        UpdateMenuColor(0);
        MarkMenuButton(buttonName);
        JSONNode featureData = StationController.instance.GetStationFeature(buttonName);

		if( featureData == null ) { Debug.LogError( "No featureData found for " + buttonName ); }

		if ( featureData != null )
        {
			switch ( featureData["id"].Value )
			{
				case "guide":

                    bool useGuideInAR = StationController.instance.UseGuideInAR() && PermissionController.instance.IsARFoundationSupported();
                    if (!useGuideInAR) { yield return StartCoroutine(GuideController.instance.LoadGuideVideoCoroutine()); }
                    else { yield return StartCoroutine(GuideController.instance.LoadGuideCoroutine()); }
                    break;

				case "avatarGuide":

					if ( ARAvatarController.instance == null ) { yield return StartCoroutine( SiteController.instance.LoadSiteCoroutine( "ARAvatarSite" ) ); }
					yield return StartCoroutine( ARAvatarController.instance.InitCoroutine() );

					/*
					if ( !PermissionController.instance.IsARFoundationSupported() )
					{
						if ( StationController.instance.HasFeature( "info" ) ){

							currentFeature = "info";
							MarkMenuButton( currentFeature );
							yield return StartCoroutine( PoiInfoController.instance.LoadInfoCoroutine() );
						}
						else{ InfoController.instance.ShowMessage( "Funktionen für diese Station noch nicht verfügbar." ); }
					}
					else
					{
						if ( ARAvatarController.instance == null ) { yield return StartCoroutine( SiteController.instance.LoadSiteCoroutine( "ARAvatarSite" ) ); }
						yield return StartCoroutine( ARAvatarController.instance.InitCoroutine() );
					}
					*/

					break;

				case "glashuette":
				case "glashuette1":
				case "glashuette2":
				case "glashuette3":
				case "glashuette4":

					GlasObjectController.instance.LoadObject();

					break;

				case "info":

                    yield return StartCoroutine(PoiInfoController.instance.LoadInfoCoroutine());
                    break;

                case "gallery":

                    yield return StartCoroutine(GalleryController.instance.LoadImagesCoroutine());
                    break;

                case "video":

                    UpdateMenuColor(1);
                    if (VideoFeatureController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("VideoFeatureSite")); }
                    yield return StartCoroutine(VideoFeatureController.instance.InitCoroutine());
                    break;

                case "panorama":

                    UpdateMenuColor(1);
                    if (PanoramaController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("PanoramaSite")); }
                    yield return StartCoroutine(PanoramaController.instance.InitCoroutine());
                    break;

                case "ar":

                    UpdateMenuColor(1);
                    yield return StartCoroutine(ARFeaturesController.instance.LoadARFeatureCoroutine());
                    break;

                case "game":

                    UpdateMenuColor(1);
                    yield return StartCoroutine(GamesController.instance.LoadGameCoroutine());
                    break;

                case "audiothek":

                    UpdateMenuColor(1);
                    DisableMenu(true);
                    yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("AudiothekSite"));
                    yield return StartCoroutine(AudiothekController.instance.InitCoroutine());
                    yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("AudiothekSite"));
                    yield return new WaitForSeconds(0.25f);

                    break;

                case "audio":

                    UpdateMenuColor(1);
                    if (AudioController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("AudioSite")); }
                    if (featureData["type"] != null && featureData["type"].Value == "audioOnly")
                    {
                        yield return StartCoroutine(AudioController.instance.LoadSingleAudioFileCoroutine());
                    }
                    else
                    {
                        DisableMenu(true);
                        yield return StartCoroutine(AudioController.instance.InitCoroutine());
                    }

                    break;

                default: break;
            }
        }
        InfoController.instance.HideLoadingScreenShot();
        InfoController.instance.loadingCircle.SetActive(false);

        isLoading = false;
    }

    public void UpdateMenuColor(int index)
    {
        if (index == 1) { arMenuButtonActivColor = Params.arMenuButtonActivColor2; }
        else { arMenuButtonActivColor = Params.arMenuButtonActivColor; }

        //mainMenuButton.GetChild(0).GetComponent<Image>().color = arMenuButtonActivColor;
    }

    public void DisableMenu(bool keepFeatureActive = false)
    {
        //CloseMenuImmediate();
        //CloseMenu();

        if (menuIsOpen)
        {
            StartCoroutine(OpenCloseMenuCoroutine());
        }

        if (keepFeatureActive)
        {
            MarkMenuButton(currentFeature);
        }
        else
        {
            currentFeature = "";
        }
    }

    public void StopFeatures()
    {
        currentFeature = "";

        GamesController.instance.Reset();
        GuideController.instance.Reset();
        GalleryController.instance.Reset();
        PoiInfoController.instance.Reset();

        VideoController.instance.StopVideo();
		if (PanoramaController.instance != null) { PanoramaController.instance.Reset(); }
        if (VideoFeatureController.instance != null) { VideoFeatureController.instance.Reset(); }
        if (AudiothekController.instance != null) { AudiothekController.instance.Reset(); }
		if ( AudioController.instance != null ) { AudioController.instance.Reset(); }
		if ( GlasObjectController.instance != null ) { GlasObjectController.instance.Reset(); }
		if ( ARAvatarController.instance != null ) { ARAvatarController.instance.Reset(); }
	}

	public void ResetCurrentFeature()
    {
        print("Resetting " + lastFeature);

		if ( lastFeature == "guide" ) { GuideController.instance.Reset(); }
		if ( lastFeature == "avatarGuide" ) { ARAvatarController.instance.Reset(); }
		if ( lastFeature.Contains( "glashuette" ) ) { GlasObjectController.instance.Reset(); }
		else if (lastFeature == "info") { PoiInfoController.instance.Reset(); }
        else if (lastFeature == "gallery") { GalleryController.instance.Reset(); }
        else if (lastFeature == "panorama" && PanoramaController.instance != null) { PanoramaController.instance.Reset(); }
        else if (lastFeature == "audio" && AudioController.instance != null) { AudioController.instance.Reset(); }
        else if (lastFeature == "ar") { ARFeaturesController.instance.Reset(); }
        else if (lastFeature == "video" && VideoFeatureController.instance != null) { VideoFeatureController.instance.Reset(); }
        else if (lastFeature == "game") { GamesController.instance.Reset(); }
    }

    public void PauseFeatures()
    {
		VideoController.instance.StopVideo();
    }
}
