using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class CustomTimeSliderEvent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public string videoType;

    bool deselecting = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        if(videoType == "IntroVideoController") { IntroVideoController.instance.videoSliderSelected = true; }    
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (videoType == "IntroVideoController") { IntroVideoController.instance.SetVideoTimeBySlider(); }
        if (!deselecting)
        {
            deselecting = true;
            //StartCoroutine(deselectTimer());
        }
    }

    IEnumerator deselectTimer()
    {
        yield return new WaitForSeconds(0.1f);
        if (videoType == "IntroVideoController") { IntroVideoController.instance.videoSliderSelected = false; }
        deselecting = false;
    }
}
