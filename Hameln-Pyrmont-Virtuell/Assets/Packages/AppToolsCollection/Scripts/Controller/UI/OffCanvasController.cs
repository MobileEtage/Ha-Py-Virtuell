
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;

using TMPro;

// This controller handles the functionality off the side menu

public class OffCanvasController : MonoBehaviour {
	
	public float menuPos = -0.8f;
	public List<GameObject> mainMenuButtonsEnabled = new List<GameObject>();
	
	[Space(10)]

	public LayoutElement openArea;
	public LayoutElement offCanvas;
	
	[Space(10)]

	public List<GameObject> offCanvasMenus = new List<GameObject>();
	
	[Space(10)]

	public GameObject offCanvasContainer;
	public GameObject offCanvasBackground;
	public GameObject offCanvasMainMenu;
	
	[Space(10)]

	public float minSwipeWidthPercentage = 0.2f;
	public List<string> swipeAreaExcludedObjects = new List<string>();
	
	private Vector3 swipeHitPoint;
	private bool hitSwipe = false;
	private float openCloseTime = 0.5f;
	private bool isLoading = false;

	public static OffCanvasController instance;
	void Awake(){
		instance = this;
	}
	
	void Start(){		
		ToolsController.instance.SetScreenPosition( offCanvasContainer.GetComponent<RectTransform>(), menuPos, 0, false );
		offCanvasBackground.GetComponent<CanvasGroup>().alpha = 0;
		offCanvasBackground.SetActive(false);
		offCanvasContainer.SetActive(false);
	}
	
	// Adjust width on landscape device	
	private void AdjustOffCanvasWidth(){
		if( Screen.width > Screen.height ){
			openArea.flexibleWidth = 4;
			offCanvas.flexibleWidth = 2;
		}else{
			openArea.flexibleWidth = 1;
			offCanvas.flexibleWidth = 4;
		}
	}
	
	void Update(){
		//AdjustOffCanvasWidth();
		//DetectSwipe();		
	}

	public void OpenMenu(){

		//print("OpenMenu");

		offCanvasContainer.SetActive(true);
		ResetMenuPositions();
		ToolsController.instance.ResetScrollRect( offCanvasMainMenu.GetComponentInChildren<ScrollRect>());
		ToolsController.instance.SetScreenPosition( offCanvasContainer.GetComponent<RectTransform>(), menuPos, 0, false );
		ToolsController.instance.Move( offCanvasContainer.GetComponent<RectTransform>(), 0, 0, openCloseTime, 3f, true, false );
		ToolsController.instance.FadeInCanvasGroup( offCanvasBackground.GetComponent<CanvasGroup>(), openCloseTime );
	}
	
	public void ResetMenuPositions(){
		for( int i = 0; i < offCanvasMenus.Count; i++ ){
			float width = offCanvasMenus[i].GetComponent<RectTransform>().rect.width;
			offCanvasMenus[i].GetComponent<RectTransform>().anchoredPosition = new Vector2( width, 0 );
		}
	}
	
	public void CloseMenuImmediate(){

		Vector2 targetPosition = new Vector2( offCanvasContainer.GetComponent<RectTransform>().rect.width * -1, offCanvasContainer.GetComponent<RectTransform>().rect.height * 0);
		offCanvasContainer.GetComponent<RectTransform>().anchoredPosition = targetPosition;
		offCanvasBackground.GetComponent<CanvasGroup>().alpha = 0;
		offCanvasContainer.SetActive(false);
	}
	
	public void CloseMenu(){

		if (isLoading) return;
		isLoading = true;
		StartCoroutine(CloseMenuCoroutine());
	}

	public IEnumerator CloseMenuCoroutine()
    {
		if (!offCanvasContainer.GetComponent<Canvas>().enabled) { isLoading = false; yield break; }
		CanvasController.instance.DisableEventSystemForSeconds(openCloseTime + 0.05f);
		ToolsController.instance.Move(offCanvasContainer.GetComponent<RectTransform>(), menuPos, 0, openCloseTime, 3f, true, true);
		yield return StartCoroutine(ToolsController.instance.FadeOutCanvasGroupCoroutine(offCanvasBackground.GetComponent<CanvasGroup>(), openCloseTime, 0, offCanvasBackground));
		offCanvasContainer.SetActive(false);

		isLoading = false;
	}


	public void OpenCustomMenu( GameObject offCanvasMenu ){
		ToolsController.instance.ResetScrollRect( offCanvasMenu.GetComponentInChildren<ScrollRect>());
		ToolsController.instance.SetScreenPosition( offCanvasMenu.GetComponent<RectTransform>(), menuPos, 0, false );
		ToolsController.instance.Move( offCanvasMenu.GetComponent<RectTransform>(), 0, 0, openCloseTime, 3f, true, false );
	}
	
	public void CloseCustomMenu( GameObject offCanvasMenu ){
		CanvasController.instance.DisableEventSystemForSeconds( openCloseTime + 0.05f );
		ToolsController.instance.Move( offCanvasMenu.GetComponent<RectTransform>(), 1, 0, openCloseTime, 3f, true, false );
	}

	private void DetectSwipe(){

		if( Input.GetMouseButtonDown(0) && ValidSwipeArea() ){
			hitSwipe = true;
			swipeHitPoint = Input.mousePosition;
		}
		
		if( Input.GetMouseButtonUp(0) && hitSwipe ){
				
			hitSwipe = false;

			if( ValidSwipeArea() ){
				
				Vector3 dist = swipeHitPoint - Input.mousePosition;
				
				if( dist.x > Screen.width * minSwipeWidthPercentage ){
					CloseMenu();				
				}else if( dist.x < -Screen.width * minSwipeWidthPercentage ){
					OpenMenu();
				}
			}
		}
	}
	
	private bool ValidSwipeArea(){
		
		#if !UNITY_EDITOR
		if( Input.touchCount > 0 && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject( Input.touches[0].fingerId ) )				
		#else
		if ( EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
		#endif
				
		{
			PointerEventData pointer = new PointerEventData(EventSystem.current);
			pointer.position = Input.mousePosition;
	
			List<RaycastResult> raycastResults = new List<RaycastResult>();
			EventSystem.current.RaycastAll(pointer, raycastResults);
						
			if(raycastResults.Count > 0)
			{
				foreach(var go in raycastResults)
				{  			
					if( swipeAreaExcludedObjects.Contains( go.gameObject.name ) ){
						return false;
					}
				}
			}
		}
		
		return true;
	}

	public void OpenPrivacyPolicy(){
		ToolsController.instance.OpenWebView("https://www.die-etagen.de/");
	}
	
	public void OpenImprint(){
		ToolsController.instance.OpenWebView("https://www.die-etagen.de/");
	}
}
