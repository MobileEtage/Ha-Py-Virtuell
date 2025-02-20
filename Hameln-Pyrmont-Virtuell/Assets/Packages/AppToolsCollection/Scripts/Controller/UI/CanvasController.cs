
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

using TMPro;

// This controller has some tools to adjust a canvas

// It will adjust the canvas screen resolution 
// depending on the screen height in inches, so we have 
// a responsive user interface where ui-elements are smaller on bigger screens

// it also will adjust the height of the content holder, so 
// we get black borders if the device has a notch or rounded corners

public class CanvasController : MonoBehaviour {

	public Canvas canvas;

	[Header("Safe area options")]
	public RectTransform content;
	public bool updateReferenceResolution = true;
	public bool updateReferenceResolutionOnWebGL = true;
	public bool useSafeAreaBorders = true;
	public bool useSafeAreaBorderBottom = true;
	[Range(0,1)] 
	public float minSafeAreaHeightPercentage = 0.9f;	// The canvas will have at least this height: Screen.height * minSafeAreaHeightPercentage
	
	[Header("Set a test height for safe area testing (percentage of screen height)")]
	[Range(0,1)]
	public float editorSafeAreaTestHeightPercentage = 0.8f;
	
	[Header("Set a screen test height (in inch) for testing in the unity editor")]
	public float editorTestHeight = 4f;	// 7.76 iPad 2018
	
	[Header("Add a black area to the top of the screen where the iOS status bar lies")]
	public bool addIOSStatusBarArea = false;
	public bool testIOSStatusBarInEditor = false;

	#if UNITY_IOS
	private float iOSPointSize = 163f;							// Standard iOS point reference value --> numberOfPoints * DPI / 163 = numberOfPixels (163 DPI of first iPhone)
	#endif

	[Header("The EventSystem")]
	public EventSystem eventSystem;

	private float referenceResolutionX = 1080;
	private float referenceResolutionY = 2337;
	//private float maxReferenceResolutionY = 3000;
	public float maxReferenceResolutionY = 1920;
	private float defaultInches = 6.0f;
	private float maxInches = 10.35f; // iPad Pro (7.76f for iPad Air)
	private float minInches = 2.91f; // iPhone 4

	private GameObject topBorder;
	private float topBorderAreaHeight = 0;
	private GameObject bottomBorder;
	private float disableEventSystemTime = 0;
	
	private bool canvasAdjusted = false;
    private Vector2 referenceResolution = Vector2.zero;

    public static CanvasController instance;
	void Awake(){
		instance = this;
	}
	
	void Start () {
		StartCoroutine( InitCanvasCoroutine() );
	}
	
	public bool IsReady(){
		return canvasAdjusted;
	}
	
	public IEnumerator InitCanvasCoroutine(){
		
		yield return new WaitForEndOfFrame();
		
		#if UNITY_WEBGL
		if( updateReferenceResolutionOnWebGL ){
			SetCanvasReferenceResolution();					
			yield return new WaitForEndOfFrame();
		}
		#else
		if( updateReferenceResolution ){
			SetCanvasReferenceResolution();					
			yield return new WaitForEndOfFrame();
		}
		#endif
		
		if(useSafeAreaBorders){
			AdjustSafeArea();
		}
		
		#if!UNITY_EDITOR
		testIOSStatusBarInEditor = false;
		#endif
		
		#if UNITY_IOS && !UNITY_EDITOR
		if(addIOSStatusBarArea ){
		AdjustStatusBarArea();
		}
		#elif UNITY_EDITOR
		if(testIOSStatusBarInEditor ){
			AdjustStatusBarArea();
		}
		#endif

		yield return new WaitForEndOfFrame();

		print("CanvasController " + canvas.GetComponent<CanvasScaler>().referenceResolution);
		
		canvasAdjusted = true;
	}

	public void SetCanvasReferenceResolution(){
		
		float inchesHeight = GetScreenHeightInches();
		float extraSize = inchesHeight - defaultInches;
				
		if(extraSize > 0)
		{
			float extraHeight = extraSize / ( maxInches - defaultInches ) * (maxReferenceResolutionY - referenceResolutionY) ;
			canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(referenceResolutionX, referenceResolutionY + extraHeight);
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
		
	// calculate the height of the iOS status bar and add a black border to the top of the screen
	public void AdjustStatusBarArea(){
		
		#if UNITY_IOS
		if( Screen.dpi > 0 ){

		try{
				
		float iOSStatusBarPixelSize = 40;	//regular iPhone should return 40, iPhone X 88
				#if !UNITY_EDITOR
		iOSStatusBarPixelSize = GetStatusBarHeight();
				#endif

		if( iOSStatusBarPixelSize <= 0 ){
		return;
		}
				
		if(topBorder == null){
		topBorder = CreateUIImage( "TopBorder", Color.black, canvas.transform );
		}
		iOSStatusBarPixelSize = Mathf.Clamp( iOSStatusBarPixelSize, 0, (float)Screen.height * (1-minSafeAreaHeightPercentage) );	// clamp to a max size
				
		float ratioCanvasHeightScreenHeight = canvas.GetComponent<RectTransform>().rect.height / (float)Screen.height;			
		iOSStatusBarPixelSize *= ratioCanvasHeightScreenHeight;		// convert from screen pixels to canvas pixels
		if( topBorderAreaHeight < iOSStatusBarPixelSize ){
		topBorderAreaHeight = iOSStatusBarPixelSize;
		}
			
		topBorder.GetComponent<RectTransform>().offsetMax = new Vector2(0, 0);
		topBorder.GetComponent<RectTransform>().offsetMin = new Vector2(0, canvas.GetComponent<RectTransform>().rect.height - topBorderAreaHeight );
		content.offsetMax = new Vector2(0, -topBorderAreaHeight);	
				
		}catch( Exception e ){
		print( "Error setting statusbarHeight " + e.Message );
		}
		}
		#endif
	}
	
	// Native helper function to get the iOS device status bar point height
	#if UNITY_IOS
	[DllImport ("__Internal")]
	private static extern float GetStatusBarHeight ();
	#endif
	
	public void AdjustSafeArea(){

		print("Screen.height " + Screen.height);
		print("Screen.safeArea.height " + Screen.safeArea.height);
		
		#if !UNITY_EDITOR		
		if( Screen.height == Screen.safeArea.height || IsNotSafeAreaDevice() ) {
		return;
		}
		#endif
		
		float height = (float)Screen.height;
		float safeHeight = Screen.safeArea.height;
		
		#if UNITY_EDITOR
		safeHeight = height * editorSafeAreaTestHeightPercentage;
		#endif
		
		safeHeight = Mathf.Clamp( safeHeight, height * minSafeAreaHeightPercentage, height);
		
		float ratio = safeHeight / height;		
		if( ratio < 1 ){
				
			topBorder = CreateUIImage( "TopBorder", Color.black, canvas.transform );

			float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
			topBorderAreaHeight =  canvasHeight * ( 1f - ratio ) * 0.5f;

			topBorder.GetComponent<RectTransform>().offsetMax = new Vector2(0, 0);
			topBorder.GetComponent<RectTransform>().offsetMin = new Vector2(0, canvasHeight - topBorderAreaHeight );
			content.offsetMax = new Vector2(0, -topBorderAreaHeight);
			
			if(useSafeAreaBorderBottom){
				
				bottomBorder = CreateUIImage( "BottomBorder", Color.black, canvas.transform );

				bottomBorder.GetComponent<RectTransform>().offsetMin = new Vector2(0, 0);
				bottomBorder.GetComponent<RectTransform>().offsetMax = new Vector2(0, -(canvasHeight - topBorderAreaHeight) );
				content.offsetMin = new Vector2(0, topBorderAreaHeight);

			}
		}
	}
	
	
	private GameObject CreateUIImage( string objName, Color imageColor, Transform parentObject ){
		
		GameObject obj = new GameObject(objName, typeof(RectTransform));
		obj.transform.SetParent( parentObject );
		obj.transform.localScale = Vector3.one;
		obj.GetComponent<RectTransform>().anchorMin = new Vector2(0,0);
		obj.GetComponent<RectTransform>().anchorMax = new Vector2(1,1);
		obj.GetComponent<RectTransform>().offsetMin = new Vector2(0,0);
		obj.GetComponent<RectTransform>().offsetMax = new Vector2(0,0);

		obj.AddComponent<Image>();
		obj.GetComponent<Image>().color = imageColor;
		
		return obj;
	}
	
	// Every button which starts an animation should call this
	// to avoid buttons clicks and side effects during the animation
	public void DisableEventSystemForSeconds( float seconds ){
		eventSystem.enabled = false;
		disableEventSystemTime = seconds;
		StopCoroutine("DisableEventSystemCoroutine");
		StartCoroutine("DisableEventSystemCoroutine");
	}
	
	private IEnumerator DisableEventSystemCoroutine(){
		yield return new WaitForSeconds( disableEventSystemTime );
		eventSystem.enabled = true;
	}
	
	public float GetCanvasHeight(){
		return canvas.GetComponent<RectTransform>().rect.height;
	}
	
	public float GetCanvasWidth(){
		return canvas.GetComponent<RectTransform>().rect.width;
	}
	
	public float GetContentHeight(){
		return content.GetComponent<RectTransform>().rect.height;
	}
	
	public float GetContentWidth(){
		return content.GetComponent<RectTransform>().rect.width;
	}
	
	public int GetContentPixelWidth(){
		return (int)(GetContentWidth() / GetCanvasWidth() * Screen.width);
	}
	
	public int GetContentPixelHeight(){
		return (int)(GetContentHeight() / GetCanvasHeight() * Screen.height);
	}
	
	public float GetExactContentPixelHeight(){
		return GetContentHeight() / GetCanvasHeight() * Screen.height;
	}
	
	public Vector2 CanvasTouchPosition( Vector2 screenPosition ){
		return new Vector2( 
			GetCanvasWidth() / (float)Screen.width * screenPosition.x,
			GetCanvasHeight() / (float)Screen.height * screenPosition.y
		);
	}
	
	public int GetBorderTopHeight(){
		if( !useSafeAreaBorders || topBorder == null ){
			return 0;
		}
		
		int pixelHeight = (int) ( ( topBorder.GetComponent<RectTransform>().rect.height / GetCanvasHeight() ) * Screen.height );
		return pixelHeight;
	}
	
	public int GetBorderBottomPixelHeight(){
		if( !useSafeAreaBorders || !useSafeAreaBorderBottom || bottomBorder == null ){
			return 0;
		}
		
		int pixelHeightBorderBottom = (int) ( ( bottomBorder.GetComponent<RectTransform>().rect.height / GetCanvasHeight() ) * Screen.height );
		return pixelHeightBorderBottom;
	}
	
	public int GetSafeAreaHeight(){
		if( !useSafeAreaBorders ){
			return 0;
		}
		
		int height = 0;
		if( topBorder != null ){
			float heightBorder = topBorder.GetComponent<RectTransform>().rect.height;
			int pixelHeightBorder = (int) ( ( topBorder.GetComponent<RectTransform>().rect.height / GetCanvasHeight() ) * Screen.height );
			height += pixelHeightBorder;
		}
		
		if( bottomBorder != null ){
			height += (int) ( ( bottomBorder.GetComponent<RectTransform>().rect.height / GetCanvasHeight() ) * Screen.height );
		}
		
		return height;
	}
	
	// To avoid creating safe area borders we define the devices which we know they do not need a safe area
	public bool IsNotSafeAreaDevice(){
		
#if UNITY_IOS
		if( 
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPad1Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPad2Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPad3Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPad4Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPad5Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPadAir1 ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPadAir2 ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPadMini1Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPadMini2Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPadMini3Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPadMini4Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPadPro10Inch1Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPadPro10Inch2Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPadPro1Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPadPro2Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone3G ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone3GS ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone4 ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone4S ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone5 ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone5C ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone5S ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone6 ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone6Plus ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone6S ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone6SPlus ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone7 ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone7Plus ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone8 ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhone8Plus ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPhoneSE1Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPodTouch1Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPodTouch2Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPodTouch3Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPodTouch4Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPodTouch5Gen ||
		UnityEngine.iOS.Device.generation == DeviceGeneration.iPodTouch6Gen
		){
		return true;
		}
#endif
		
		// This devices probably have safe area
		//DeviceGeneration.iPadPro3Gen
		//DeviceGeneration.iPadPro11Inch
		//DeviceGeneration.iPhoneX
		//DeviceGeneration.iPhoneXR
		//DeviceGeneration.iPhoneXS
		//DeviceGeneration.iPhoneXSMax
		
		return false;
	}

    public void SetReferenceResolutionLandscape()
    {
        if (referenceResolution.x == 0 && referenceResolution.y == 0)
        {
            referenceResolution = canvas.GetComponent<CanvasScaler>().referenceResolution;
        }

        canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(referenceResolution.x, referenceResolution.x);
    }

    public void SetReferenceResolutionPortrait()
    {
        if (referenceResolution.x != 0 && referenceResolution.y != 0)
        {
            canvas.GetComponent<CanvasScaler>().referenceResolution = referenceResolution;
        }
    }

    public IEnumerator SwitchToLandscapeCoroutine()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        yield return new WaitForSeconds(0.25f);
    }
}
