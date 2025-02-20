using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class ARFeaturesController : MonoBehaviour
{
    public static ARFeaturesController instance;
    void Awake()
    {
        instance = this;
    }

    public IEnumerator LoadARFeatureCoroutine()
    {
        JSONNode featureData = StationController.instance.GetStationFeature("ar");
        if (featureData == null) yield break;
        if (featureData["type"] == null) yield break;

        print("LoadARFeatureCoroutine " + featureData["type"].Value);

        switch (featureData["type"].Value)
        {
            case "pigeons":

                ARMenuController.instance.DisableMenu(true);
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("ViewingPigeonsSite"));
                ViewingPigeonsController.instance.Init();

                /*
                if (!ARController.instance.arSession.enabled)
                {
                    InfoController.instance.loadingCircle.SetActive(true);
                    ARController.instance.InitARFoundation();
                    yield return new WaitForSeconds(0.5f);
                    InfoController.instance.loadingCircle.SetActive(false);
                }
                */

                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ViewingPigeonsSite"));
                yield return new WaitForSeconds(0.25f);
                break;

            case "selfieGame":

                ARMenuController.instance.DisableMenu(true);
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("SelfieGameSite"));
                yield return StartCoroutine(SelfieGameController.instance.InitCoroutine(1));

                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("SelfieGameSite"));
                yield return new WaitForSeconds(0.25f);
                break;

            case "selfieGame2":

                ARMenuController.instance.DisableMenu(true);
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("SelfieGameSite"));
                yield return StartCoroutine(SelfieGameController.instance.InitCoroutine());

                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("SelfieGameSite"));
                yield return new WaitForSeconds(0.25f);
                break;

            case "selfie":

                ARMenuController.instance.DisableMenu(true);
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("SelfieSite"));
                yield return StartCoroutine(SelfieController.instance.InitCoroutine());

                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("SelfieSite"));
                yield return new WaitForSeconds(0.25f);
                break;

            case "postcard":

                ARMenuController.instance.DisableMenu(true);
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("PostcardSite"));
                yield return StartCoroutine(PostcardController.instance.InitCoroutine());

                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("PostcardSite"));
                yield return new WaitForSeconds(0.25f);
                break;

            case "arPhoto":

                ARMenuController.instance.DisableMenu(true);
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("ARPhotoSite"));
                yield return StartCoroutine(ARPhotoController.instance.InitCoroutine());

                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("ARPhotoSite"));
                yield return new WaitForSeconds(0.25f);
                break;

            case "guide":

                ARMenuController.instance.DisableMenu(true);
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("GuideVideoSite"));
                yield return StartCoroutine(ARGuideController.instance.InitCoroutine());

                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("GuideVideoSite"));
                yield return new WaitForSeconds(0.25f);
                break;

            case "3d-Synagoge":

                ARMenuController.instance.DisableMenu(true);
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("SynagogeSite"));
                yield return StartCoroutine(ARObjectController.instance.InitCoroutine());

                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("SynagogeSite"));
                yield return new WaitForSeconds(0.25f);
                break;

            case "audiothek":

                ARMenuController.instance.DisableMenu(true);
                if (AudiothekController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("AudiothekSite")); }
                yield return StartCoroutine(AudiothekController.instance.InitCoroutine());
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("AudiothekSite"));
                yield return new WaitForSeconds(0.25f);
                break;

            case "avatarGuide":

                ARMenuController.instance.DisableMenu(true);
                if (AvatarGuideController.instance == null) { yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("AvatarGuideSite")); }
                yield return StartCoroutine(AvatarGuideController.instance.InitCoroutine());
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("AvatarGuideSite"));
                yield return new WaitForSeconds(0.25f);
                break;

			default: break;
        }
    }

    public void Reset()
    {
        if (ViewingPigeonsController.instance != null) ViewingPigeonsController.instance.Reset();
        if (SelfieController.instance != null) SelfieController.instance.Reset();
        if (PostcardController.instance != null) PostcardController.instance.Reset();
        if (SelfieGameController.instance != null) SelfieGameController.instance.Reset();
        if (ARPhotoController.instance != null) ARPhotoController.instance.Reset();
        if (ARGuideController.instance != null) ARGuideController.instance.Reset();
        if (ARObjectController.instance != null) ARObjectController.instance.Reset();
        if (AvatarGuideController.instance != null) AvatarGuideController.instance.Reset();
        if (AudiothekController.instance != null) AudiothekController.instance.Reset();
    }
}
