using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BatteryController : MonoBehaviour
{
    public GameObject batteryDialog;
    public TextMeshProUGUI batteryLevelLabel;

    [Space(10)]

    public bool batteryMessageShowed = false;
    public float batteryLevel = 1;

    [Space(10)]

    public float checkBatteryInterval = 30;
    public float currentCheckBatteryTime = 0;
    public float showWarningBelowPercentage = 0.3f;

    [Space(10)]

    public bool editorUseTestPercentage = false;
    public float editorTestBatteryLevel = 1;

    void Update()
    {
        if (batteryMessageShowed) return;

        currentCheckBatteryTime += Time.deltaTime;
        if(currentCheckBatteryTime >= checkBatteryInterval)
        {
            currentCheckBatteryTime = 0;

            if (IsPhotoCameraEnabled())
            {
                batteryLevel = SystemInfo.batteryLevel;

#if UNITY_EDITOR
                if (editorUseTestPercentage) { batteryLevel = editorTestBatteryLevel; }
#endif

#if !UNITY_EDITOR
                if (SystemInfo.batteryStatus == BatteryStatus.Charging) { return; }
#endif

                if (batteryLevel >= 0 && batteryLevel <= showWarningBelowPercentage)
                {
                    batteryMessageShowed = true;
                    batteryLevelLabel.text = (batteryLevel*100).ToString("F0") + "%";
                    StartCoroutine(ShowBatteryMessageCoroutine()); ;
                }
            }
        }
    }

    public bool IsPhotoCameraEnabled()
    {
        if (ARController.instance != null && ARController.instance.arSession.enabled) return true;

        if (WebcamController.instance != null && WebcamController.instance.webcamTexture != null && WebcamController.instance.webcamTexture.isPlaying) {      
            return true;
        }

        return false;
    }

    public IEnumerator ShowBatteryMessageCoroutine()
    {
        batteryDialog.SetActive(true);
        yield return new WaitForSeconds(8.0f);
        batteryDialog.SetActive(false);
    }
}
