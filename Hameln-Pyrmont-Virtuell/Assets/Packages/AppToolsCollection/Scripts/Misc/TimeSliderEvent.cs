using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class TimeSliderEvent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	public VideoTarget videoTarget;
	
	bool deselecting = false;
    
	public void OnPointerDown(PointerEventData eventData) {
		VideoController.instance.videoSliderSelected = true;
	}
    
	public void OnPointerUp(PointerEventData eventData) {
		VideoController.instance.SetVideoTimeBySlider( videoTarget );
		if (!deselecting)
		{
			deselecting = true;
			//StartCoroutine(deselectTimer());
		}
	}

	IEnumerator deselectTimer()
	{
		yield return new WaitForSeconds(0.1f);
		VideoController.instance.videoSliderSelected = false;
		deselecting = false;
	}
}
