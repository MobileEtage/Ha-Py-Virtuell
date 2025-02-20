using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class GamesController : MonoBehaviour
{
    public static GamesController instance;
    void Awake()
    {
        instance = this;
    }



    public IEnumerator LoadGameCoroutine()
    {
        JSONNode featureData = StationController.instance.GetStationFeature("game");
        if (featureData == null) yield break;
        if (featureData["type"] == null) yield break;

        print("LoadGameCoroutine " + featureData["type"].Value);

        switch (featureData["type"].Value)
        {
            case "quiz":

                ARMenuController.instance.DisableMenu(true);
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("QuizSite"));
                QuizController.instance.Init(featureData["quizId"].Value);
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("QuizSite"));
                yield return new WaitForSeconds(0.25f);
                if (ARController.instance != null) ARController.instance.StopAndResetARSession();

                break;

            case "dalliKlick":

                ARMenuController.instance.DisableMenu(true);
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("DalliKlickSite"));

                //DalliKlickController.instance.Init(featureData["gameId"].Value);
                DalliKlickController.instance.Init(featureData, featureData["gameIds"]);

                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DalliKlickSite"));
                yield return new WaitForSeconds(0.25f);
                if (ARController.instance != null) ARController.instance.StopAndResetARSession();

                break;

            case "touchGame":

                ARMenuController.instance.DisableMenu(true);
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("TouchGameSite"));
                TouchGameController.instance.Init();
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("TouchGameSite"));
                yield return new WaitForSeconds(0.25f);
                if (ARController.instance != null) ARController.instance.StopAndResetARSession();

                break;

            case "hearGame":

                ARMenuController.instance.DisableMenu(true);
                yield return StartCoroutine(SiteController.instance.LoadSiteCoroutine("HearGameSite"));
                HearGameController.instance.Init();
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("HearGameSite"));
                yield return new WaitForSeconds(0.25f);
                if (ARController.instance != null) ARController.instance.StopAndResetARSession();

                break;

            default: break;
        }
    }

    public void Reset()
    {
        if (QuizController.instance != null) QuizController.instance.Reset();
        if (DalliKlickController.instance != null) DalliKlickController.instance.Reset();
        if (TouchGameController.instance != null) TouchGameController.instance.Reset();
        if (HearGameController.instance != null) HearGameController.instance.Reset();
    }
}
