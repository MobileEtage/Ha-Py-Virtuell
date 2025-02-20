using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
	public GameObject pointer;
	public Camera mainCamera;
	
	public GameObject switchToLandscapeIcon;
	public GameObject switchToPortraitIcon;
	
	public static UIController instance;
	void Awake()
	{
		instance = this;
	}
	
	void Update(){
		
		//#if UNITY_EDITOR
		ShowPointer();
		//#endif
	}
	
	public void ShowPointer(){
		
		if( AdminUIController.instance.adminMenuEnabled && Input.GetMouseButton(0) ){
	
			Vector2 screenPosition = Vector2.zero;
			
			if( RectTransformUtility.ScreenPointToLocalPointInRectangle( CanvasController.instance.canvas.GetComponent<RectTransform>(), Input.mousePosition, null, out screenPosition)){

				pointer.SetActive(true);
				pointer.GetComponent<RectTransform>().anchoredPosition = screenPosition;
			}
			else{
				pointer.SetActive(false);
			}
		}
		else{
			
			pointer.SetActive(false);
		}
	}
}
