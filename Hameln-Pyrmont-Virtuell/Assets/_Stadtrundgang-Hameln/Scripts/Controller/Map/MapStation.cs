using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mapbox.Utils;
using SimpleJSON;

public class MapStation : MonoBehaviour
{
    public string stationId = "";
    public string numberText = "";
    public string filterType = "";

    [Space(10)]

    public JSONNode dataJson;
    public Vector2d geoPosition;
    public Image markerImage;
    public Image backgroundImage;
    public TextMeshProUGUI markerLabel;
    public GameObject finishedUI;

    [Space(10)]

    public GameObject multiLocationIndicator;
    public int multiLocationId = -1;
    public bool isBlinking = false;

    private float blinkTime = 0;
    private float blinkCount = 0;
    private float timer = 1;
    private bool paused = false;
    private bool initialized = false;

    void Start()
    {
        if (filterType == "" && backgroundImage != null)
        {
            backgroundImage.color = Params.mapStationNotFinishedBackgroundColor;
        }
    }

    private void Update()
    {
        if (GetComponent<CanvasGroup>() == null) return;
        Animate();
    }

    void OnEnable()
    {
        isBlinking = false;
    }

    void OnDisable()
    {
        isBlinking = false;
    }

    public void Animate()
    {
        float targetAlpha = 1;

        if (isBlinking)
        {
            if (!paused)
            {
                targetAlpha = Mathf.PingPong(blinkTime, 1f);
                blinkTime += Time.deltaTime * 4;
            }

            if (!paused)
            {
                timer += Time.deltaTime;
                if (timer >= 1 && blinkCount == 0) { isBlinking = false; }
                else if (timer >= 1) { paused = true; timer = 0; blinkCount--; }
            }
            else
            {
                timer += Time.deltaTime;
                if (timer >= 3) { paused = false; timer = 0; blinkTime = 1; }
            }
        }

        GetComponent<CanvasGroup>().alpha = targetAlpha;
    }

    public void Blink()
    {
        isBlinking = true;
        paused = false;
        blinkTime = 1;
        timer = 0;
    }

    public void Blink(int count)
    {
        blinkCount = 10;
        isBlinking = true;
        paused = false;
        blinkTime = 1;
        timer = 0;
    }
}
