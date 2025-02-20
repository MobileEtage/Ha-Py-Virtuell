
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

using TMPro;
using SimpleJSON;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ToolsController : MonoBehaviour {
		
	[HideInInspector] public bool isMovingRectTransform = false;
	[HideInInspector] public bool isFadingCanvasGroup = false;
	private bool useUniqueSavePathNameFromURL = true;
	
	private const string MatchEmailPattern =
	@"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
	+ @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
	+ @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
	+ @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})$";
	
	private GameObject webViewGameObject;
	
	#if UNITY_ANDROID && !UNITY_EDITOR
	AndroidJavaClass androidInterfaceClass;
	AndroidJavaObject androidInterfaceController { get { return androidInterfaceClass.GetStatic<AndroidJavaObject>("instance"); } }
	#endif
	
	public static ToolsController instance;
	void Awake () {
		instance = this;
	}
	
	void Start(){

		//PlayerPrefs.SetInt("guideInfoARShowed", 0);
		//PlayerPrefs.SetInt("guideInfoSelfieShowed", 0);

		//InitAndroidNativeToolsController();
		//SetCountryCode();
	}

    private void Update()
    {
#if UNITY_EDITOR
        //if (Input.GetKeyDown(KeyCode.S)) { ScreenCapture.CaptureScreenshot("screenshot_" + Screen.width + "x" + Screen.height + "_" +  DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + ".png"); }
#endif
	}

    private void InitAndroidNativeToolsController(){
		
		#if UNITY_ANDROID && !UNITY_EDITOR

		if( androidInterfaceClass == null) androidInterfaceClass = new AndroidJavaClass("de.dieetagen.tetrawatertest.ToolsController");
		androidInterfaceClass.CallStatic( "Initialize", this.gameObject.name );
		
		if( androidInterfaceController != null ){
			
		var androidJC = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		var unityActivity = androidJC.GetStatic<AndroidJavaObject>("currentActivity");

		androidInterfaceController.Call(
		"SetAppContext", 
		unityActivity
		);			
		}
		
		#endif
	}
	
	private void SetCountryCode(){
		
		string countryCode = "";

		#if UNITY_IOS && !UNITY_EDITOR
		countryCode = GetCountryCode();
		#elif UNITY_ANDROID && !UNITY_EDITOR
		if( androidInterfaceController != null ){
		countryCode = androidInterfaceController.Call<string>("GetCountryCode");
		}
		#else
		countryCode = LanguageController.GetEditorTestCountry();
		#endif
		
		if( string.IsNullOrEmpty(countryCode) || countryCode.Length > 2 ){
			countryCode = LanguageController.GetLanguageCode().ToUpper();
		}
		LanguageController.SetCountryCode( countryCode );
		print( "LanguageController.GetCountryCode() " + LanguageController.GetCountryCode() );
	}
	
	public void SaveImageToGallery( string savePath, string filename ){
		#if UNITY_ANDROID && !UNITY_EDITOR
		if( androidInterfaceController != null ){
		androidInterfaceController.Call("SaveImageToGallery", savePath, filename);
		}
		#endif
	}
	
	public void DebugInfo( string info ){
		print(info);
	}
	
	public bool IsAnimating(){
		return isMovingRectTransform || isFadingCanvasGroup;
	}
	
	/************************************************************************************************************************/
	// Helper functions to move and position a RectTransform ui-element relative on screen
	
	// percentageVertical -1 means completly under the screen
	// percentageVertical 1 means completly over the screen
	// percentageVertical 0 means in the center of screen
	
	// percentageHorizontal -1 means completly left beside the screen
	// percentageHorizontal 1 means completly right beside the screen
	// percentageHorizontal 0 means in the center of screen
	/************************************************************************************************************************/
	
	public void SetScreenPosition( RectTransform obj, float percentageHorizontal, float percentageVertical, bool activate ){		
		float height = obj.rect.height;
		float width = obj.rect.width;
		obj.anchoredPosition = new Vector2( width * percentageHorizontal, height * percentageVertical);

		if( obj.GetComponent<Canvas>() != null ){
			obj.GetComponent<Canvas>().enabled = activate;
		}else{
			obj.gameObject.SetActive(activate);
		}
		
	}
	
	public void Move( RectTransform obj, float percentageHorizontal, float percentageVertical, float moveTime, float speedCurveFaktor, bool activateOnStart, bool deactivateOnEnd ){
		if( isMovingRectTransform ) return;
		isMovingRectTransform = true;
		
		StartCoroutine( MoveCoroutine(obj, percentageHorizontal, percentageVertical, moveTime, speedCurveFaktor, activateOnStart, deactivateOnEnd) );
	}

	public IEnumerator MoveCoroutine( RectTransform obj, float percentageHorizontal, float percentageVertical, float moveTime, float speedCurveFaktor, bool activateOnStart, bool deactivateOnEnd)
	{
		float height = obj.rect.height;
		float width = obj.rect.width;
		Vector2 targetPosition = new Vector2( width * percentageHorizontal, height * percentageVertical);
		
		if( activateOnStart ){
			if( obj.GetComponent<Canvas>() != null ){
				obj.GetComponent<Canvas>().enabled = true;
			}else{
				obj.gameObject.SetActive(true);
			}
		}
				
		Vector2 startPosition = obj.anchoredPosition;

		float currentTime = 0;
		float timePassed = 0;
		float delta = Time.deltaTime;


		// Erst schnell, dann langsam
		timePassed = moveTime;
		while (timePassed > 0)
		{
			obj.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, currentTime);

			if (timePassed > 0){
				float faktor = Mathf.Pow(timePassed / moveTime, speedCurveFaktor);
				currentTime = 1-faktor;
			}

			delta = Time.deltaTime;

			if (timePassed - delta < 0){
				timePassed = 0;
			}
			else{
				timePassed -= delta;
			}

			yield return null;
		}

		obj.anchoredPosition = targetPosition;
		
		if( deactivateOnEnd ){
			if( obj.GetComponent<Canvas>() != null ){
				obj.GetComponent<Canvas>().enabled = false;
			}else{
				obj.gameObject.SetActive(false);
			}
		}
		
		isMovingRectTransform = false;
	}
	
	public IEnumerator MoveCoroutine( RectTransform obj, float percentageHorizontal, float percentageVertical, float moveTime, float speedCurveFaktor, bool activateOnStart, bool deactivateOnEnd, Action<bool> Callback)
	{
		isMovingRectTransform = true;
		
		//float height = canvas.GetComponent<RectTransform>().rect.height;
		float height = obj.rect.height;
		float width = obj.rect.width;
		Vector2 targetPosition = new Vector2( width * percentageHorizontal, height * percentageVertical);
		
		if( activateOnStart ){
			
			obj.gameObject.SetActive(true);
			if( obj.GetComponent<Canvas>() != null ){
				obj.GetComponent<Canvas>().enabled = true;
			}
		}
		
		Vector2 startPosition = obj.anchoredPosition;

		float currentTime = 0;
		float timePassed = 0;
		float delta = Time.deltaTime;


		// Erst schnell, dann langsam
		timePassed = moveTime;
		while (timePassed > 0)
		{
			obj.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, currentTime);

			if (timePassed > 0){
				float faktor = Mathf.Pow(timePassed / moveTime, speedCurveFaktor);
				currentTime = 1-faktor;
			}

			delta = Time.deltaTime;

			if (timePassed - delta < 0){
				timePassed = 0;
			}
			else{
				timePassed -= delta;
			}

			yield return null;
		}
		 
		obj.anchoredPosition = targetPosition;
		
		if( deactivateOnEnd ){
			if( obj.GetComponent<Canvas>() != null ){
				obj.GetComponent<Canvas>().enabled = false;
			}else{
				obj.gameObject.SetActive(false);
			}
		}		
		Callback(true);
		
		isMovingRectTransform = false;

	}
	
	
	/************************************************************************************************************************/
	// Helper functions to fade a canvas group
	/************************************************************************************************************************/
	
	public void FadeInCanvasGroup( CanvasGroup canvasGroup, float fadingTime, float targetAlpha = 1, float delay = 0, GameObject objectToHide = null ){
		if( isFadingCanvasGroup ) return;
		isFadingCanvasGroup = true;
		
		StartCoroutine( FadeInCanvasGroupCoroutine(canvasGroup, fadingTime, targetAlpha, delay, objectToHide) );
	}
	
	public IEnumerator FadeInCanvasGroupCoroutine( CanvasGroup canvasGroup, float fadingTime, float targetAlpha = 1, float delay = 0, GameObject objectToHide = null ){

		canvasGroup.alpha = 0;
		
		if( delay > 0 ){
			yield return new WaitForSeconds(delay);
		}
		
		canvasGroup.gameObject.SetActive(true);
		if( canvasGroup.GetComponent<Canvas>() != null ){
			canvasGroup.GetComponent<Canvas>().enabled = true;
		}
		
		float alpha = 0;
		float val = 0;
		while( val < 1 ){
			canvasGroup.alpha = alpha;
			yield return null;
			val += Time.deltaTime / fadingTime;
			alpha = Mathf.Lerp(0, targetAlpha, val);
		}
		
		canvasGroup.alpha = targetAlpha;
		
		if(objectToHide != null) {
			if( objectToHide.GetComponent<Canvas>() != null ){
				objectToHide.GetComponent<Canvas>().enabled = false;
			}else{
				objectToHide.SetActive(false);
			}
		}
		
		isFadingCanvasGroup = false;
	}
	
	public IEnumerator FadeInCanvasGroupCoroutine( CanvasGroup canvasGroup, float fadingTime, float delay, GameObject objectToHide, Action<bool> Callback ){

		if( delay > 0 ){
			yield return new WaitForSeconds(delay);
		}
		
		if( canvasGroup.GetComponent<Canvas>() != null ){
			canvasGroup.GetComponent<Canvas>().enabled = true;
		}else{
			canvasGroup.gameObject.SetActive(true);
		}
		
		float alpha = 0;
		while( alpha < 1 ){
			canvasGroup.alpha = alpha;
			yield return null;
			alpha += Time.deltaTime / fadingTime;
		}
		
		canvasGroup.alpha = 1;
		if(objectToHide != null) {
			if( objectToHide.GetComponent<Canvas>() != null ){
				objectToHide.GetComponent<Canvas>().enabled = false;
			}else{
				objectToHide.SetActive(false);
			}
		}		
		Callback(true);
		
		isFadingCanvasGroup = false;
	}
	
	public void FadeOutCanvasGroup( CanvasGroup canvasGroup, float fadingTime, float delay = 0, GameObject objectToHide = null ){
		if( isFadingCanvasGroup ) return;
		isFadingCanvasGroup = true;
		
		StartCoroutine( FadeOutCanvasGroupCoroutine(canvasGroup, fadingTime, delay, objectToHide) );
	}
	
	public IEnumerator FadeOutCanvasGroupCoroutine( CanvasGroup canvasGroup, float fadingTime, float delay = 0, GameObject objectToHide = null ){

		if( delay > 0 ){
			yield return new WaitForSeconds(delay);
		}
		
		float alpha = canvasGroup.alpha;
		while( alpha > 0 ){
			canvasGroup.alpha = alpha;
			yield return null;
			alpha -= Time.deltaTime / fadingTime;
		}
		
		canvasGroup.alpha = 0;
		if(objectToHide != null) {
			if( objectToHide.GetComponent<Canvas>() != null ){
				objectToHide.GetComponent<Canvas>().enabled = false;
			}else{
				objectToHide.SetActive(false);
			}
		}
		isFadingCanvasGroup = false;
	}
	
	public IEnumerator FadeOutCanvasGroupCoroutine( CanvasGroup canvasGroup, float fadingTime, float delay, GameObject objectToHide, Action<bool> Callback ){

		if( delay > 0 ){
			yield return new WaitForSeconds(delay);
		}
		
		float alpha = canvasGroup.alpha;
		while( alpha > 0 ){
			canvasGroup.alpha = alpha;
			yield return null;
			alpha -= Time.deltaTime / fadingTime;
		}
		
		canvasGroup.alpha = 0;
		if(objectToHide != null) {
			if( objectToHide.GetComponent<Canvas>() != null ){
				objectToHide.GetComponent<Canvas>().enabled = false;
			}else{
				objectToHide.SetActive(false);
			}
		}
		
		Callback(true);

		isFadingCanvasGroup = false;
	}
	
	/************************************************************************************************************************/
	// Helper functions to validate email
	/************************************************************************************************************************/
	
	public bool isValidEmail (string email)
	{
		if (email != null)
			return Regex.IsMatch (email, MatchEmailPattern);
		else
			return false;
	}
	
	/************************************************************************************************************************/
	// Helper functions to reset scrollRect to beginning
	/************************************************************************************************************************/
	
	public void ResetScrollRect( ScrollRect scrollRect ){
		if(scrollRect) scrollRect.horizontalNormalizedPosition = 0;
		if(scrollRect) scrollRect.verticalNormalizedPosition = 1;
	}
	
	public void ResetScrollRects( GameObject obj ){
		ScrollRect[] scrollRects = obj.GetComponentsInChildren<ScrollRect>(true);
		for( int i = 0; i < scrollRects.Length; i++ ){

			scrollRects[i].horizontalNormalizedPosition = 0;
			StartCoroutine(ResetScrollRectsCoroutine(scrollRects[i]));
		}
	}
	
	public IEnumerator ResetScrollRectsCoroutine(ScrollRect scrollRect)
	{
		if (scrollRect.viewport != null && scrollRect.viewport.GetComponent<Mask>() != null)
		{
			scrollRect.viewport.GetComponent<Mask>().enabled = false;
			yield return null;
			scrollRect.viewport.GetComponent<Mask>().enabled = true;
		}
	}

	/************************************************************************************************************************/
	// get all ui elements from raycasthit, 
	// check the depth, if the depth of our element is the highest return true (exclude depth of children)
	/************************************************************************************************************************/

	public bool IsPointerOverGameObject( GameObject obj, bool onFound = false ){
		
		if(obj == null) return false;
		
		#if !UNITY_EDITOR
		
		if( Input.touchCount > 0 ){
		//if ( EventSystem.current != null && EventSystem.current.IsPointerOverGameObject( Input.touches[0].fingerId ) )
		if ( EventSystem.current != null )
		{
		PointerEventData pointer = new PointerEventData(EventSystem.current);
		pointer.position = Input.mousePosition;
	
		List<RaycastResult> raycastResults = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointer, raycastResults);

		int maxDepth = 0;
		int myDepth = 0;
		bool hit = false;
		if(raycastResults.Count > 0)
		{
		foreach(var go in raycastResults)
		{  	
		
		if( onFound && go.gameObject == obj ){
		return true;
		}
		
		if( go.depth > maxDepth && !go.gameObject.transform.IsChildOf(obj.transform) ){
		maxDepth = go.depth;
		}
		if( go.gameObject == obj ) {
		myDepth = go.depth;
		hit = true;
		}
		}
		}
			
		if( hit && maxDepth <= myDepth){
		return true;
		}
		}
		}
		
		
		#else
		
		if ( EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
		{
			PointerEventData pointer = new PointerEventData(EventSystem.current);
			pointer.position = Input.mousePosition;
	
			List<RaycastResult> raycastResults = new List<RaycastResult>();
			EventSystem.current.RaycastAll(pointer, raycastResults);
			
			int maxDepth = 0;
			int myDepth = 0;
			bool hit = false;
			if(raycastResults.Count > 0)
			{
				foreach(var go in raycastResults)
				{  			
					//print(go.gameObject.name);
					
					if( onFound && go.gameObject == obj ){
						return true;
					}
					
					if( go.depth > maxDepth && !go.gameObject.transform.IsChildOf(obj.transform) ){
						maxDepth = go.depth;
					}
					if( go.gameObject == obj ) {
						myDepth = go.depth;
						hit = true;
					}
				}
			}
			
			if( hit && maxDepth <= myDepth){
				return true;
			}
		}
		
		#endif
		
		return false;
	}

	/************************************************************************************************************************/
	// Helper function to load an image from given path into an ui image
	/************************************************************************************************************************/
	
	public void LoadTextureIntoSpriteImage(Image img, string texturePath, bool resize = false)
	{
		if (File.Exists(texturePath))
		{
			Texture2D tex = null;
			byte[] fileData;

			fileData = File.ReadAllBytes(texturePath);
			tex = new Texture2D(2, 2, TextureFormat.RGB24, true);
			tex.wrapMode = TextureWrapMode.Clamp;
			tex.LoadImage(fileData);
		
			// create sprite
			Sprite NewSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 100, 0, SpriteMeshType.Tight);
			img.sprite = NewSprite;
			img.preserveAspect = true;
			
			if(resize){
				// set height of image to fit width of screen and keep its aspect ratio
				float ratio = (float)tex.height / (float)tex.width;
				float targetWidth = CanvasController.instance.GetCanvasWidth();
				float targetHeight = ratio * targetWidth;
				if( img.GetComponent<LayoutElement>() != null ){
					img.GetComponent<LayoutElement>().minHeight = targetHeight;
					img.GetComponent<LayoutElement>().preferredHeight = targetHeight;
				}
			}
		}
		else
		{
			Debug.Log("Texture not found: " + texturePath);
		}
	}
	
	/************************************************************************************************************************/
	// Helper function to load a Texture2D into an ui image
	/************************************************************************************************************************/
	
	public void LoadTextureIntoSpriteImage(Image img, Texture2D tex)
	{
		if ( tex != null )
		{
			// create sprite
			Sprite NewSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 100, 0, SpriteMeshType.Tight);
			img.sprite = NewSprite;
			img.preserveAspect = true;
		}
		else
		{
			Debug.Log("Texture is null");
		}
	}
	
	/************************************************************************************************************************/
	// Helper function to load a Texture2D into an ui image
	/************************************************************************************************************************/
	
	public void LoadSpriteFromAtlasIntoImageFromResources(Image img, string atlasPath, string spriteName)
	{
		Dictionary<string, Sprite> dictSprites = new Dictionary<string, Sprite>();
		Sprite[] sprites = Resources.LoadAll<Sprite>(atlasPath);
		foreach (Sprite sprite in sprites){dictSprites.Add(sprite.name, sprite);}
		img.sprite = dictSprites[spriteName];
	}
	
	/************************************************************************************************************************/
	// Helper function to load a Sprite into an ui image
	/************************************************************************************************************************/
	
	public void LoadTextureIntoSpriteImageFromResources(Image img, string path)
	{
		Texture2D tex = Resources.Load<Texture2D>(path);
		if ( tex != null )
		{
			// create sprite
			Sprite NewSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 100, 0, SpriteMeshType.Tight);
			img.sprite = NewSprite;
			img.preserveAspect = true;
		}
		else
		{
			Debug.Log("Texture is null");
		}
	}
	
	/************************************************************************************************************************/
	// Helper function to add an open weblink action to a button
	/************************************************************************************************************************/
	
	public void ApplyOpenWeblinkButtonFunction( Button button, string weblink ){
		button.onClick.AddListener(() => OpenWebView(weblink));
	}
	
	public void OpenWebView( string url ){	
		OpenWebView(url, true);
	}
	
	public void OpenWebView( string url, bool useWebview = true ){	

		print("OpenWebView " + url);
		
		if( LanguageController.TranslationExists(url) ){
			url = LanguageController.GetTranslation(url);
		}
		
		
		#if UNITY_EDITOR
		
		Application.OpenURL(url);
		
		#else
		
		if( url.EndsWith(".pdf") ){
		
			Application.OpenURL(url);
			
			/*
			if( useWebview ){
					
				InAppBrowser.DisplayOptions options = new InAppBrowser.DisplayOptions();
				options.pinchAndZoomEnabled = true;
				options.backButtonText = LanguageController.GetTranslation("zurück").ToUpper();
				InAppBrowser.OpenURL(url, options);
						
						
			}else{
				Application.OpenURL(url);
			}
			*/
		
		}else{
			
			if( useWebview ){
					
				InAppBrowser.DisplayOptions options = new InAppBrowser.DisplayOptions();
				options.pinchAndZoomEnabled = true;
				options.backButtonText = LanguageController.GetTranslation("zurück").ToUpper();
				InAppBrowser.OpenURL(url, options);
					
					
			}else{
				Application.OpenURL(url);
			}
		}
		#endif
	}
	
	/*
	public void OpenWebView( string url ){	

	if( LanguageController.TranslationExists(url) ){
	url = LanguageController.GetTranslation(url);
	}
				
		#if UNITY_EDITOR
	Application.OpenURL(url);
		#else
	if( url.EndsWith(".pdf") ){
	Application.OpenURL(url);
	}else{
			
	InAppBrowser.DisplayOptions options = new InAppBrowser.DisplayOptions();
	options.pinchAndZoomEnabled = true;
	options.backButtonText = LanguageController.GetTranslation("zurück").ToUpper();
	InAppBrowser.OpenURL(url, options);

	}
		#endif
	}
	*/
	
	/************************************************************************************************************************/
	// Helper function to get a valid string filename which can be saved to local filesystem
	/************************************************************************************************************************/
	
	public string GetValidFilenameFromURL( string url ){
		Regex illegalInFileName = new Regex(@"[\\/:*?""<>|]");
		string filename = Path.GetFileName( url );
		return illegalInFileName.Replace(filename, "");
	}
	
	public string GetSavePathFromURL( string url ){
		
		string savePath = Application.persistentDataPath + "/" + GetValidFilenameFromURL(url);
		if( useUniqueSavePathNameFromURL ){
			
			// URL to unique md5 hash
			savePath = Application.persistentDataPath + "/" + CalculateMD5Hash(url) + "_" + GetValidFilenameFromURL(url);

			// Remove invalid characters
			//foreach (char c in System.IO.Path.GetInvalidFileNameChars()){ url = url.Replace(c, '-'); }
			//savePath = Application.persistentDataPath + "/" + url;

			// Remove dots
			//string extension = Path.GetExtension(url);
			//string urlWithouExtension = Path.ChangeExtension(url, null);
			//urlWithouExtension = urlWithouExtension.Replace(".", "_");
			//savePath = Application.persistentDataPath + "/" + urlWithouExtension + extension;
		}		
		return savePath;
	}

	public string GetSavePathFromURL(string url, int maxSize)
	{
		string savePath = Application.persistentDataPath + "/" + GetValidFilenameFromURL(url);
		if (useUniqueSavePathNameFromURL)
		{
			if (maxSize <= 0) { savePath = Application.persistentDataPath + "/" + CalculateMD5Hash(url) + "_" + GetValidFilenameFromURL(url); }
			else { savePath = Application.persistentDataPath + "/" + CalculateMD5Hash(url) + "_" + maxSize.ToString() + "_" + GetValidFilenameFromURL(url); }

			// Remove invalid characters
			//foreach (char c in System.IO.Path.GetInvalidFileNameChars()){ url = url.Replace(c, '-'); }
			//savePath = Application.persistentDataPath + "/" + url;

			// Remove dots
			//string extension = Path.GetExtension(url);
			//string urlWithouExtension = Path.ChangeExtension(url, null);
			//urlWithouExtension = urlWithouExtension.Replace(".", "_");
			//savePath = Application.persistentDataPath + "/" + urlWithouExtension + extension;
		}
		return savePath;
	}

	public string GetTmpSavePathFromURL( string url ){
		
		string savePath = Application.persistentDataPath + "/tmp_" + GetValidFilenameFromURL(url);
		if( useUniqueSavePathNameFromURL ){
			
			savePath = Application.persistentDataPath + "/tmp_" + CalculateMD5Hash(url) + "_" + GetValidFilenameFromURL(url);

			//foreach (char c in System.IO.Path.GetInvalidFileNameChars()){ url = url.Replace(c, '-'); }
			//savePath = Application.persistentDataPath + "/tmp_" + url;

			//string extension = Path.GetExtension(url);
			//string urlWithouExtension = Path.ChangeExtension(url, null);
			//urlWithouExtension = urlWithouExtension.Replace(".", "_");
			//savePath = Application.persistentDataPath + "/tmp_" + urlWithouExtension + extension;
		}		
		return savePath;
	}
	
	/************************************************************************************************************************/
	// Helper function to remove html-tags from a string
	/************************************************************************************************************************/
	
	public string StripHTML(string htmlString)
	{
		string pattern = @"<(.|\n)*?>";
		return Regex.Replace(htmlString, pattern, string.Empty);
	}
	
	/************************************************************************************************************************/
	// Helper function to convert unix timestamp string to DateTime
	/************************************************************************************************************************/
	
	public DateTime GetDateTimeFromTimestampString( string val ){
		long result = 0;
		bool success = long.TryParse( val, out result );
				
		if( success ){
			DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			DateTime date= start.AddSeconds(result).ToLocalTime();
			return date;
		}
		return DateTime.Now;
	} 
	
	/************************************************************************************************************************/
	// Helper function to get Timestamp
	/************************************************************************************************************************/
	
	public long GetTimestamp(){
		return (long)(System.DateTime.Now.ToUniversalTime() - new System.DateTime (1970, 1, 1)).TotalSeconds;
	}

	public long GetTimestampMilliSeconds()
	{
		return (long)(System.DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds;
	}

	/************************************************************************************************************************/
	// Helper function to get Timestamp
	/************************************************************************************************************************/

	public long GetTimestampFromDateTime( DateTime date ){
		return (long)(date.ToUniversalTime() - new System.DateTime (1970, 1, 1)).TotalSeconds;
	} 
	
	/************************************************************************************************************************/
	// Helper functions to get date
	/************************************************************************************************************************/
	
	public string GetCurrentDate(){
		
		int day = DateTime.Now.Day;
		string month = DateTime.Now.ToString("MMMM", new CultureInfo("de-DE"));
		int year = DateTime.Now.Year;
		
		return day + ". " + LanguageController.GetTranslation(month) + " " + year;
	}
	
	/************************************************************************************************************************/
	// Helper functions to get color from color string
	/************************************************************************************************************************/
	
	public Color GetColorFromHexString( string hexCode ){
		
		if( hexCode.Length == 6 ){	// avoid transparent color
			hexCode += "FF";
		}
		
		if( !hexCode.StartsWith("#") ){
			hexCode = "#" + hexCode;
		}
		
		Color color;
		if (ColorUtility.TryParseHtmlString(hexCode, out color))
			return color;

		return Color.white;
	}

	/************************************************************************************************************************/
	// Helper function to get canvas position from mouse position
	/************************************************************************************************************************/

	public Vector2 GetCanvasPositionFromMousePosition( RectTransform rectTransform ){
		
		int borderBottomHeight = CanvasController.instance.GetBorderBottomPixelHeight();
		Vector2 screenPixelPosition = new Vector2( Input.mousePosition.x, Input.mousePosition.y - borderBottomHeight );
		
		Vector2 canvasPosition = Vector2.zero;
		canvasPosition.x = screenPixelPosition.x / Screen.width * rectTransform.rect.width;
		canvasPosition.y = screenPixelPosition.y / ( Screen.height - CanvasController.instance.GetSafeAreaHeight() ) * rectTransform.rect.height;
		
		return canvasPosition;
	}
	
	/************************************************************************************************************************/
	// Helper function to get pixel screen position from current mouse position. We also need to adjust the position if we have safe area borders
	/************************************************************************************************************************/

	public Vector2 GetPixelScreenPositionFromMousePosition(){
		int borderBottomHeight = CanvasController.instance.GetBorderBottomPixelHeight();
		return new Vector2( Input.mousePosition.x, Input.mousePosition.y - borderBottomHeight );
	}
	
	/************************************************************************************************************************/
	// Helper function check if a scene is loaded
	/************************************************************************************************************************/

	private bool SceneIsLoaded( string sceneID ){
		
		for (int i = 0; i < SceneManager.sceneCount; i++ )
		{
			Scene scene = SceneManager.GetSceneAt(i);
			if( scene.name == sceneID ){
				if( scene.isLoaded ){
					return true;
				}else{
					return false;
				}
			}
		}
		return false;
	}
	
	/************************************************************************************************************************/
	// Helper function to get lerp percentage value with a curve factor
	/************************************************************************************************************************/
	
	public float GetLerpFactor( float moveTime, float currentPercentage, float timeCountdown, float speedCurveFaktor )
	{
		// Usage
		/*
		float moveTime = 0.3f;
		float currentPercentage = 0;
		float timeCountdown = moveTime;
		float speedCurveFactor = 2;
		
		while( timeCountdown > 0 ){
		currentPercentage = GetLerpFactor( moveTime, currentPercentage, timeCountdown, speedCurveFactor );
		timeCountdown -= Time.deltaTime;
		}
		*/

		if (timeCountdown > 0){
			float faktor = Mathf.Pow(timeCountdown / moveTime, speedCurveFaktor);
			return 1-faktor;
		}
		else{
			return currentPercentage;
		}
	}
	
	/************************************************************************************************************************/
	// Helper function to check if a variable is a number
	/************************************************************************************************************************/
	
	public bool IsNumeric (System.Object Expression)
	{
		if(Expression == null || Expression is DateTime)
			return false;

		if(Expression is Int16 || Expression is Int32 || Expression is Int64 || Expression is Decimal || Expression is Single || Expression is Double || Expression is Boolean)
			return true;
   
		try 
		{
			if(Expression is string)
				Double.Parse(Expression as string);
			else
				Double.Parse(Expression.ToString());
			return true;
		} catch {} // just dismiss errors but return false
		return false;
	}

	/************************************************************************************************************************/
	// Helper function to check if an ui element is visible
	/************************************************************************************************************************/
	
	public bool IsVisible( Transform myTransform ){
		
		Transform myParent = myTransform.parent;
		while( myParent != null ){
			
			Canvas canvas = myParent.GetComponentInParent<Canvas>();
			if( canvas != null ){
				if( !canvas.enabled ){
					return false;
				}else{
					myParent = canvas.transform.parent;
				}
			}
			else{
				break;
			}	
		}	     	
                
		return true;  
	}
	
	/************************************************************************************************************************/
	// Native helper functions
	/************************************************************************************************************************/
	
	#if UNITY_IOS
	[System.Runtime.InteropServices.DllImport("__Internal")]
	private static extern string GetCountryCode ();
	#endif
	
	
	/************************************************************************************************************************/
	// Helper functions to change material
	/************************************************************************************************************************/
	
	// Needs StandardShaderUtils script
	public void ChangeRenderMode(GameObject obj, StandardShaderUtils.BlendMode mode)
	{
		Renderer[] rend = obj.GetComponentsInChildren<Renderer>();

		for (int j = 0; j < rend.Length; j++)
		{
			Material[] mat = rend[j].materials;


			if (rend[j].gameObject.name == "Glass")
			{
				continue;
			}
			else if (rend[j].gameObject.name == "Reflection")
			{
				continue;
			}
			else if (rend[j].gameObject.name == "DepthMask")
			{
				continue;
			}


			for (int k = 0; k < mat.Length; k++)
			{
				StandardShaderUtils.ChangeRenderMode(mat[k], mode);
			}

		}
	}
	
	public void FadeOut(GameObject obj, bool isStandardShader = false, float fadeTime = 1.0f)
	{
		StartCoroutine(FadeOutCoroutine(obj, isStandardShader, fadeTime));
	}

	public void FadeIn(GameObject obj, bool isStandardShader = false, float fadeTime = 1.0f)
	{
		StartCoroutine(FadeInCoroutine(obj, isStandardShader, fadeTime));
	}

	public IEnumerator FadeOutCoroutine(GameObject obj, bool isStandardShader = false, float fadeTime = 1.0f)
	{
		if(isStandardShader) ChangeRenderMode(obj, StandardShaderUtils.BlendMode.Ghost);
		Renderer[] rend = obj.GetComponentsInChildren<Renderer>();

		float alpha = 1;
		while (alpha > 0)
		{
			SetAlpha(rend, alpha);
			alpha -= Time.deltaTime / fadeTime;
			yield return null;
		}
		SetAlpha(rend, 0);
	}

	public IEnumerator FadeInCoroutine(GameObject obj, bool isStandardShader = false, float fadeTime = 1.0f)
	{
		if(isStandardShader) ChangeRenderMode(obj, StandardShaderUtils.BlendMode.Ghost);
		Renderer[] rend = obj.GetComponentsInChildren<Renderer>();

		float alpha = 0;
		while (alpha < 1)
		{
			SetAlpha(rend, alpha);
			alpha += Time.deltaTime / fadeTime;
			yield return null;
		}
		SetAlpha(rend, 1);
		if(isStandardShader) ChangeRenderMode(obj, StandardShaderUtils.BlendMode.Opaque);
	}

	public void SetAlpha(GameObject obj, float alpha){
		Renderer[] rend = obj.GetComponentsInChildren<Renderer>();
		SetAlpha(rend, alpha);
	}
	
	public void SetAlpha(Renderer[] rend, float alpha)
	{
		for (int i = 0; i < rend.Length; i++)
		{
			Material[] mat = rend[i].materials;
			for (int j = 0; j < mat.Length; j++)
			{
				if ( mat[j].HasProperty("_Color") )
				{
					if (rend[i].gameObject.name == "Glass")
					{
						mat[j].color = new Color(mat[j].color.r, mat[j].color.g, mat[j].color.b, Mathf.Clamp(alpha, 0, 0.02f));
					}
					else if(rend[i].gameObject.name == "Reflection")
					{
						mat[j].color = new Color(mat[j].color.r, mat[j].color.g, mat[j].color.b, Mathf.Clamp(alpha, 0, 0.15f));
					}
					else{
						mat[j].color = new Color(mat[j].color.r, mat[j].color.g, mat[j].color.b, alpha);
					}

				}
			}
			rend[i].materials = mat;
		}
	}

	public void SetMaterial(GameObject obj, Material mat)
	{
		if (obj == null || mat == null) return;

		List<Renderer> renderers = new List<Renderer>();
		if (obj.GetComponent<Renderer>() != null)
		{
			renderers.Add(obj.GetComponent<Renderer>());
		}

		Renderer[] rend = obj.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < rend.Length; i++)
		{
			renderers.Add(rend[i]);
		}

		for (int i = 0; i < renderers.Count; i++)
		{
			renderers[i].material = mat;
		}
	}
	
	/************************************************************************************************************************/
	// Helper functions to get mobile platform
	/************************************************************************************************************************/
	
	public string GetPlatform(){
		#if UNITY_IOS 
		return "iOS";
		#endif	
		return "Android";
	}
	
	/************************************************************************************************************************/
	// Helper functions to get gameobject bounds
	/************************************************************************************************************************/
	
	public Vector3 getBoundsSize(GameObject obj)
	{
		Vector3 maxPos = new Vector3(-10000, -10000, -10000);
		Vector3 minPos = new Vector3(10000, 10000, 10000);
		FindBounds(obj.transform, ref minPos, ref maxPos);

		return new Vector3(Mathf.Abs(maxPos.x - minPos.x), Mathf.Abs(maxPos.y - minPos.y), Mathf.Abs(maxPos.z - minPos.z));
	}

	public void FindBounds(Transform child, ref Vector3 minPos, ref Vector3 maxPos)
	{

		if (child == null)
			return;

		if (child.GetComponent<Renderer>() != null)
		{
			if (maxPos.x < child.GetComponent<Renderer>().bounds.max.x)
			{
				maxPos.x = child.GetComponent<Renderer>().bounds.max.x;
			}
			if (maxPos.y < child.GetComponent<Renderer>().bounds.max.y)
			{
				maxPos.y = child.GetComponent<Renderer>().bounds.max.y;
			}
			if (maxPos.z < child.GetComponent<Renderer>().bounds.max.z)
			{
				maxPos.z = child.GetComponent<Renderer>().bounds.max.z;
			}
			if (minPos.x > child.GetComponent<Renderer>().bounds.min.x)
			{
				minPos.x = child.GetComponent<Renderer>().bounds.min.x;
			}
			if (minPos.y > child.GetComponent<Renderer>().bounds.min.y)
			{
				minPos.y = child.GetComponent<Renderer>().bounds.min.y;
			}
			if (minPos.z > child.GetComponent<Renderer>().bounds.min.z)
			{
				minPos.z = child.GetComponent<Renderer>().bounds.min.z;
			}
		}

		//Ermitteln der Bounds
		foreach (Transform t in child)
		{
			if (t.GetComponent<Renderer>() != null)
			{
				if (maxPos.x < t.GetComponent<Renderer>().bounds.max.x)
				{
					maxPos.x = t.GetComponent<Renderer>().bounds.max.x;
				}
				if (maxPos.y < t.GetComponent<Renderer>().bounds.max.y)
				{
					maxPos.y = t.GetComponent<Renderer>().bounds.max.y;
				}
				if (maxPos.z < t.GetComponent<Renderer>().bounds.max.z)
				{
					maxPos.z = t.GetComponent<Renderer>().bounds.max.z;
				}
				if (minPos.x > t.GetComponent<Renderer>().bounds.min.x)
				{
					minPos.x = t.GetComponent<Renderer>().bounds.min.x;
				}
				if (minPos.y > t.GetComponent<Renderer>().bounds.min.y)
				{
					minPos.y = t.GetComponent<Renderer>().bounds.min.y;
				}
				if (minPos.z > t.GetComponent<Renderer>().bounds.min.z)
				{
					minPos.z = t.GetComponent<Renderer>().bounds.min.z;
				}
			}
			FindBounds(t, ref minPos, ref maxPos);
		}
	}
	
	public void GenerateBoxCollider(GameObject obj)
	{

		if (obj.GetComponent<BoxCollider>() == null)
		{
			Vector3 startPosition = obj.transform.position;
			Vector3 startRotation = obj.transform.eulerAngles;
			obj.transform.position = Vector3.zero;
			obj.transform.eulerAngles = Vector3.zero;

			//Find bounds
			Vector3 maxPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			Vector3 minPos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			SetBoundingValues(obj, ref minPos, ref maxPos);

			float distX = Mathf.Abs(maxPos.x - minPos.x);
			float distY = Mathf.Abs(maxPos.y - minPos.y);
			float distZ = Mathf.Abs(maxPos.z - minPos.z);

			GameObject o = GameObject.CreatePrimitive(PrimitiveType.Cube);
			o.transform.SetParent(obj.transform);
			o.transform.position = new Vector3(minPos.x + distX * 0.5f, minPos.y + distY * 0.5f, minPos.z + distZ * 0.5f);
			o.transform.localEulerAngles = Vector3.zero;

			o.transform.SetParent(null);
			o.transform.localScale = new Vector3(distX * 1, distY, distZ * 1);
			o.transform.SetParent(obj.transform);

			o.GetComponent<Renderer>().enabled = false;
			//o.tag = "Model";
			//o.layer = LayerMask.NameToLayer("Model");

			obj.transform.position = startPosition;
			obj.transform.eulerAngles = startRotation;
			obj.AddComponent<BoxCollider>();
			obj.GetComponent<BoxCollider>().center = o.transform.localPosition;
			obj.GetComponent<BoxCollider>().size = o.transform.localScale;
			
			Destroy(o);
		}
	}
	
	// Depend on the children renderers we get the bounds
	public void SetBoundingValues(GameObject obj, ref Vector3 minPos, ref Vector3 maxPos, string excludeGameObjectName = "")
	{

		Transform[] allChildren = obj.GetComponentsInChildren<Transform>();
		for (int i = 0; i < allChildren.Length; i++)
		{
			if (allChildren[i].GetComponent<Renderer>() != null && (!allChildren[i].name.Contains(excludeGameObjectName) || excludeGameObjectName == ""))
			{
				if (maxPos.x < allChildren[i].GetComponent<Renderer>().bounds.max.x)
				{
					maxPos.x = allChildren[i].GetComponent<Renderer>().bounds.max.x;
				}
				if (maxPos.y < allChildren[i].GetComponent<Renderer>().bounds.max.y)
				{
					maxPos.y = allChildren[i].GetComponent<Renderer>().bounds.max.y;
				}
				if (maxPos.z < allChildren[i].GetComponent<Renderer>().bounds.max.z)
				{
					maxPos.z = allChildren[i].GetComponent<Renderer>().bounds.max.z;
				}
				if (minPos.x > allChildren[i].GetComponent<Renderer>().bounds.min.x)
				{
					minPos.x = allChildren[i].GetComponent<Renderer>().bounds.min.x;
				}
				if (minPos.y > allChildren[i].GetComponent<Renderer>().bounds.min.y)
				{
					minPos.y = allChildren[i].GetComponent<Renderer>().bounds.min.y;
				}
				if (minPos.z > allChildren[i].GetComponent<Renderer>().bounds.min.z)
				{
					minPos.z = allChildren[i].GetComponent<Renderer>().bounds.min.z;
				}
			}
		}
	}
	
	// Depend on the bounds of all renderers we add a boxcollider
	/*
	public void GenerateBoundigBoxCollider(GameObject obj, string excludeGameObjectName)
	{

	// Generate BoxCollider for all renderer
	Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
	foreach (var r in renderers)
	{
	GenerateBoxCollider(r.gameObject);
	}
	}
	*/
	
	public Vector3 GetCenter(GameObject obj)
	{
		//Find bounds
		Vector3 maxPos = new Vector3(float.MinValue, float.MinValue, float.MinValue);
		Vector3 minPos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		SetBoundingValues(obj, ref minPos, ref maxPos);

		float distX = Mathf.Abs(maxPos.x - minPos.x);
		float distY = Mathf.Abs(maxPos.y - minPos.y);
		float distZ = Mathf.Abs(maxPos.z - minPos.z);

		Vector3 pos = new Vector3(minPos.x + distX * 0.5f, minPos.y + distY * 0.5f, minPos.z + distZ * 0.5f);
		
		if( pos.x < 100000 && pos.y < 100000 && pos.z < 100000 ) return pos;
		
		print("bad pos");
		return obj.transform.position;
	}
	
	public GameObject InstantiateObject(GameObject prefab, Transform holder)
	{
		GameObject obj = Instantiate(prefab);
		obj.SetActive(true);
		obj.transform.SetParent( holder );
		obj.transform.localScale = Vector3.one;
		obj.transform.localPosition = Vector3.zero;
		obj.transform.localEulerAngles = Vector3.zero;
		return obj;
	}
	
	public GameObject InstantiateObject(string path, Transform holder)
	{
		var prefab = Resources.Load(path, typeof(GameObject)) as GameObject;
		if( prefab != null ){
			
			GameObject obj = Instantiate(prefab);
			obj.name = Path.GetFileName(path);
			obj.SetActive(true);
			obj.transform.SetParent( holder );
			obj.transform.localScale = Vector3.one;
			obj.transform.localPosition = Vector3.zero;
			obj.transform.localEulerAngles = Vector3.zero;
			return obj;
		}
		return null;
	}
	
	public IEnumerator InstantiateObjectCoroutine(string path, Transform holder, Action<GameObject> Callback)
	{
		//Request data to be loaded
		ResourceRequest loadAsync = Resources.LoadAsync(path, typeof(GameObject));

		//Wait till we are done loading
		while (!loadAsync.isDone)
		{
			yield return null;
		}

		//Get the loaded data
		GameObject prefab = loadAsync.asset as GameObject;
		
		if( prefab != null ){
			
			GameObject obj = Instantiate(prefab);
			obj.name = Path.GetFileName(path);
			obj.SetActive(true);
			obj.transform.SetParent( holder );
			obj.transform.localScale = Vector3.one;
			obj.transform.localPosition = Vector3.zero;
			obj.transform.localEulerAngles = Vector3.zero;
			Callback(obj);
		}
		else{
			Callback(null);
		}
	}
	
	public GameObject FindGameObjectByName(GameObject obj, string objName)
	{
		Transform[] allChildren = obj.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < allChildren.Length; i++)
		{
			if( allChildren[i].name == objName ) return allChildren[i].gameObject;		
		}
		return null;
	}

	public GameObject FindGameObjectContaingName(GameObject obj, string objName)
	{
		Transform[] allChildren = obj.GetComponentsInChildren<Transform>( true );
		for ( int i = 0; i < allChildren.Length; i++ )
		{
			if ( objName.Contains( allChildren[i].name ) ) return allChildren[i].gameObject;
		}
		return null;
	}

	public GameObject FindParentByName(GameObject obj, string objName)
	{
		if( obj.transform.parent != null ){
			if( obj.transform.parent.name == objName ){ return obj.transform.parent.gameObject; }
			else{ return FindParentByName(obj.transform.parent.gameObject, objName); }
		}
		return null;
	}
	
	public float GetFloatValueFromJsonNode( JSONNode node ){
		
		string valueString = node.Value.Replace(',', '.');
		
		float val = 0;
		//bool parsed = float.TryParse( valueString, out val );
		bool parsed = float.TryParse( valueString, 	NumberStyles.Float, CultureInfo.InvariantCulture, out val );
	
		if( parsed ){
			return val;
		}
		
		return node.AsFloat;
	}

	public float GetFloatFromString(string floatString)
	{
		string valueString = floatString.Replace( ',', '.' );

		float val = 0;
		//bool parsed = float.TryParse( valueString, out val );
		bool parsed = float.TryParse( valueString, NumberStyles.Float, CultureInfo.InvariantCulture, out val );

		if ( parsed ) { return val; }
		return 0;
	}

	public double GetDoubleValueFromJsonNode( JSONNode node ){
		
		string valueString = node.Value.Replace(',', '.');
		
		double val = 0;
		//bool parsed = float.TryParse( valueString, out val );
		bool parsed = double.TryParse( valueString, NumberStyles.Float, CultureInfo.InvariantCulture, out val );
	
		if( parsed ){
			return val;
		}
		
		return node.AsDouble;
	}
	
	public float AngleFromCoordinate(float lat1, float long1, float lat2, float long2) {
		
		lat1 *= Mathf.Deg2Rad;
		lat2 *= Mathf.Deg2Rad;
		long1 *= Mathf.Deg2Rad;
		long2 *= Mathf.Deg2Rad;
 
		float dLon = (long2 - long1);
		float y = Mathf.Sin(dLon) * Mathf.Cos(lat2); 
		float x = (Mathf.Cos(lat1) * Mathf.Sin(lat2)) - (Mathf.Sin(lat1) * Mathf.Cos(lat2) * Mathf.Cos(dLon));
		float brng = Mathf.Atan2(y, x); 
		brng = Mathf.Rad2Deg* brng; 
		brng = (brng + 360) % 360; 
		//brng = 360 - brng;
		return brng;
	}
	
	public void ChangeLayer( GameObject myObject, string layerName ){
		
		Transform[] children = myObject.GetComponentsInChildren<Transform>(true);
		foreach(Transform child in children){
			child.gameObject.layer = LayerMask.NameToLayer(layerName);
		}
				
		RectTransform[] childrenRect = myObject.GetComponentsInChildren<RectTransform>(true);
		foreach(RectTransform child in childrenRect){
			child.gameObject.layer = LayerMask.NameToLayer(layerName);
		}
	}

	public void SetLayer(GameObject obj, string layerName)
	{
		Transform[] gameObjects = obj.GetComponentsInChildren<Transform>( true );
		for ( int i = 0; i < gameObjects.Length; i++ ) { gameObjects[i].gameObject.layer = LayerMask.NameToLayer( layerName ); }
	}

	public bool HasLayer(LayerMask layerMask, string layerName)
	{
		if ( layerMask == (layerMask | (1 << LayerMask.NameToLayer( layerName ))) ) { return true; }
		return false;
	}

	public List<string> GetLayerNamesInLayerMask(LayerMask layerMask)
	{
		List<string> layerNames = new List<string>();
		for ( int i = 0; i <= 31; i++ ) { if ( HasLayer( layerMask, LayerMask.LayerToName( i ) ) ) { layerNames.Add( LayerMask.LayerToName( i ) ); } }
		return layerNames;
	}

	public void AddLayerToLayerMask(ref LayerMask layerMask, string layerName)
	{
		layerMask |= (1 << LayerMask.NameToLayer( layerName ));
	}

	public void RemoveLayerFromLayerMask(ref LayerMask layerMask, string layerName)
	{
		layerMask &= ~(1 << LayerMask.NameToLayer( layerName ));
	}

	public LayerMask GetLayerMaskWithNames(params string[] layerNames)
	{
		LayerMask layerMask = 0;
		for ( int i = 0; i < layerNames.Length; i++ ) { AddLayerToLayerMask( ref layerMask, layerNames[i] ); }
		return layerMask;

		// Or just use: LayerMask layerMask = LayerMask.GetMask("LayerA", "LayerB");
	}

	public void SetCullingMask(Camera myCamera, params string[] layerNames)
	{
		LayerMask layerMask = GetLayerMaskWithNames( layerNames );
		myCamera.cullingMask = layerMask;
	}

	public Vector3 FindCenterWithinPoints(List<Vector3> positions)
	{
		if( positions.Count <= 0 ) return Vector3.zero;
		var bound = new Bounds(positions[0], Vector3.zero);
		for(int i = 1; i < positions.Count; i++)
		{
			bound.Encapsulate(positions[i]);
		}
		return bound.center;
	}
	
	public float GetArea( List<Vector2> points ){
		
		return Math.Abs(points.Take(points.Count - 1)
			.Select((p, i) => (points[i + 1].x - p.x) * (points[i + 1].y + p.y))
			.Sum() / 2);
	}
	
	public float CalculateDistance(float lat_1, float lat_2, float long_1, float long_2)
	{
		int R = 6371;
		var lat_rad_1 = Mathf.Deg2Rad * lat_1;
		var lat_rad_2 = Mathf.Deg2Rad * lat_2;
		var d_lat_rad = Mathf.Deg2Rad * (lat_2 - lat_1);
		var d_long_rad = Mathf.Deg2Rad * (long_2 - long_1);
		var a = Mathf.Pow(Mathf.Sin(d_lat_rad / 2), 2) + (Mathf.Pow(Mathf.Sin(d_long_rad / 2), 2) * Mathf.Cos(lat_rad_1) * Mathf.Cos(lat_rad_2));
		var c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
		var total_dist = R * c * 1000; // convert to meters
		return total_dist;
	}
	
	public bool IsPointerOverUIObject() {

		try{
			
			if (EventSystem.current == null) return false;
			PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
			eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			List<RaycastResult> results = new List<RaycastResult>();
			EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
		
			bool hitUI = results.Count > 0;
			if( hitUI ){
				return true;
			}
		
			#if UNITY_EDITOR
			if (EventSystem.current.IsPointerOverGameObject()){ return true; }
			#else
			if (Input.touchCount > 0)
			{
			var touch = Input.GetTouch(0);
			if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return true;
			}
			#endif
		
		}catch(Exception e){
			
			print("IsPointerOverUIObject error " + e.Message);
		}
		
		return false;
	}
	
	public bool IsValidURL(string url)
	{
		Uri uriResult;
		bool isURL = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
			&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
		return isURL;
	}

	public void ApplyOnlineImage( Image image, string url, bool shouldEnvelopeParent = false, string imageType = "" ){
		
		if( url == "null" ) return;
		
		if( image.gameObject.GetComponent<UIImage>() != null ){
			if( image.gameObject.GetComponent<UIImage>().url != url ){
				DestroyImmediate( image.gameObject.GetComponent<UIImage>() );
				image.gameObject.AddComponent<UIImage>();
			}
		}
		else{
			image.gameObject.AddComponent<UIImage>();
		}

		image.gameObject.GetComponent<UIImage>().url = url;
		image.gameObject.GetComponent<UIImage>().shouldEnvelopeParent = shouldEnvelopeParent;
		image.gameObject.GetComponent<UIImage>().imageType = imageType;
	}


	public Vector3 GetInfrontMarkerPosition( Transform marker, float forwardDistance, float upDistance ){
		
		Vector3 forwardVector = marker.forward;
		forwardVector.y = 0;
		Vector3 pos = marker.position + forwardVector.normalized*forwardDistance + Vector3.up*upDistance;
		return pos;
	}
		
	public Vector2 GetTouchPosition()
	{
#if UNITY_EDITOR
		if (Input.GetMouseButton(0))
		{
			var mousePosition = Input.mousePosition;
			return new Vector2(mousePosition.x, mousePosition.y);
		}
#else
		if (Input.touchCount > 0)
		{
		return Input.GetTouch(0).position;
		}else{
		if (Input.GetMouseButton(0))
		{
		var mousePosition = Input.mousePosition;
		return new Vector2(mousePosition.x, mousePosition.y);
		}
		}
#endif

		return new Vector2(Screen.width*0.5f, Screen.height*0.5f);
	}
	
	public void DisableInternetConnection(){
		
		#if UNITY_ANDROID
		SetWifiEnabled(false);
		#endif
	}
	
	public void EnableInternetConnection(){
		
		#if UNITY_ANDROID
		SetWifiEnabled(true);
		#endif
	}
	
	public bool SetWifiEnabled(bool enabled)
	{
		try{
			
			using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
			{
				try
				{
					using (var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
					{
						bool success = wifiManager.Call<bool>("setWifiEnabled", enabled);
						print("SetWifiEnabled success " + success);
						return success;
					}
				}
					catch (Exception e)
					{
						print("SetWifiEnabled1 error " + e.Message);
					}
			}
			
		}catch( Exception e ){
			print("SetWifiEnabled2 error " + e.Message);
		}
		
		return false;
	}

	public bool IsWifiEnabled()
	{
		try{
			
			using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
			{
				try
				{
					using (var wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
					{
						return wifiManager.Call<bool>("isWifiEnabled");
					}
				}
					catch (Exception e)
					{
						print("IsWifiEnabled1 error " + e.Message);
					}
			}
			
		}catch( Exception e ){
			print("IsWifiEnabled2 error " + e.Message);
		}
		
		return false;
	}
	
	public string ReadFromJwtToken( string jwtoken ){
		
		try{
			
			var parts = jwtoken.Split('.');
			if (parts.Length != 3)
			{
				print("Token must consist from 3 delimited by dot parts");
			}
			else{
				
				var payload = parts[1];
				var payloadJsonString = Encoding.UTF8.GetString(Base64UrlDecode(payload));
				JSONNode payloadJson = JSONNode.Parse(payloadJsonString);
				
				print(payloadJsonString);
				
				if( payloadJson["token"] != null ){
					return payloadJson["token"].Value;
				}
			}
			
		}
			catch( UnityException e ){
			
				print("ReadFromJwtToken error: " + e.Message);
			}

		
		return "";
	}
	
	// from JWT spec
	public static byte[] Base64UrlDecode(string input)
	{
		var output = input;
		output = output.Replace('-', '+'); // 62nd char of encoding
		output = output.Replace('_', '/'); // 63rd char of encoding
		switch (output.Length % 4) // Pad with trailing '='s
		{
		case 0: break; // No pad chars in this case
		case 2: output += "=="; break; // Two pad chars
		case 3: output += "="; break;  // One pad char
		default: throw new Exception("Illegal base64url string!");
		}
		var converted = Convert.FromBase64String(output); // Standard base64 decoder
		return converted;
	}
	
	public void ActivateParent( GameObject obj, string parentName, bool activate = true ){
		
		if( obj.transform.parent != null ){
			if( obj.transform.parent.name == parentName ){ 
				obj.transform.parent.gameObject.SetActive(activate); 
			}else{
				ActivateParent( obj.transform.parent.gameObject, parentName, activate );
			}
		}
	}
	
	public float GetAnimationTime( Animator animator ){
		
		AnimatorStateInfo animationState = animator.GetCurrentAnimatorStateInfo(0);
		AnimatorClipInfo[] myAnimatorClip = animator.GetCurrentAnimatorClipInfo(0);
		return myAnimatorClip[0].clip.length * animationState.normalizedTime;
	}
	
	public float GetAnimationLength( Animator animator ){
		
		AnimatorClipInfo[] myAnimatorClip = animator.GetCurrentAnimatorClipInfo(0);
		return myAnimatorClip[0].clip.length;
	}
	
	public Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
	{
		float u = 1 - t;
		float tt = t*t;
		float uu = u*u;
		float uuu = uu * u;
		float ttt = tt * t;
		
		Vector3 p = uuu * p0; //first term
		p += 3 * uu * t * p1; //second term
		p += 3 * u * tt * p2; //third term
		p += ttt * p3; //fourth term
		
		return p;
	}

	public float ClampBetween_0_360(float eulerAngles)
	{
		float result = eulerAngles - Mathf.CeilToInt(eulerAngles / 360f) * 360f;
		if (result < 0)
		{
			result += 360f;
		}
		return result;
	}
    
	public string CalculateMD5Hash(string input)
	{
		MD5 md5 = System.Security.Cryptography.MD5.Create();
		byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
		byte[] hash = md5.ComputeHash(inputBytes);
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < hash.Length; i++)
		{
			sb.Append(hash[i].ToString("x2"));
		}
		return sb.ToString();
	}

	public Vector2 GetMainGameViewSize()
	{
#if UNITY_EDITOR
		System.Type T = System.Type.GetType("UnityEditor.GameView,UnityEditor");
		System.Reflection.MethodInfo GetSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
		System.Object Res = GetSizeOfMainGameView.Invoke(null, null);
		return (Vector2)Res;
#endif
		return new Vector2(Screen.width, Screen.height);
	}

	public IEnumerator CleanMemoryCoroutine()
	{
		print("Cleaning memory ...");
		yield return null;
        	    
		float timer = 2.0f;
		AsyncOperation async = Resources.UnloadUnusedAssets();
		while (!async.isDone && timer > 0)
		{
			timer -= Time.deltaTime;
			yield return null;
			//Debug.Log(async.progress + " " + timer);
		}
	    
		if(timer <= 0){ Debug.Log("CleanMemoryCoroutine tok more than 2 seconds, leaving ...");}
	    
		//Resources.UnloadUnusedAssets();
		//System.GC.Collect();
        
		//yield return new WaitForSeconds(1.0f);
	    
	}
    
	public bool IsPowerOfTwo(int x)
	{
		return (x != 0) && ((x & (x - 1)) == 0);
	}

	// Filter certain HTML-Tags and replace with valid TextMeshPro Tags
	// Also removing all incompatible Tags
	public string ConvertHTMLToValidTextMeshProText(string value)
	{
		//if (value.Contains("href")) { Debug.LogError("Text with href-Tag: " + value); }

		// Adjust some custom HTML-Tags
		value = value.Replace("<p></p>", "");

		// Filter certain HTML Tags and replace to non-Tags
		value = value.Replace("<li>", "[li]");
		value = value.Replace("</li>", "[/li]");
		value = value.Replace("<strong>", "[strong]");
		value = value.Replace("</strong>", "[/strong]");
		value = value.Replace("<br>", "[br]");
		value = value.Replace("<p>", "[p]");
		value = value.Replace("</p>", "[/p]");
		value = value.Replace("<ul>", "[ul]");
		value = value.Replace("</ul>", "[/ul]");
		value = value.Replace("<h1>", "[h1]");
		value = value.Replace("</h1>", "[/h1]");
		value = value.Replace("<h2>", "[h2]");
		value = value.Replace("</h2>", "[/h2]");

		// Filter href-Tags (<a href="...">...</a>) and convert to TextMeshPro link-Tag (<link="...">...</link>)
		int href_Tag_Start = value.IndexOf("<a");
		if (href_Tag_Start >= 0)
		{
			try
			{
				int href_Tag_End = value.IndexOf(">", href_Tag_Start);
				if (href_Tag_End > href_Tag_Start)
				{
					string href_Tag_Content = value.Substring(href_Tag_Start, (href_Tag_End - href_Tag_Start) + 1);
					//Debug.LogError(href_Tag_Content);

					int indexLink = href_Tag_Content.IndexOf(" href=\"");
					if (indexLink >= 0)
					{
						int webLinkStartindex = indexLink + 7;
						string subLink = href_Tag_Content.Substring(webLinkStartindex);
						int webLinkEndQuoteIndex = subLink.IndexOf("\"");
						if (webLinkEndQuoteIndex >= 0)
						{
							string weblink = subLink.Substring(0, webLinkEndQuoteIndex);
							//Debug.LogError("Link is: " + weblink);
							string textMeshProLink = "<u><link=\"" + weblink + "\">";
							if (value.Contains(href_Tag_Content)) {

								value = value.Replace(href_Tag_Content, textMeshProLink);
								//Debug.LogError("Replaced href-Tag with" + textMeshProLink);
							}
							value = value.Replace("</a>", "</link></u>");
						}
					}
				}
			}
				catch (Exception e)
				{
					Debug.Log("Could not parse href-Tag " + e.Message);
				}
		}

		// Keep TextMeshPro Tags by replace to non-Tags
		List<string> textMeshProTags = new List<string>() { "align", "alpha", "color", "b", "i", "cspace", "font", "indent", "line-height", "line-indent", "link", "lowercase", "uppercase", "smallcaps", "margin", "mark", "mspace", "noparse", "nobr", "page", "pos", "size", "space", "sprite", "s", "u", "style", "sub", "sup", "voffset", "width" };

		for (int i = 0; i < textMeshProTags.Count; i++)
		{
			string openingTag = "<" + textMeshProTags[i];
			string closingTag = "</" + textMeshProTags[i] + ">";
			string openingNonTag = "[" + textMeshProTags[i];
			string closingNonTag = "[/" + textMeshProTags[i] + "]";
			value = value.Replace(openingTag, openingNonTag);
			value = value.Replace(closingTag, closingNonTag);
		}


		// Remove all left tags which we do not support
		value = Regex.Replace(value, @"<[^>]+>| ", " ").Trim();

		// Replace filtered HTML-Tags to TextmeshPro compatible Tags
		value = value.Replace("[/li][li]", "[/li]\n<line-height=50%>\n</line-height>[li]");		// Optional: Spacing between list-elements
		value = value.Replace("[li]", "<indent=5%><b>•</b></indent><indent=10%>");
		value = value.Replace("[/li]", "</indent>");
		value = value.Replace("[strong]", "<b>");
		value = value.Replace("[/strong]", "</b>");
		value = value.Replace("[ul]", "");
		value = value.Replace("[/ul]", "");

		value = value.Replace("[br][h1]", "[h1]");
		value = value.Replace("[h1]", "\n\n<b><size=150%>");
		value = value.Replace("[/h1]", "</size></b>\n\n");
		value = value.Replace("[br][h2]", "[h2]");
		value = value.Replace("[h2]", "\n\n<b><size=125%>");
		value = value.Replace("[/h2]", "</size></b>\n\n");

		value = value.Replace("[br]", "\n");
		value = value.Replace("\n[p]", "\n<line-height=75%>\n</line-height>");
		value = value.Replace("[p]", "\n<line-height=75%>\n</line-height>");
		value = value.Replace("[/p]", "");

		// Replace TextMeshPro non-Tags back to valid Tags
		for (int i = 0; i < textMeshProTags.Count; i++)
		{
			string openingTag = "<" + textMeshProTags[i];
			string closingTag = "</" + textMeshProTags[i] + ">";
			string openingNonTag = "[" + textMeshProTags[i];
			string closingNonTag = "[/" + textMeshProTags[i] + "]";

			if (textMeshProTags[i] == "link") {

				openingTag = "<color=#61D2e0><u><" + textMeshProTags[i];
				closingTag = "</" + textMeshProTags[i] + "></u></color>";
			}

			value = value.Replace(openingNonTag, openingTag);
			value = value.Replace(closingNonTag, closingTag);
		}

		return value;
	}

	public void ShowHideRenderer(GameObject obj, bool enable)
	{
		bool shouldEnable = enable;

		Renderer[] rend = obj.GetComponentsInChildren<Renderer>( true );
		for ( int i = 0; i < rend.Length; i++ )
		{
			rend[i].enabled = shouldEnable;
		}

		Canvas[] can = obj.GetComponentsInChildren<Canvas>( true );
		for ( int i = 0; i < can.Length; i++ )
		{
			can[i].enabled = shouldEnable;
		}
	}

	public bool HasBlendShapes(GameObject obj, int minCount = 1)
	{
		SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
		if ( skinnedMeshRenderer == null )
		{
			return false;
		}

		if ( skinnedMeshRenderer.sharedMesh == null )
		{
			return false;
		}

		if ( skinnedMeshRenderer.sharedMesh.blendShapeCount <= 0 )
		{
			return false;
		}

		if ( skinnedMeshRenderer.sharedMesh.blendShapeCount < minCount )
		{

			return false;
		}

		return true;
	}

	public int GetBlendShapeIndex(SkinnedMeshRenderer skinnedMeshRenderer, string blendShapeName)
	{
		if ( skinnedMeshRenderer == null )
		{
			return -1;
		}

		if ( skinnedMeshRenderer.sharedMesh == null )
		{
			return -1;
		}

		for ( int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++ )
		{
			if ( skinnedMeshRenderer.sharedMesh.GetBlendShapeName( i ) == blendShapeName )
			{
				return i;
			}
		}

		for ( int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++ )
		{
			if ( skinnedMeshRenderer.sharedMesh.GetBlendShapeName( i ).Contains( blendShapeName ) )
			{
				return i;
			}
		}

		return -1;
	}

	public int GetBlendShapeIndex(GameObject obj, string blendShapeName)
	{
		SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
		if ( skinnedMeshRenderer == null )
		{
			return -1;
		}

		if ( skinnedMeshRenderer.sharedMesh == null )
		{
			return -1;
		}

		for ( int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++ )
		{

			if ( skinnedMeshRenderer.sharedMesh.GetBlendShapeName( i ) == blendShapeName )
			{
				return i;
			}
		}

		for ( int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++ )
		{

			if ( skinnedMeshRenderer.sharedMesh.GetBlendShapeName( i ).Contains( blendShapeName ) )
			{
				return i;
			}
		}

		return -1;
	}

	public bool HasBlendShapesIndex(GameObject obj, int index)
	{
		SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
		if ( skinnedMeshRenderer == null )
		{
			return false;
		}

		if ( skinnedMeshRenderer.sharedMesh == null )
		{
			return false;
		}

		if ( skinnedMeshRenderer.sharedMesh.blendShapeCount < index )
		{
			print( skinnedMeshRenderer.sharedMesh.blendShapeCount + " " + index );
			return false;
		}

		return true;
	}

	public bool IsPointerOverUIObject2(bool limitToUILayer = false)
	{

		try
		{

			if ( EventSystem.current == null ) return false;
			PointerEventData eventDataCurrentPosition = new PointerEventData( EventSystem.current );
			eventDataCurrentPosition.position = new Vector2( Input.mousePosition.x, Input.mousePosition.y );
			List<RaycastResult> results = new List<RaycastResult>();
			EventSystem.current.RaycastAll( eventDataCurrentPosition, results );

			bool hitUI = results.Count > 0;
			if ( hitUI )
			{
				if ( limitToUILayer )
				{
					// Also check if one of the objects is in the UI-layer
					for ( int i = 0; i < results.Count; i++ ) { if ( LayerMask.LayerToName( results[i].gameObject.layer ) == "UI" ) { return true; } }
					return false;
				}
				else
				{
					return true;
				}
			}

#if UNITY_EDITOR
			if ( EventSystem.current.IsPointerOverGameObject() ) { return true; }
#else
			if (Input.touchCount > 0)
			{
			var touch = Input.GetTouch(0);
			if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return true;
			}
#endif

		}
		catch ( Exception e )
		{

			print( "IsPointerOverUIObject error " + e.Message );
		}

		return false;
	}

	public bool IsInsideBounds(Vector3 worldPos, BoxCollider bc)
	{
		Vector3 localPos = bc.transform.InverseTransformPoint( worldPos );
		Vector3 delta = localPos - bc.center + bc.size * 0.5f;
		return Vector3.Max( Vector3.zero, delta ) == Vector3.Min( delta, bc.size );
	}

	public void DoIt()
    {
		//GameObjectUtility.RemoveMonoBehavioursWithMissingScript();

		/*
		Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
		for (int i = 0; i < transforms.Length; i++)
		{
			GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transforms[i].gameObject);
		}
		*/

		/*
		Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].GetComponent<AccessibleButton>() == null)
            {
				buttons[i].gameObject.AddComponent<AccessibleButton>();
				TextMeshProUGUI t = buttons[i].GetComponentInChildren<TextMeshProUGUI>(true);

				if (t != null) {

					buttons[i].GetComponent<AccessibleButton>().m_NameLabel = t.gameObject;
					Debug.Log("Text component " + t.text);
				}
                else { Debug.Log("No Text component " + buttons[i].name); }
			}
		}
		*/
	}

	/*
	public void DoIt2()
	{
		Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
		for (int i = 0; i < buttons.Length; i++)
		{
			if (buttons[i].GetComponent<AccessibleButton>() != null)
			{
				try { DestroyImmediate(buttons[i].GetComponent<AccessibleButton>()); }
                catch (Exception e) { print(e.Message); }
			}
		}

		TextMeshProUGUI[] labels = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
		for (int i = 0; i < labels.Length; i++)
		{
			if (labels[i].GetComponent<AccessibleLabel>() != null)
			{
				try { DestroyImmediate(labels[i].GetComponent<AccessibleLabel>()); }
				catch (Exception e) { print(e.Message); }
			}
		}
	}
	*/
}


#if UNITY_EDITOR

[CustomEditor(typeof(ToolsController))]
class ToolsControllerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		ToolsController myTarget = (ToolsController)target;

		//if (GUILayout.Button("DoIt")) { myTarget.DoIt(); }
		//if (GUILayout.Button("DoIt2")) { myTarget.DoIt2(); }
	}
}

#endif
