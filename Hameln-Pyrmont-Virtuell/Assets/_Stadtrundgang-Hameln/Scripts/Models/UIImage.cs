using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// This class calls the ImageDownloadController to load or 
// download an image file and assign the image sprite to the image component of this object

[RequireComponent(typeof(Image))]
public class UIImage : MonoBehaviour {

    public string url = "";
    public string imageType = "";
    public float defaultWidth = 940f;

    [Space(10)]

    private List<int> maxSizes = new List<int>() { 1024 };
    private int targetSize = 1024;

    [Space(10)]

    public bool shouldEnvelopeParent = false;
    public bool envelopeParentDone = false;

    [Space(10)]

    public bool adjustHeightOfParent = false;
    public bool updateParentImageSize = true;

    [Space(10)]

    public bool adjustWidthOfParent = false;

    [Space(10)]

    public bool spriteLoaded = false;
	public bool checkSpriteExits = false;

    private bool heightAdjusted = false;
    private bool widthAdjusted = false;
    private int adjustheightDelay = 1;

	void Start(){ 
				
		if( ImageDownloadController.instance == null ) return;
		
		ImageDownloadController.instance.LoadImageFromCacheOrDownload( 
			url, GetComponent<Image>(), maxSizes, targetSize, this
		);
	}
	
	public void SetSizes(List<int> maxSizes, int targetSize)
	{
		this.maxSizes = maxSizes;
		this.targetSize = targetSize;
	}

	public void HideBeforeLoaded(){
		
		spriteLoaded = false;
		checkSpriteExits = true;
		
		if( GetComponentInParent<LayoutElement>() == null ) return;
		GetComponentInParent<LayoutElement>().minHeight = 0f;
		transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(defaultWidth, 0f );
	}
	
	public void OnSpriteLoaded(){

		envelopeParentDone = false;
		heightAdjusted = false;
		widthAdjusted = false;
        if (this.gameObject.activeInHierarchy) { StartCoroutine(OnSpriteLoadedCoroutine()); }

        /*
		spriteLoaded = true;
		if( checkSpriteExits ){
		
			if( GetComponentInParent<LayoutElement>() == null ) return;

			print("OnSpriteLoaded");

			GetComponentInParent<LayoutElement>().minHeight = defaultWidth;
			transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2( defaultWidth, defaultWidth );
		}
		*/
    }

    public IEnumerator OnSpriteLoadedCoroutine(){
		
		yield return new WaitForEndOfFrame();

		if ( GetComponentInParent<TourListElement>() != null ){ GetComponentInParent<TourListElement>().SetHeight(); }

		spriteLoaded = true;
		if( checkSpriteExits ){
		
			if( GetComponentInParent<LayoutElement>() == null ) yield break;

			//print("OnSpriteLoaded " + defaultWidth);

            GetComponentInParent<LayoutElement>().minHeight = defaultWidth;
			transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(defaultWidth, defaultWidth);
		}
	}

	void Update(){
		
		if( checkSpriteExits && !spriteLoaded ) return;
		if (GetComponent<Image>().sprite == null) return;
		//if (GetComponent<Image>().sprite.name == "dummy1" || GetComponent<Image>().sprite.name == "dummy") return;
		if (adjustheightDelay > 0) { adjustheightDelay--; return; }

        if (shouldEnvelopeParent && !envelopeParentDone)
        {
            EnvelopeParent();
            envelopeParentDone = true;
        }

        if (adjustHeightOfParent && !heightAdjusted )
        {
            AdjustHeight();
        }

        if (adjustWidthOfParent && !widthAdjusted)
        {
            AdjustWidth();
        }
    }

    public void EnvelopeParent()
    {
        GetComponent<AspectRatioFitter>().enabled = true;
	    float aspect = GetComponent<Image>().sprite.rect.width / GetComponent<Image>().sprite.rect.height;
        
	    //print("EnvelopeParent " + GetComponent<Image>().sprite.rect.width + " " + GetComponent<Image>().sprite.rect.height + " " + aspect);
	    
        GetComponent<AspectRatioFitter>().aspectRatio = aspect;
    }

    public void AdjustHeight(){
				
		float aspect = GetComponent<Image>().sprite.rect.width/GetComponent<Image>().sprite.rect.height;
	    //print("AdjustHeight " + aspect + " " + defaultWidth);
	    
		//if( aspect > 1.1f || aspect < 0.9f ){
			
			if( aspect > 0 ){

                float targetHeight = defaultWidth / aspect;
                if (updateParentImageSize) { transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector3(defaultWidth, targetHeight); }
				GetComponentInParent<LayoutElement>().minHeight = targetHeight;	
				GetComponentInParent<LayoutElement>().preferredHeight = targetHeight;
				
				GameObject slider = ToolsController.instance.FindParentByName(this.gameObject, "InfoImageSlider");
				if( slider != null ){
					if( slider.GetComponent<LayoutElement>() != null && slider.GetComponent<LayoutElement>().preferredHeight < targetHeight)
                    {
						slider.GetComponent<LayoutElement>().minHeight = targetHeight;
						slider.GetComponent<LayoutElement>().preferredHeight = targetHeight;
					}
				}
			}
		//}
		
		heightAdjusted = true;
	}

    public void AdjustWidth()
    {

        float aspect = GetComponent<Image>().sprite.rect.width / GetComponent<Image>().sprite.rect.height;
		float h = GetComponentInParent<LayoutElement>().minHeight;
		float w = aspect * h;

        GetComponentInParent<LayoutElement>().minWidth = w;
        GetComponentInParent<LayoutElement>().preferredWidth = w;
        if (updateParentImageSize) { transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector3(w, h); }

        widthAdjusted = true;
    }

    public void EnableEnvelopeParent()
    {
        shouldEnvelopeParent = true;
        envelopeParentDone = false;
        adjustheightDelay = 1;
    }
}
