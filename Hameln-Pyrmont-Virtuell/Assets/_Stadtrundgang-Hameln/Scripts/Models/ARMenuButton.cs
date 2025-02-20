using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARMenuButton : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        float targetAlpha = 35f / 255f;
        if (!ARMenuController.instance.isMenuImmediateOpen) { targetAlpha = 0; }
        float alpha = Mathf.Lerp(GetComponentInChildren<LeTai.TrueShadow.TrueShadow>().Color.a, targetAlpha, Time.deltaTime * 10);

        GetComponentInChildren<LeTai.TrueShadow.TrueShadow>().Color = new Color(0,0,0,alpha);
    }
}
