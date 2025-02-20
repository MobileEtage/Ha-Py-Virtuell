using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightMenuImage : MonoBehaviour
{
    public GameObject referenceHeightObject;
    public bool initialized = false;
    public bool blinkEnded = false;
    public bool isBlinking = false;
    public float blinkDuration = 3;

    private float blinkTime = 0;

    void Start()
    {
        
    }

    void Update()
    {
        if (!initialized) return;
        if (!referenceHeightObject.activeInHierarchy) return;
        GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, referenceHeightObject.GetComponent<RectTransform>().rect.height+60);
 
        if (GetComponent<CanvasGroup>() == null) return;

        float targetAlpha = 1;
        if (isBlinking)
        {
            targetAlpha = Mathf.PingPong(blinkTime, 1f);
            //targetAlpha = 0.5f + targetAlpha;
            blinkTime += Time.deltaTime * 4;
            if(blinkTime >= blinkDuration) { isBlinking = false; blinkEnded = true; }
        }

        GetComponent<CanvasGroup>().alpha = targetAlpha;

        if (blinkEnded)
        {
            if (Input.GetMouseButtonDown(0))
            {
                blinkEnded = false;
                gameObject.SetActive(false);
            }
        }
    }

    public void UpdateSize()
    {
        GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, referenceHeightObject.GetComponent<RectTransform>().rect.height + 60);
    }

    public void Blink()
    {
        isBlinking = true;
        blinkTime = 1;
    }
}
