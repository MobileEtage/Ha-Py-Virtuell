using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScanAnimator : MonoBehaviour
{
	public bool enableScanAnimationOnStart = false;
	public float startDelay = 2;

	public GameObject scanObject;
	public GameObject iPhoneAnimation;
	public GameObject iPadAnimation;
    public GameObject scanInfo;
    public GameObject scanAnimationUI;
    public GameObject depthMask_iPhone;
    public GameObject depthMask_iPad;
	public GameObject screen_iPhone;
	public GameObject screen_iPad;
	
    public float fadeTime = 0.5f;
	
	[Header("Set a screen test height (in inch) for testing in the unity editor")]
	public float editorTestHeight = 4f;
	
	private float maxInches = 10.35f; // iPad Pro (7.76f for iPad Air)
	private float minInches = 2.91f; // iPhone 4
	private float minTabletHeight = 6.0f;
	
	private bool isPlaying = false;
	private bool isHiding = false;

	public static ScanAnimator instance;
	void Awake()
	{
		instance = this;	
	}

	void Start(){
		
		if( GetScreenHeightInches() > minTabletHeight ){
			iPadAnimation.SetActive(true);
		}else{
			iPhoneAnimation.SetActive(true);

		}
				
		if( enableScanAnimationOnStart ){
			StartCoroutine(StartWithDelayCoroutine());
		}
	}
	
	public float GetScreenHeightInches(){
		
		float screenHeightInches = minInches;
		
		#if UNITY_EDITOR
		screenHeightInches = Mathf.Clamp(editorTestHeight, minInches, maxInches);
		#else
		
		if (Screen.dpi > 0)
		{
		screenHeightInches = (float)Screen.height / Screen.dpi;
		screenHeightInches = Mathf.Clamp(screenHeightInches, minInches, maxInches);
		}
		#endif
		
		return screenHeightInches;	
	}
	
	public void StartWithDelay(){
		StartCoroutine("StartWithDelayCoroutine");
	}
	
	private IEnumerator StartWithDelayCoroutine(){
		yield return new WaitForSeconds(startDelay);
		ShowScanAnimation();
	}
	
	public void ShowScanAnimation(){
		if (isPlaying || scanObject.activeInHierarchy) return;
		isPlaying = true;
		isHiding = false;
		StartCoroutine("ShowScanAnimationCoroutine");
	}

	public void HideScanAnimation(){
		if (isHiding || !scanObject.activeInHierarchy) return;
		isHiding = true;
		isPlaying = false;
		StartCoroutine("HideScanAnimationCoroutine");
	}
	
	private IEnumerator ShowScanAnimationCoroutine(){

		StopCoroutine("HideScanAnimationCoroutine");

		scanObject.SetActive(true);
		scanInfo.SetActive(true);
        scanInfo.GetComponent<Animator>().Play("ShowScan");
        scanAnimationUI.GetComponent<Animator>().Play("ShowScan");
        ToolsController.instance.FadeIn(scanObject, false, fadeTime);
		yield return new WaitForSeconds(fadeTime);

		isPlaying = false;
	}

	private IEnumerator HideScanAnimationCoroutine()
	{
		StopCoroutine("ShowScanAnimationCoroutine");

		scanInfo.GetComponent<Animator>().Play("HideScan");
        scanAnimationUI.GetComponent<Animator>().Play("HideScan");
        ToolsController.instance.FadeOut(scanObject, false, fadeTime);
		yield return new WaitForSeconds(fadeTime);
		scanObject.SetActive(false);
		isHiding = false;
	}
	
	public void ShowHideDepthMask(bool show)
	{
        depthMask_iPhone.SetActive(show);
        depthMask_iPad.SetActive(show);
	}
    
	public void ShowHideMobileScreen(bool show)
	{
		screen_iPhone.SetActive(show);
		screen_iPad.SetActive(show);
	}
	
    public void Reset(){
		
		StopAllCoroutines();
		scanObject.SetActive(false);
		isHiding = false;
		isPlaying = false;
    }

}
