using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;

public class TestFeaturesController : MonoBehaviour
{
    private bool isLoading = false;
    public GameObject faceTrackingButton;

    public static TestFeaturesController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Init();
    }

    public void Init()
    {
        #if !UNITY_IOS && !UNITY_EDITOR
        faceTrackingButton.SetActive(false);
        #endif
    }

    public void TestFeature(string id)
    {
        switch (id)
        {
            case "TestAudiothek": TestAudiothek(); break;
            case "TestAvatarGuide": TestAvatarGuide(); break;
            case "BodenScan": TestBodenScan(); break;
            default: LoadSite(id); break;
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

    public void TestBodenScan()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(TestBodenScanCoroutine());
    }

    public IEnumerator TestBodenScanCoroutine()
    {
        if (BodenScanController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("BodenScanSite")); }
        yield return StartCoroutine(BodenScanController.instance.InitCoroutine());
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("BodenScanSite"));

        isLoading = false;
    }

    public void LoadSite(string id)
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(LoadSiteCoroutine(id));
    }

    public IEnumerator LoadSiteCoroutine(string id)
    {
        if(id == "Soldier") { if (ModelController.instance == null) 
        { 
            yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("RomanSoldierSite")); }
            yield return StartCoroutine(ModelController.instance.InitCoroutine());
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("RomanSoldierSite"));
        }
        else if (id == "FaceTracking")
        {
            
	        SceneManager.LoadScene("FaceTrackingSite");
	        yield return null;
	        yield return new WaitForEndOfFrame();
            yield return StartCoroutine(FaceTrackingController.instance.InitCoroutine());
            
            /*
            ARController.instance.arPlaneManager.gameObject.SetActive(false);
            if (FaceTrackingController.instance == null)
            {
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("FaceTrackingSite"));
            }
            yield return StartCoroutine(FaceTrackingController.instance.InitCoroutine());
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("FaceTrackingSite"));
            */

        }

        isLoading = false;
    }

    public void Back()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(BackCoroutine());
    }

    public IEnumerator BackCoroutine()
    {
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ImprintSite"));

        Reset();
        isLoading = false;
    }

    public void Reset()
    {

    }
}
