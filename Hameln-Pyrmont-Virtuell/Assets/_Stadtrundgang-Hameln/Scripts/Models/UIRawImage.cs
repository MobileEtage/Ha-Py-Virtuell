using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// This class calls the ImageDownloadController to load or 
// download an image file and assign the image texture to the image component of this object

[RequireComponent(typeof(RawImage))]
public class UIRawImage : MonoBehaviour {

	public string url = "";
	public string imageType = "";
	public float defaultWidth = 940f;

    [Space(10)]

    private List<int> maxSizes = new List<int>() { 2048 };
    private int targetSize = 2048;

    [Space(10)]

    public bool shouldEnvelopeParent = false;
	public bool envelopeParentDone = false;

	[Space(10)]

	public bool adjustHeightOfParent = false;
	public bool updateParentImageSize = true;

	[Space(10)]

	public bool textureLoaded = false;
	public bool checkTextureExits = false;

	private bool heightAdjusted = false;
	private int adjustheightDelay = 1;

	void Start(){ 
				
		if( ImageDownloadController.instance == null ) return;
		
		ImageDownloadController.instance.LoadRawImageFromCacheOrDownload( 
			url, GetComponent<RawImage>(), maxSizes, targetSize, this
		);
	}

    public void SetSizes(List<int> maxSizes, int targetSize)
    {
        this.maxSizes = maxSizes;
        this.targetSize = targetSize;
    }
    
	public void HideBeforeLoaded(){
		
		textureLoaded = false;
		checkTextureExits = true;
		
		if( GetComponentInParent<LayoutElement>() == null ) return;
		GetComponentInParent<LayoutElement>().minHeight = 0f;
		transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(defaultWidth, 0f );
	}
	
	public void OnTextureLoaded(){

        envelopeParentDone = false;
        heightAdjusted = false;
        StartCoroutine( OnTextureLoadedCoroutine() );
	}

	public IEnumerator OnTextureLoadedCoroutine(){
		
		yield return new WaitForEndOfFrame();
		
		textureLoaded = true;
		if( checkTextureExits ){
		
			if( GetComponentInParent<LayoutElement>() == null ) yield break;

			//print("OnTextureLoaded " + defaultWidth);

			GetComponentInParent<LayoutElement>().minHeight = defaultWidth;
			transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(defaultWidth, defaultWidth);
		}
	}
	
	void Update(){
		
		if( checkTextureExits && !textureLoaded ) return;
		if (GetComponent<RawImage>().texture == null) return;
		//if (GetComponent<RawImage>().texture.name == "dummy1" || GetComponent<RawImage>().texture.name == "dummy") return;
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
	}

	public void EnvelopeParent()
	{
		GetComponent<AspectRatioFitter>().enabled = true;
		float aspect = (float)GetComponent<RawImage>().texture.width / (float)GetComponent<RawImage>().texture.height;		
		GetComponent<AspectRatioFitter>().aspectRatio = aspect;
	}

	public void AdjustHeight(){
				
		float aspect = (float)GetComponent<RawImage>().texture.width/(float)GetComponent<RawImage>().texture.height;
		//print("AdjustHeight " + aspect + " " + defaultWidth);
	    
		//if( aspect > 1.1f || aspect < 0.9f ){
			
			if( aspect > 0 ){

				float targetHeight = defaultWidth / aspect;
				if (updateParentImageSize) { transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector3(defaultWidth, targetHeight); }
				GetComponentInParent<LayoutElement>().minHeight = targetHeight;	
				GetComponentInParent<LayoutElement>().preferredHeight = targetHeight;
				
				GameObject slider = ToolsController.instance.FindParentByName(this.gameObject, "InfoImageSlider");
				if( slider != null ){
					if( slider.GetComponent<LayoutElement>() != null ){
						slider.GetComponent<LayoutElement>().minHeight = targetHeight;
						slider.GetComponent<LayoutElement>().preferredHeight = targetHeight;
					}
				}
			}
		//}
		
		heightAdjusted = true;
	}

	public void EnableEnvelopeParent()
	{
		shouldEnvelopeParent = true;
		envelopeParentDone = false;
		adjustheightDelay = 1;
	}
}
