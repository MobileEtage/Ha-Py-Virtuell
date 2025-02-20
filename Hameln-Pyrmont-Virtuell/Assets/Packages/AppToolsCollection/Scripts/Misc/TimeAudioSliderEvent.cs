using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class TimeAudioSliderEvent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public string sliderType = "AudiothekController";

    private bool deselecting = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (sliderType == "AudiothekController") { AudiothekController.instance.audioSliderSelected = true; }
        else if (sliderType == "AudioController") { AudioController.instance.audioSliderSelected = true; }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (sliderType == "AudiothekController") { AudiothekController.instance.SetAudioTimeBySlider(); }
        else if (sliderType == "AudioController") { AudioController.instance.SetAudioTimeBySlider(); }

        if (!deselecting)
        {
            deselecting = true;
            //StartCoroutine(deselectTimer());
        }
    }

    IEnumerator deselectTimer()
    {
        yield return new WaitForSeconds(0.1f);
        if (sliderType == "AudiothekController") { AudiothekController.instance.audioSliderSelected = false; }
        else if (sliderType == "AudioController") { AudioController.instance.audioSliderSelected = false; }
        deselecting = false;
    }
}
