using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StationListElement : MonoBehaviour
{
    public string stationId = "";
    public Image background;
    public Image previewImage;
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI numberLabel;

    /*
    public void ToggleFavorit(string tourId, string stationId)
    {
        favoritImage.SetActive(!favoritImage.activeInHierarchy);
        int active = favoritImage.activeInHierarchy ? 1:0;
        PlayerPrefs.SetInt(tourId + "_" + stationId, active);
    }
    */
}
