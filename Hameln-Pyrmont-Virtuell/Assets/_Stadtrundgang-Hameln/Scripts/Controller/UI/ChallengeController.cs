using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;

public class ChallengeController : MonoBehaviour
{
    public TextMeshProUGUI progressLabel;
    public TextMeshProUGUI stationPointsLabel;
    public TextMeshProUGUI quizPointsLabel;
    public TextMeshProUGUI imageQuizPointsLabel;
    public GameObject textDescription;
    public GameObject textSuccess;

    private bool isLoading = false;

    public static ChallengeController instance;
    void Awake()
    {
        instance = this;
    }

    public void Init()
    {
        // Stations
        int stationCount = TourController.instance.GetStationCount();
        int stationFinishedCount = TourController.instance.GetFinishedStationCount();

        // Quiz
        int quizQuestions = 6;  // Todo: this should be dynamically be loaded
        int quizPoints = 0;

        string quizId = "quiz1";
        quizPoints += PlayerPrefs.GetInt("Quiz_" + TourController.instance.GetCurrentTourId() + "_" + quizId + "_Points", 0);

        quizId = "quiz2";
        quizPoints += PlayerPrefs.GetInt("Quiz_" + TourController.instance.GetCurrentTourId() + "_" + quizId + "_Points", 0);

        quizId = "quiz3";
        quizPoints += PlayerPrefs.GetInt("Quiz_" + TourController.instance.GetCurrentTourId() + "_" + quizId + "_Points", 0);

        // Dalli Klick
        //int imageQuizMaxPoints = 3 * 9;  // Todo: this should be dynamically be loaded
        int imageQuizMaxPoints = 3 * 8;  // Todo: this should be dynamically be loaded
        int imageQuizPoints = 0;

        string imageGameId = "dalliKlick1";
        imageQuizPoints += PlayerPrefs.GetInt("DalliKlick_" + TourController.instance.GetCurrentTourId() + "_" + imageGameId + "_Points", 0);
        //imageQuizPoints = Mathf.Clamp(imageQuizPoints, 0, imageQuizMaxPoints);

        // All texts
        int finishedItems = stationFinishedCount + quizPoints + imageQuizPoints;
        int itemsCount = stationCount + quizQuestions + imageQuizMaxPoints;

        float progress = (float)finishedItems / (float)itemsCount;
        progressLabel.text = LanguageController.GetTranslation("Dein Status") + ": " + (progress*100).ToString("F0") + " %/100 %";
        stationPointsLabel.text = stationFinishedCount + "/" + stationCount + " " + LanguageController.GetTranslation("Stationen angeschaut,");
        quizPointsLabel.text = quizPoints + "/" + quizQuestions + " " + LanguageController.GetTranslation("Quizfragen beantwortet,");
	    imageQuizPointsLabel.text = imageQuizPoints + "/" + imageQuizMaxPoints + " " + LanguageController.GetTranslation("Punkte beim Bilderrätsel.");

        if (progress >= 1)
        {
            textDescription.SetActive(false);
            textSuccess.SetActive(true);
        }
        else
        {
            textDescription.SetActive(true);
            textSuccess.SetActive(false);
        }
    }

    public void Back()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(BackCoroutine());
    }

    public IEnumerator BackCoroutine()
    {
        if (SiteController.instance.previousSite != null)
        {
            if (SiteController.instance.previousSite.siteID == "MapSite")
            {
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("MapSite"));
            }
            else if (SiteController.instance.previousSite.siteID == "QuizSite")
            {
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("QuizSite"));
            }
            else if (SiteController.instance.previousSite.siteID == "DalliKlickSite")
            {
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DalliKlickSite"));
            }
            else
            {
                yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DashboardSite"));
            }
        }
        else
        {
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("DashboardSite"));
        }

        Reset();
        isLoading = false;
    }

    public void ShowInfo()
    {
        ToolsController.instance.OpenWebView("https://www.osnabruecker-land.de/");

        //if (isLoading) return;
        //isLoading = true;
        //StartCoroutine(ShowInfoCoroutine());
    }

    public IEnumerator ShowInfoCoroutine()
    {
        yield return null;
        isLoading = false;
    }

    public void Reset()
    {

    }
}
