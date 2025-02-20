
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

// This class handles downloading and assigning images
/*

An image with a UIImage script will call this controller to load or download the required image from the given url
We can add localized urls in the lang.json, so for every country it will download custom images.
If no valid url for the country is found in the lang.json, no images will be downloaded

*/

public class ImageDownloadController : MonoBehaviour { 
	
	private bool shouldCompressImages = false;
	private int downloadImageTimeout = 60;	
	private Texture2D tex;
	
	public static ImageDownloadController instance;
	void Awake () {

		instance = this;

		//if( !Directory.Exists( Application.persistentDataPath + "/" + saveFolder ) ){
		//	Directory.CreateDirectory( Application.persistentDataPath + "/" + saveFolder );
		//}
	}
		
	// Loads an image from saved url path or downloads the image if it not exists
	public void LoadImageFromCacheOrDownload( string url, Image img, List<int> maxSizes, int targetSize, UIImage uiImage  ){

		print("LoadImageFromCacheOrDownload " + url);
		
        GameObject loadingImage = ToolsController.instance.FindGameObjectByName(img.gameObject, "LoadingImage");
        if (loadingImage != null) { loadingImage.SetActive(false); }

        // Get url for device country
        string localizedURL = LanguageController.GetTranslation(url);
		
		// Check if it is a valid url
		Uri uriResult;
		bool isURL = Uri.TryCreate(localizedURL, UriKind.Absolute, out uriResult) 
			&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
		if( !isURL ) return;

		// Load from local save path or download
		string texturePath = ToolsController.instance.GetSavePathFromURL(url, targetSize);		
		if ( File.Exists( texturePath ) ){

			//print("File exists " + localizedURL);

			//StartCoroutine( LoadImageFromLocalStorageCoroutine( texturePath, img ) );
			//return;
			
			byte[] fileData = File.ReadAllBytes(texturePath);
			tex = new Texture2D(2, 2);
			tex.wrapMode = TextureWrapMode.Clamp;
			//tex.LoadRawTextureData(fileData);
			//tex.Apply();
					
			//if( tex.LoadImage(fileData) ){
			if( ImageConversion.LoadImage(tex, fileData, true) ){
			        
				try{
							
					if(shouldCompressImages){ 				
						if(
							ToolsController.instance.IsPowerOfTwo(tex.width) && 
							ToolsController.instance.IsPowerOfTwo(tex.height))
						{ tex.Compress(true); }							
					}
							
				}catch(Exception e){
					print("Error compress " + e.Message);
				}
				
				// create sprite
				if( img != null ){

					Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 100, 0, SpriteMeshType.Tight);
					sprite.name = Path.GetFileNameWithoutExtension(texturePath);
					img.sprite = sprite;
					img.preserveAspect = true;
					if (uiImage != null) { uiImage.OnSpriteLoaded(); }
					tex = null;
				}
			}else{
				print( "Could not load image from path: " + texturePath );
			}
		}
		else{

			//print("File not exists " + localizedURL);

			StartCoroutine( DownloadImageCoroutine( localizedURL, img, maxSizes, targetSize, uiImage ) );
		}
	}
	
	public void LoadRawImageFromCacheOrDownload( string url, RawImage img, List<int> maxSizes, int targetSize, UIRawImage uiRawImage  ){
		print("LoadRawImageFromCacheOrDownload " + url);

		// Get url for device country
		string localizedURL = LanguageController.GetTranslation(url);
		
		// Check if it is a valid url
		Uri uriResult;
		bool isURL = Uri.TryCreate(localizedURL, UriKind.Absolute, out uriResult) 
			&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
		if( !isURL ) return;

		// Load from local save path or download
		string texturePath = ToolsController.instance.GetSavePathFromURL(url, targetSize);
		if ( File.Exists( texturePath ) ){

			byte[] fileData = File.ReadAllBytes(texturePath);		
			tex = new Texture2D(2, 2);		
			tex.wrapMode = TextureWrapMode.Clamp;
			//tex.LoadRawTextureData(fileData);
			//tex.Apply();
					
			//if( tex.LoadImage(fileData) ){
			if( ImageConversion.LoadImage(tex, fileData, true) ){
			      
				try{
							
					if(shouldCompressImages){ 				
						if(
							ToolsController.instance.IsPowerOfTwo(tex.width) && 
							ToolsController.instance.IsPowerOfTwo(tex.height))
						{ tex.Compress(true); }							
					}
							
				}catch(Exception e){
					print("Error compress " + e.Message);
				}
				
				img.texture = tex;
				tex = null;
				
			}else{
				print( "Could not load image from path: " + texturePath );
			}
		}
		else{

			//print("File not exists " + localizedURL);
			StartCoroutine( DownloadRawImageCoroutine( localizedURL, img, maxSizes, uiRawImage ) );
		}
	}
	
	public void LoadTextureIntoSpriteImage(Texture2D tex, Image img)
	{
        if (img != null)
        {
            try
            {
                if (shouldCompressImages)
                {
                    if (
                        ToolsController.instance.IsPowerOfTwo(tex.width) &&
                        ToolsController.instance.IsPowerOfTwo(tex.height))
                    { tex.Compress(true); }
                }

            }
            catch (Exception e)
            {
                print("Error compress " + e.Message);
            }

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 100, 0, SpriteMeshType.Tight);
            sprite.name = Path.GetFileNameWithoutExtension(tex.name);
            img.sprite = sprite;
	        img.preserveAspect = true;
	        tex = null;
        }
    }

	// Coroutine to download an save an image
	public IEnumerator DownloadImageCoroutine( string url, Image img, List<int> maxSizes, int targetSize, UIImage uiImage  )
	{
		GameObject loadingImage = ToolsController.instance.FindGameObjectByName(img.gameObject, "LoadingImage");
		if(loadingImage != null) { loadingImage.SetActive(true); }

		using ( UnityWebRequest uwr = UnityWebRequestTexture.GetTexture( url, false ) )
		{
			uwr.timeout = downloadImageTimeout;
			uwr.SendWebRequest();
            while (!uwr.isDone){ yield return null; if (loadingImage != null) { loadingImage.GetComponent<Image>().fillAmount = uwr.downloadProgress; } }
            if (loadingImage != null) { loadingImage.GetComponent<Image>().fillAmount = 1.0f; yield return null; }

            if (!uwr.isNetworkError && !uwr.isHttpError)
			{
                tex = DownloadHandlerTexture.GetContent(uwr);
				if( tex != null ){

					tex.name = Path.GetFileNameWithoutExtension(url);

                    // Iterate through all maxSizes and save version of tex with this size
                    for (int i = 0; i < maxSizes.Count; i++)
                    {
                        int maxSize = maxSizes[i];
                        bool shouldResize = (tex.width > maxSizes[i]) || (tex.height > maxSizes[i]);
						if (!shouldResize || maxSize <= 0)
						{
                            string texturePathTmp = ToolsController.instance.GetSavePathFromURL(url, maxSize);
                            string extensionTmp = Path.GetExtension(url).ToLower();
                            if (extensionTmp == ".jpg" || extensionTmp == ".jpeg") { File.WriteAllBytes(texturePathTmp, tex.EncodeToJPG()); }
                            else { File.WriteAllBytes(texturePathTmp, tex.EncodeToPNG()); }

                            if (maxSize == targetSize) { LoadTextureIntoSpriteImage(tex, img); }
							continue;
                        }

                        int targetWidth = maxSize;
                        int targetHeight = maxSize;
                        if (tex.width > tex.height)
                        {
                            float ratio = (float)tex.height / (float)tex.width;
                            targetWidth = maxSize;
                            targetHeight = (int)(maxSize * ratio);
                        }
                        else
                        {
                            float ratio = (float)tex.width / (float)tex.height;
                            targetHeight = maxSize;
                            targetWidth = (int)(maxSize * ratio);
                        }

                        TextureScale.Bilinear(tex, targetWidth, targetHeight);

                        string texturePath = ToolsController.instance.GetSavePathFromURL(url, maxSize);
                        string extension = Path.GetExtension(url).ToLower();
                        if (extension == ".jpg" || extension == ".jpeg"){ File.WriteAllBytes(texturePath, tex.EncodeToJPG()); }
                        else { File.WriteAllBytes(texturePath, tex.EncodeToPNG()); }

                        if (maxSize == targetSize) { LoadTextureIntoSpriteImage(tex, img); }
                    }

                    if (uiImage != null) { uiImage.OnSpriteLoaded(); }
                    if (loadingImage != null) { loadingImage.SetActive(false); }
                }
                else
				{
                    //if (loadingImage != null) { loadingImage.SetActive(false); }
                }
			}
			else{
				print( uwr.error );
			}
		}
	}
	
	// Coroutine to download an save an image
	public IEnumerator DownloadRawImageCoroutine( string url, RawImage img, List<int> maxSizes, UIRawImage uiRawImage  )
	{
        GameObject loadingImage = ToolsController.instance.FindGameObjectByName(img.gameObject, "LoadingImage");

        yield return StartCoroutine(
			DownloadTextureCoroutine(url, img, maxSizes, false, (bool success, Texture2D tex) => {

				if( success && img != null ){

					try{
							
						if(shouldCompressImages){ 				
							if(
								ToolsController.instance.IsPowerOfTwo(tex.width) && 
								ToolsController.instance.IsPowerOfTwo(tex.height))
							{ tex.Compress(true); }							
						}
							
					}catch(Exception e){
						print("Error compress " + e.Message);
					}

                    if (loadingImage != null) { loadingImage.SetActive(false); }
					img.texture = tex;
					tex = null;
					uiRawImage.OnTextureLoaded();
				}
			})
		);
	}
	
	public IEnumerator DownloadTextureCoroutine( string url, List<int> maxSizes, bool resizePowerOfTwo, Action<bool, Texture2D> Callback  )
	{
		using ( UnityWebRequest uwr = UnityWebRequestTexture.GetTexture( url, false ) )
		{
			uwr.timeout = downloadImageTimeout;
			yield return uwr.SendWebRequest();

			if ( !uwr.isNetworkError && !uwr.isHttpError)
			{
				//var tex = DownloadHandlerTexture.GetContent(uwr);
				tex = DownloadHandlerTexture.GetContent(uwr);
				
				if( tex != null ){
					
                    for (int i = 0; i < maxSizes.Count; i++)
                    {
                        int maxSize = maxSizes[i];
                        bool shouldResize = (tex.width > maxSizes[i]) || (tex.height > maxSizes[i]);
                        if (!shouldResize || maxSize <= 0){

                            string texturePathTmp = ToolsController.instance.GetSavePathFromURL(url, maxSize);
                            string extensionTmp = Path.GetExtension(url).ToLower();
                            if (extensionTmp == ".jpg" || extensionTmp == ".jpeg") { File.WriteAllBytes(texturePathTmp, tex.EncodeToJPG()); }
                            else { File.WriteAllBytes(texturePathTmp, tex.EncodeToPNG()); }

                            continue; 
						}

                        int targetWidth = maxSize;
                        int targetHeight = maxSize;
                        if (tex.width > tex.height)
                        {
                            float ratio = (float)tex.height / (float)tex.width;
                            targetWidth = maxSize;
                            targetHeight = (int)(maxSize * ratio);
                        }
                        else
                        {
                            float ratio = (float)tex.width / (float)tex.height;
                            targetHeight = maxSize;
                            targetWidth = (int)(maxSize * ratio);
                        }

                        TextureScale.Bilinear(tex, targetWidth, targetHeight);

                        if (resizePowerOfTwo)
                        {
                            tex = MakePowerOfTwoTexture(tex);

                            string texturePath = ToolsController.instance.GetSavePathFromURL(url, maxSize);
                            texturePath = Path.ChangeExtension(texturePath, ".png");
                            File.WriteAllBytes(texturePath, tex.EncodeToPNG());
                        }
                        else
                        {

                            string texturePath = ToolsController.instance.GetSavePathFromURL(url, maxSize);
                            string extension = Path.GetExtension(url).ToLower();
                            if (extension == ".jpg" || extension == ".jpeg")
                            {
                                File.WriteAllBytes(texturePath, tex.EncodeToJPG());
                            }
                            else { File.WriteAllBytes(texturePath, tex.EncodeToPNG()); }
                        }
                    }

					Callback(true, tex);
				}
			}
			else{
				
				print( uwr.error );
				Callback(false, null);
			}			
		}
	}


    public IEnumerator DownloadTextureCoroutine(string url, RawImage img, List<int> maxSizes, bool resizePowerOfTwo, Action<bool, Texture2D> Callback)
    {
        GameObject loadingImage = ToolsController.instance.FindGameObjectByName(img.gameObject, "LoadingImage");
        if (loadingImage != null) { loadingImage.SetActive(true); }

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url, false))
        {
            uwr.timeout = downloadImageTimeout;
            //yield return uwr.SendWebRequest();
            
			uwr.SendWebRequest();
            while (!uwr.isDone) { yield return null; if (loadingImage != null) { loadingImage.GetComponent<Image>().fillAmount = uwr.downloadProgress; } }
            if (loadingImage != null) { loadingImage.GetComponent<Image>().fillAmount = 1.0f; yield return null; }

            if (!uwr.isNetworkError && !uwr.isHttpError)
            {
                //var tex = DownloadHandlerTexture.GetContent(uwr);
                tex = DownloadHandlerTexture.GetContent(uwr);

                if (tex != null)
                {
                    for (int i = 0; i < maxSizes.Count; i++)
                    {
                        int maxSize = maxSizes[i];
                        bool shouldResize = (tex.width > maxSizes[i]) || (tex.height > maxSizes[i]);
                        if (!shouldResize || maxSize <= 0) {

                            string texturePathTmp = ToolsController.instance.GetSavePathFromURL(url, maxSize);
                            string extensionTmp = Path.GetExtension(url).ToLower();
                            if (extensionTmp == ".jpg" || extensionTmp == ".jpeg") { File.WriteAllBytes(texturePathTmp, tex.EncodeToJPG()); }
                            else { File.WriteAllBytes(texturePathTmp, tex.EncodeToPNG()); }

                            continue; 
						}

                        int targetWidth = maxSize;
                        int targetHeight = maxSize;
                        if (tex.width > tex.height)
                        {
                            float ratio = (float)tex.height / (float)tex.width;
                            targetWidth = maxSize;
                            targetHeight = (int)(maxSize * ratio);
                        }
                        else
                        {
                            float ratio = (float)tex.width / (float)tex.height;
                            targetHeight = maxSize;
                            targetWidth = (int)(maxSize * ratio);
                        }

                        TextureScale.Bilinear(tex, targetWidth, targetHeight);

                        if (resizePowerOfTwo)
                        {
                            tex = MakePowerOfTwoTexture(tex);

                            string texturePath = ToolsController.instance.GetSavePathFromURL(url, maxSize);
                            texturePath = Path.ChangeExtension(texturePath, ".png");
                            File.WriteAllBytes(texturePath, tex.EncodeToPNG());
                        }
                        else
                        {
                            string texturePath = ToolsController.instance.GetSavePathFromURL(url, maxSize);
                            string extension = Path.GetExtension(url).ToLower();
                            if (extension == ".jpg" || extension == ".jpeg")
                            {
                                File.WriteAllBytes(texturePath, tex.EncodeToJPG());
                            }
                            else { File.WriteAllBytes(texturePath, tex.EncodeToPNG()); }
                        }
                    }

                    Callback(true, tex);
                }
            }
            else
            {
                print(uwr.error);
                Callback(false, null);
            }
        }
    }

    public Texture2D MakePowerOfTwoTexture(Texture2D tex){
				
		if(tex.width > 2048 || tex.height > 2048) return tex;

		int newWidth = (int)Mathf.NextPowerOfTwo(tex.width);
		int newHeight = (int)Mathf.NextPowerOfTwo(tex.height);
		int morePixelWidth = newWidth-tex.width;
		int morePixelHeight = newHeight-tex.height;
		int hW = morePixelWidth /2;		
		int hH = morePixelHeight /2;
		
		Texture2D newTex = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, true);
		
		for (int y = 0; y < newHeight; y++)
		{
			for (int x = 0; x < newWidth; x++)
			{
				if( 
					y <= hH || y > (hH+tex.height-1) ||
					x <= hW || x > (hW+tex.width-1)
				){ 
					newTex.SetPixel(x, y, new Color(0,0,0,0));				
				}
				else{
					
					//int pos = (x-hW)*(y-hH);
					newTex.SetPixel(x, y, tex.GetPixel(x-hW, y-hH));
				}
			}
		}
		newTex.Apply();
		return newTex;
		
		//Color32[] newColors = new Color32[newWidth * newHeight];
		//tex.Reinitialize(newWidth, newHeight);
		//var newBytes = tex.GetRawTextureData<byte>();		


		//print(morePixelWidth);
		//print(morePixelHeight);
		//print(hW);
		//print(hH);
		//print(newWidth);
		//print(newHeight);
		//print(tex.width);
		//print(tex.height);
		
		//for (int y = 0; y < newHeight; y++)
		//{
		//	for (int x = 0; x < newWidth; x++)
		//	{
		//		if( 
		//			y < hH || y > (hH+tex.height) ||
		//			x < hW || x > (hW+tex.width)
		//		){ 
		//			if (tex.format == TextureFormat.RGB24)
		//			{
		//				int s = 3 * x*y;
		//				newBytes[s] = 255;
		//				newBytes[s + 1] = 0;
		//				newBytes[s + 2] = 0;
		//			}
		//			else if (tex.format == TextureFormat.RGBA32)
		//			{
		//				int s = 4 * y*y;
		//				newBytes[s] = 255;
		//				newBytes[s + 1] = 0;
		//				newBytes[s + 2] = 0;
		//				newBytes[s + 3] = 0;
		//			}
		//			else{
						
		//				int s = 3 * x*y;
		//				newBytes[s] = 255;
		//				newBytes[s + 1] = 0;
		//				newBytes[s + 2] = 0;
		//			}
					
		//		}
		//	}
		//}
		//tex.Apply();
	}
	
	/*
	public void SizeToParent(RawImage image, float padding = 0) {
		
		var parent = image.transform.parent.GetComponent<RectTransform>();
		var imageTransform = image.GetComponent<RectTransform>();
		if (!parent) { return; } //if we don't have a parent, just return our current width;
		padding = 1 - padding;
		float w = 0, h = 0;
		float ratio = image.texture.width / (float)image.texture.height;
		var bounds = new Rect(0, 0, parent.rect.width, parent.rect.height);
		if (Mathf.RoundToInt(imageTransform.eulerAngles.z) % 180 == 90) {
		//Invert the bounds if the image is rotated
		bounds.size = new Vector2(bounds.height, bounds.width);
		}
		//Size by height first
		h = bounds.height * padding;
		w = h * ratio;
		if (w > bounds.width * padding) { //If it doesn't fit, fallback to width;
		w = bounds.width * padding;
		h = w / ratio;
		}
		imageTransform.sizeDelta = new Vector2(w, h);
	}
	*/
}