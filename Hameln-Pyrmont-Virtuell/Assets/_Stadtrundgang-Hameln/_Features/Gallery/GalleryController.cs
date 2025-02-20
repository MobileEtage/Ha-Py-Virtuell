using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using TMPro;
using SimpleJSON;

public class GalleryController : MonoBehaviour
{
	public bool fitWidth = true;
	private bool useSprites = false;

    [Space(10)]

    public RectTransform siteTitle;
    public ScrollRect scrollRect;
    public GameObject imagesHolder;
    public GameObject fullScreenImageRoot;
	public GameObject fullScreenRoot;
	public GameObject fullScreenImage;
    public GameObject fullScreenRawImage;
    public GameObject fullScreenCopyrightButton;
    public GameObject fullScreenCopyrightDescriptionContent;
    public TextMeshProUGUI fullScreenDescriptionLabel;
    public TextMeshProUGUI fullScreenDescriptionLabel_Landscape;
	public GameObject imageGradientLandscape;
    public bool fullScreenCopyrightIsOpen = false;
    public TextMeshProUGUI fullScreenCopyrightLabel;
    public TextMeshProUGUI fullScreenCopyrightHelperLabel;
    public TextMeshProUGUI imageIndexLabel;
    public TextMeshProUGUI imageIndexLabel_Landscape;

    public bool isFullscreen = false;

    private bool isLoading = false;
    private bool shouldRotate = false;
    private JSONNode featureData;
    private List<GameObject> imageElements = new List<GameObject>();
    private GameObject currentImage;

    private List<string> swipeAreaExcludedObjects = new List<string>();
    private float minSwipeWidthPercentage = 0.2f;
    private bool hitSwipe = false;
    private Vector3 swipeHitPoint;

	private float titleWidth = 0;
	
    public static GalleryController instance;
    void Awake()
    {
	    instance = this;
        
	    //if(PlayerPrefs.GetInt("spriteMode", 1) == 1){ useSprites = true; PlayerPrefs.SetInt("spriteMode", 0);}
	    //else{ useSprites = false; PlayerPrefs.SetInt("spriteMode", 1);}
	    
	    print("GalleryController " + useSprites);
    }

	void Start(){
		
		StartCoroutine(InitCoroutine());
	}
	
	public IEnumerator InitCoroutine(){

        while (!CanvasController.instance.IsReady()) { yield return null; }
        yield return null;
        titleWidth = siteTitle.sizeDelta.x;
        siteTitle.GetComponentInParent<Canvas>().gameObject.SetActive(false);
	}
	
    private void Update()
    {
        if (SiteController.instance.currentSite != null && SiteController.instance.currentSite.siteID != "GallerySite") return;

        if (OrientationUIController.instance != null && OrientationUIController.instance.isLandscape) {

            fullScreenDescriptionLabel_Landscape.transform.parent.gameObject.SetActive(true);
			imageGradientLandscape.SetActive(true);
            fullScreenDescriptionLabel.gameObject.SetActive(false);
            imageIndexLabel_Landscape.gameObject.SetActive(true);
            imageIndexLabel.gameObject.SetActive(false);
        }
        else {

            fullScreenDescriptionLabel_Landscape.transform.parent.gameObject.SetActive(false);
			imageGradientLandscape.SetActive(false);
            fullScreenDescriptionLabel.gameObject.SetActive(true);
            imageIndexLabel_Landscape.gameObject.SetActive(false);
            imageIndexLabel.gameObject.SetActive(true);
        }

        //DetectSwipe();
    }

    public IEnumerator LoadImagesCoroutine()
    {
        featureData = StationController.instance.GetStationFeature("gallery");
        if (featureData == null) yield break;
        if (featureData["images"] == null) yield break;
        if (featureData["images"].Count <= 0) yield break;

        scrollRect.verticalNormalizedPosition = 1;
        InfoController.instance.loadingCircle.SetActive(true);
        yield return new WaitForSeconds(0.25f);
	    
        imageElements.Clear();
        foreach (Transform child in imagesHolder.transform) { if (child.name != "Title") { Destroy(child.gameObject); } }
        for (int i = 0; i < featureData["images"].Count; i++)
        {
            if (featureData["images"][i]["url"].Value.EndsWith(".gif")) continue;

        	GameObject imageObj;
        	if(useSprites){
        	
	            imageObj = ToolsController.instance.InstantiateObject("UI/GalleryImagePrefab", imagesHolder.transform);
	
	            imageObj.GetComponent<GalleryImage>().uiImage.shouldEnvelopeParent = true;
	            imageObj.GetComponent<GalleryImage>().uiImage.adjustHeightOfParent = true;
	            imageObj.GetComponent<GalleryImage>().uiImage.updateParentImageSize = true;
	            imageObj.GetComponent<GalleryImage>().uiImage.defaultWidth = 920f;
	            imageObj.GetComponent<GalleryImage>().uiImage.url = featureData["images"][i]["url"].Value;
	            imageObj.GetComponent<GalleryImage>().index = i;
	
	            if (fitWidth)
	            {     
	            	if( titleWidth <= 0 ) { titleWidth = siteTitle.sizeDelta.x; }
                    imageObj.GetComponent<GalleryImage>().uiImage.defaultWidth = titleWidth;
                }
            }
	        else{
	        	
		        imageObj = ToolsController.instance.InstantiateObject("UI/GalleryRawImagePrefab", imagesHolder.transform);
	
		        imageObj.GetComponent<GalleryImage>().uiRawImage.shouldEnvelopeParent = true;
		        imageObj.GetComponent<GalleryImage>().uiRawImage.adjustHeightOfParent = true;
		        imageObj.GetComponent<GalleryImage>().uiRawImage.updateParentImageSize = true;
		        imageObj.GetComponent<GalleryImage>().uiRawImage.defaultWidth = 920f;
		        imageObj.GetComponent<GalleryImage>().uiRawImage.url = featureData["images"][i]["url"].Value;
		        imageObj.GetComponent<GalleryImage>().index = i;
	
		        if (fitWidth)
		        {     
			        if( titleWidth <= 0 ) { titleWidth = siteTitle.sizeDelta.x; }
			        imageObj.GetComponent<GalleryImage>().uiRawImage.defaultWidth = titleWidth;
                }
            }
	        
            // Button event
            imageObj.GetComponentInChildren<Button>().onClick.AddListener(() => SwitchToFullscreen(imageObj));

            // Text
            if (featureData["images"][i]["description"] != null)
            {
                GameObject label = ToolsController.instance.InstantiateObject("UI/GalleryImageText", imagesHolder.transform);
                label.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslationFromNode(featureData["images"][i]["description"]);
                imageObj.GetComponent<GalleryImage>().descriptionText = LanguageController.GetTranslationFromNode(featureData["images"][i]["description"]);
            }

            if (featureData["images"][i]["copyright"] != null && featureData["images"][i]["copyright"].Value != "")
            {
                imageObj.GetComponent<GalleryImage>().copyrightButton.SetActive(false);
                imageObj.GetComponent<GalleryImage>().copyrightDescriptionContent.SetActive(true);

                string text = "<sprite=5> ";
                imageObj.GetComponent<GalleryImage>().copyrightText = featureData["images"][i]["copyright"].Value;
                imageObj.GetComponent<GalleryImage>().copyrightLabel.text = text;
                imageObj.GetComponent<GalleryImage>().copyrightHelperLabel.text = text;
            }
            else
            {
                imageObj.GetComponent<GalleryImage>().copyrightText = "";
                imageObj.GetComponent<GalleryImage>().copyrightLabel.text = "";
                imageObj.GetComponent<GalleryImage>().copyrightDescriptionContent.SetActive(false);
            }

            imageElements.Add(imageObj);
        }

        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("GallerySite"));
        yield return new WaitForSeconds(0.5f);
        ARController.instance.StopARSession();
        ARMenuController.instance.DisableMenu(true);

        InfoController.instance.loadingCircle.SetActive(false);
    }

    public void SwitchToFullscreen(GameObject imageObj)
	{
		fullScreenImage.SetActive(useSprites);
		fullScreenRawImage.SetActive(!useSprites);
		
        currentImage = imageObj;

        isFullscreen = true;
	    fullScreenImageRoot.SetActive(true);
        
		if(useSprites && imageObj.GetComponent<GalleryImage>().uiImage != null){ 
	    	fullScreenImage.GetComponent<Image>().sprite = imageObj.GetComponent<GalleryImage>().uiImage.GetComponent<Image>().sprite;
	    }	    
	    else if(imageObj.GetComponent<GalleryImage>().uiRawImage != null){ 
	    	fullScreenRawImage.GetComponent<RawImage>().texture = imageObj.GetComponent<GalleryImage>().uiRawImage.GetComponent<RawImage>().texture;
	    }

		if (useSprites && fullScreenImage.GetComponent<Image>() != null && fullScreenImage.GetComponent<Image>().sprite != null)
        {
            float aspect = fullScreenImage.GetComponent<Image>().sprite.rect.width / fullScreenImage.GetComponent<Image>().sprite.rect.height;
		    fullScreenImage.GetComponent<AspectRatioFitter>().aspectRatio = aspect;
		    fullScreenRoot.GetComponent<AspectRatioFitter>().aspectRatio = aspect;
        }
	    else if (fullScreenRawImage.GetComponent<RawImage>() && fullScreenRawImage.GetComponent<RawImage>().texture != null)
	    {
		    float aspect = (float)fullScreenRawImage.GetComponent<RawImage>().texture.width / fullScreenRawImage.GetComponent<RawImage>().texture.height;
		    fullScreenRawImage.GetComponent<AspectRatioFitter>().aspectRatio = aspect;
		    fullScreenRoot.GetComponent<AspectRatioFitter>().aspectRatio = aspect;  
	    }

        fullScreenDescriptionLabel.text = imageObj.GetComponent<GalleryImage>().descriptionText;
        fullScreenDescriptionLabel_Landscape.text = imageObj.GetComponent<GalleryImage>().descriptionText;

        fullScreenRoot.GetComponentInChildren<GalleryImage>(true).copyrightText = imageObj.GetComponent<GalleryImage>().copyrightText;
        fullScreenRoot.GetComponentInChildren<GalleryImage>().SetIsOpen(false);

        imageIndexLabel.text = (imageObj.GetComponent<GalleryImage>().index + 1) + "<color=#bbbbbb>/" + featureData["images"].Count;
        imageIndexLabel_Landscape.text = (imageObj.GetComponent<GalleryImage>().index + 1) + "<color=#bbbbbb>/" + featureData["images"].Count;
    }

    public void BackFromFullscreen()
    {
        isFullscreen = false;
        fullScreenImageRoot.SetActive(false);
    }

    public void NextPreviousImage(int dir)
    {
        if (featureData == null) return;
        if (featureData["images"] == null) return;
        if (featureData["images"].Count <= 1) return;

        if (!isFullscreen) return;
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(NextPreviousImageCoroutine(dir));
    }

    public IEnumerator NextPreviousImageCoroutine(int dir)
    {
        for (int i = 0; i < imageElements.Count; i++)
        {
            if(imageElements[i] == currentImage)
            {
                if (dir == 1)
                {
                    if (i + 1 >= imageElements.Count) { SwitchToFullscreen(imageElements[0]); }
                    else { SwitchToFullscreen(imageElements[i + 1]); }
                }
                else
                {
                    if (i - 1 < 0) { SwitchToFullscreen(imageElements[imageElements.Count-1]); }
                    else { SwitchToFullscreen(imageElements[i - 1]); }
                }
                break;
            }
        }

        yield return null;
        isLoading = false;
    }

    private void DetectSwipe()
    {
        if (Input.GetMouseButtonDown(0) && ValidSwipeArea())
        {
            hitSwipe = true;
            swipeHitPoint = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0) && hitSwipe)
        {

            hitSwipe = false;

            if (ValidSwipeArea())
            {

                Vector3 dist = swipeHitPoint - Input.mousePosition;

                if (dist.x > Screen.width * minSwipeWidthPercentage)
                {
                    NextPreviousImage(1);
                }
                else if (dist.x < -Screen.width * minSwipeWidthPercentage)
                {
                    NextPreviousImage(-1);
                }
            }
        }
    }

    private bool ValidSwipeArea()
    {

#if !UNITY_EDITOR
		if( Input.touchCount > 0 && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject( Input.touches[0].fingerId ) )				
#else
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
#endif

        {
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = Input.mousePosition;

            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, raycastResults);

            if (raycastResults.Count > 0)
            {
                foreach (var go in raycastResults)
                {
                    if (swipeAreaExcludedObjects.Contains(go.gameObject.name))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public void Back()
    {
        InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation);

        //if (isLoading) return;
        //isLoading = true;
        //StartCoroutine(BackCoroutine());
    }

    public IEnumerator BackCoroutine()
	{
        yield return StartCoroutine(StationController.instance.BackToStationSiteCoroutine());
        ARMenuController.instance.MarkMenuButton("");
        ARMenuController.instance.currentFeature = "";
        Reset();

        isLoading = false;
    }

    public void Reset()
    {
        imageElements.Clear();
	    foreach (Transform child in imagesHolder.transform) { if (child.name != "Title") { Destroy(child.gameObject); } }

        fullScreenImageRoot.SetActive(false);
	    if(useSprites && fullScreenImage.GetComponent<Image>() != null && fullScreenImage.GetComponent<Image>().sprite != null) { Destroy(fullScreenImage.GetComponent<Image>().sprite); fullScreenImage.GetComponent<Image>().sprite = null; }
	    else if(fullScreenRawImage.GetComponent<RawImage>() != null && fullScreenRawImage.GetComponent<RawImage>().texture != null) { Destroy(fullScreenRawImage.GetComponent<RawImage>().texture); fullScreenRawImage.GetComponent<RawImage>().texture = null; }

        isFullscreen = false;
        fullScreenCopyrightIsOpen = false;
    }
}