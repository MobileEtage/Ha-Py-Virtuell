using System;
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
using LeTai.TrueShadow;

public class MenuController : MonoBehaviour
{
    [Header("Drag menu")]

    public DragMenuController drageMenuController;
    public GameObject dragMenuContent;

    public bool isLoadingMenu = false;
    public bool isLoading = false;

    public static MenuController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {

    }

    public void OpenMenu()
    {
        OffCanvasController.instance.OpenMenu();
    }

    public void CloseMenu()
    {
        OffCanvasController.instance.CloseMenu();
    }

    public void OpenDragMenu(string menuName)
    {
        foreach (Transform child in dragMenuContent.transform)
        {
            if (child.gameObject.name != "DragArea") { child.gameObject.SetActive(false); }
        }
        GameObject menu = ToolsController.instance.FindGameObjectByName(dragMenuContent, menuName);
        menu.SetActive(true);
        drageMenuController.Open();
    }

    public void CloseDragMenu()
    {

        drageMenuController.Close();
    }

    public void OpenMenu(string id)
    {
        switch (id)
        {
            case "map": OpenMap( Params.showMapFilterOptions ); break;
            case "tours": OpenTouren(); MapController.instance.DisableMap(); break;
            case "tutorial": OpenTutorial(); MapController.instance.DisableMap(); break;
            case "favorites": OpenFavorites(); MapController.instance.DisableMap(); break;
            case "imprint": OpenImprint(); MapController.instance.DisableMap(); break;
            case "privacy": OpenPrivacy(); MapController.instance.DisableMap(); break;
			case "feedback": OpenFeedback(); break;
            case "newsletter": OpenNewsletter(); break;
            case "challenge": OpenChallenge(); break;
            case "introVideo": OpenIntroVideo(); break;
            case "settings": OpenSettings(); break;
        }
    }

    public void OpenMap(bool showFilter = false)
    {
        if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID == "MapSite")
        {
            CloseMenu();
			if ( Params.showMapFilterOptions ) { MapFilterController.instance.filterOptionsDragMenu.Open(); }
            return;
        }

        MapController.instance.shouldShowMapFilter = showFilter;
        MapController.instance.LoadMapFromMenu();

        /*
        if (isLoading) return;
        isLoading = true;
        StartCoroutine( OpenMapCoroutine() );
        */
    }

    public IEnumerator OpenMapCoroutine()
    {
        print("OpenMapCoroutine");

        bool hasPermissionGPS = false;
        yield return StartCoroutine(
            PermissionController.instance.ValidatePermissionsGPSCoroutine((bool success) => {
                hasPermissionGPS = success;
            })
        );

        MapController.instance.Init();

        CloseMenu();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("MapSite"));

        isLoading = false;
    }

    public void OpenTouren()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(OpenTourenCoroutine());
    }

    public IEnumerator OpenTourenCoroutine()
    {
        CloseMenu();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DashboardSite"));

        isLoading = false;
    }

    public void OpenTutorial()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(OpenTutorialCoroutine());
    }

    public IEnumerator OpenTutorialCoroutine()
    {
        if (TutorialController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("TutorialSite")); }
        yield return StartCoroutine(TutorialController.instance.InitTutorialCoroutine("mainTutorial"));
        CloseMenu();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("TutorialSite"));

        isLoading = false;
    }

    public void OpenFavorites()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(OpenFavoritesCoroutine());
    }

    public IEnumerator OpenFavoritesCoroutine()
    {
        CloseMenu();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("FavoritesSite"));

        isLoading = false;
    }

    public void OpenPrivacy()
    {
        if (Params.usePrivacyWeblink)
        {
            ToolsController.instance.OpenWebView(Params.privacyURL);
        }
        else
        {
            if (isLoading) return;
            isLoading = false;
            StartCoroutine(OpenPrivacyCoroutine());
        }
    }

    public IEnumerator OpenPrivacyCoroutine()
    {
        CloseMenu();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("PrivacySite"));

        isLoading = false;
    }

    public void OpenImprint()
    {
        if (Params.useImprintWeblink)
        {
            ToolsController.instance.OpenWebView(Params.imprintURL);
        }
        else
        {
            if (isLoading) return;
            isLoading = false;
            StartCoroutine(OpenImprintCoroutine());
        }
    }

    public IEnumerator OpenImprintCoroutine()
    {
        CloseMenu();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ImprintSite"));

        isLoading = false;
    }

    public void OpenFeedback()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(OpenFeedbackCoroutine());
    }

    public IEnumerator OpenFeedbackCoroutine()
    {
        CloseMenu();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("FeedbackSite"));

        isLoading = false;
    }

    public void OpenChallenge()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(OpenChallengeCoroutine());
    }

    public IEnumerator OpenChallengeCoroutine()
    {
        CloseMenu();
        if (ChallengeController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("ChallengeSite")); }
        ChallengeController.instance.Init();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ChallengeSite"));

        isLoading = false;
    }

    public void OpenIntroVideo()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(OpenIntroVideoCoroutine());
    }

    public IEnumerator OpenIntroVideoCoroutine()
    {
        CloseMenu();
        if (IntroVideoController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("IntroVideoSite")); }
        yield return StartCoroutine(IntroVideoController.instance.InitCoroutine());
        IntroVideoController.instance.continueTutorial = false;

        isLoading = false;
    }

    public void OpenNewsletter()
    {
        ToolsController.instance.OpenWebView(Params.newsletterLink);

        //if (isLoading) return;
        //isLoading = true;
        //StartCoroutine(OpenNewsletterCoroutine());
    }

    public IEnumerator OpenNewsletterCoroutine()
    {
        CloseMenu();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("NewsletterSite"));

        isLoading = false;
    }

    public void OpenSettings()
    {
        if (isLoading) return;
        isLoading = false;
        StartCoroutine(OpenSettingsCoroutine());
    }

    public IEnumerator OpenSettingsCoroutine()
    {
        CloseMenu();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("SettingsSite"));

        isLoading = false;
    }
}
