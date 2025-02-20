using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextController : MonoBehaviour
{
    public bool shouldScaleFont = false;

    [Range(1, 3)]
    public float textScale = 1.0f;
    private float currentTextScale = 1.0f;

    private int sendRatePerSecond = 10;
    private float sendIntervall = 1;
    private float sendTimer = 0f;

    private int sendRatePerSecond2 = 1;
    private float sendIntervall2 = 1;
    private float sendTimer2 = 0f;

    public static TextController instance;
    void Awake()
    {
        instance = this;

        if (PlayerPrefs.HasKey("FontScale")) { 
            textScale = PlayerPrefs.GetFloat("FontScale", 1.0f);          
        }
    }

    void Update()
    {
        if (shouldScaleFont) { 

            UpdateTextScalePerIntervall();
            //UpdateTextScalePerIntervall2();
        }
    }

    public void UpdateTextScale(float percentage)
    {
        textScale = percentage;
        UpdateTextScale();
    }

    public void UpdateTextScale()
    {
        if(textScale != currentTextScale)
        {
            currentTextScale = textScale;
            PlayerPrefs.SetFloat("FontScale", currentTextScale);

            TextMeshProUGUI[] labels = Resources.FindObjectsOfTypeAll(typeof(TextMeshProUGUI)) as TextMeshProUGUI[];
            foreach (TextMeshProUGUI t in labels)
            {
                if (t.GetComponent<TextOptions>() == null){ t.gameObject.AddComponent<TextOptions>(); }
                if (t.GetComponent<TextOptions>() != null){ t.GetComponent<TextOptions>().UpdateTextScale(currentTextScale);}
            }
        }
    }

    public void UpdateTextScalePerIntervall()
    {
        float deltaTime = Time.deltaTime;
        sendTimer += deltaTime;

        sendIntervall = 1f / sendRatePerSecond;
        if (sendIntervall > 0)
        {
            while (sendTimer >= sendIntervall)
            {
                sendTimer -= sendIntervall;
                UpdateTextScale();
            }
        }
    }

    public void UpdateTextScalePerIntervall2()
    {
        float deltaTime = Time.deltaTime;
        sendTimer2 += deltaTime;

        sendIntervall2 = 1f / sendRatePerSecond2;
        if (sendIntervall2 > 0)
        {
            while (sendTimer2 >= sendIntervall2)
            {
                sendTimer2 -= sendIntervall2;

                TextMeshProUGUI[] labels = Resources.FindObjectsOfTypeAll(typeof(TextMeshProUGUI)) as TextMeshProUGUI[];
                foreach (TextMeshProUGUI t in labels)
                {
                    if (t.GetComponent<TextOptions>() == null) { t.gameObject.AddComponent<TextOptions>(); }
                    //if (t.GetComponent<TextOptions>() != null) { t.GetComponent<TextOptions>().UpdateTextScale(currentTextScale); }
                }
            }
        }
    }
}
