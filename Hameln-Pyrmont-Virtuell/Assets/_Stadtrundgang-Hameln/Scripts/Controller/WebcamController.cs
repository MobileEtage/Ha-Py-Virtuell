using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebcamController : MonoBehaviour
{
    public GameObject webcamContent;
    public Camera webcamCamera;
    public RawImage cameraImage;
    public WebCamDevice webcamDevice;
    public WebCamTexture webcamTexture;

    private Texture2D photo;
    private RenderTexture renderTexture;

    public static WebcamController instance;
    void Awake()
    {
        instance = this;
    }

    public IEnumerator StartWebcamTextureCoroutine()
    {
        yield return StartCoroutine(StartWebcamTextureCoroutine(true));
    }

    public IEnumerator StartWebcamTextureCoroutine(bool useFrontFacingCamera = false)
    {
        if (WebCamTexture.devices.Length <= 0)
        {
            print("No WebCamTexture devices available");
            yield break;
        }

        if (webcamTexture == null)
        {
            webcamDevice = WebCamTexture.devices[0];

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR

			if( webcamDevice.isFrontFacing != useFrontFacingCamera ){
				
				for( int i = 0; i < WebCamTexture.devices.Length; i++ ){
					
					if( 
                        ( useFrontFacingCamera && WebCamTexture.devices[i].isFrontFacing ) ||
                        ( !useFrontFacingCamera && !WebCamTexture.devices[i].isFrontFacing )
                    )
                    {						
                        webcamDevice = WebCamTexture.devices[i];
						break;
					}
				}
			}		
#endif

            print("Using webcamDevice " + webcamDevice.name);


            /************** Get camera resolution **************/

            int targetWidth = 1080;
            int targetHeight = 1920;
            int requestedHeight = 1920;
            int maxHeight = 1920;
            bool useMaxResoultion = false;

            Vector2 heightRange = new Vector2(1920, 2400);
            bool useHeightRange = true;

            float targetRatio = (float)Screen.width / (float)Screen.height;
            if (Screen.width > Screen.height) { targetRatio = (float)Screen.height / (float)Screen.width; }
            targetRatio = 0.75f;

            float bestRatio = -1;
            float minRatioDist = float.MaxValue;

            if (webcamDevice.availableResolutions != null)
            {
                int maxPixel = 0;
                for (int i = 0; i < webcamDevice.availableResolutions.Length; i++)
                {
                    print("availableResolution " + webcamDevice.availableResolutions[i].width + " " + webcamDevice.availableResolutions[i].height);

                    int w = webcamDevice.availableResolutions[i].width;
                    int h = webcamDevice.availableResolutions[i].height;

                    if (w > h)
                    {
                        w = webcamDevice.availableResolutions[i].height;
                        h = webcamDevice.availableResolutions[i].width;
                    }

                    if (useMaxResoultion)
                    {
                        // Find max MP (width*height)
                        int pixel = w * h;
                        if (pixel > maxPixel)
                        {
                            maxPixel = pixel;
                            targetWidth = w;
                            targetHeight = h;
                        }
                    }
                    else
                    {
                        if (useHeightRange)
                        {
                            if (h >= heightRange.x && h <= heightRange.y)
                            {
                                float ratio = (float)w / (float)h;
                                float ratioDist = Mathf.Abs(ratio - targetRatio);
                                if (ratioDist < minRatioDist)
                                {
                                    targetWidth = w;
                                    targetHeight = h;
                                    minRatioDist = ratioDist;
                                    bestRatio = ratio;

                                    print("bestRatio " + bestRatio);
                                }

                                //break;
                            }
                        }
                        else
                        {

                            // Find best resolution depending on requestedHeight
                            if (h >= requestedHeight && h <= maxHeight)
                            {
                                float ratio = (float)w / (float)h;
                                float ratioDist = Mathf.Abs(ratio - targetRatio);
                                if (ratioDist < minRatioDist)
                                {
                                    targetWidth = w;
                                    targetHeight = h;
                                    minRatioDist = ratioDist;
                                    bestRatio = ratio;

                                    print("bestRatio " + bestRatio);
                                }

                                //break;
                            }
                            else if (bestRatio < 0 && h >= requestedHeight)
                            {
                                targetWidth = w;
                                targetHeight = h;
                            }
                        }
                    }
                }
            }

            print("Init WebCamTexture " + targetWidth + " " + targetHeight);
            webcamTexture = new WebCamTexture(webcamDevice.name, targetWidth, targetHeight, 60);
        }


        if (webcamTexture != null)
        {
            webcamContent.SetActive(true);

            if (!webcamTexture.isPlaying)
            {
                print("Starting webcam");
                webcamTexture.Play();
                cameraImage.texture = webcamTexture;

                yield return new WaitForSeconds(1.0f);
            }

            /************** Fix rotation of image, because it's most likely rotated **************/
            int rotAngle = -webcamTexture.videoRotationAngle;
	        print("rotAngle " + rotAngle);
	        cameraImage.transform.localEulerAngles = new Vector3(0, 0, rotAngle);
	        
            /************** Set aspect ratio **************/
            cameraImage.GetComponent<AspectRatioFitter>().aspectRatio = ((float)webcamTexture.width / (float)webcamTexture.height);

            if (rotAngle == 270 || rotAngle == -270 || rotAngle == 90 || rotAngle == -90)
            {
                if (cameraImage.GetComponent<AspectRatioFitter>().aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent)
                {
                    float targetScale = 1 / cameraImage.GetComponent<AspectRatioFitter>().aspectRatio;
                    float screenRatio = (float)Screen.width / (float)Screen.height;

                    print("screenRatio " + screenRatio);
                    print("targetScale " + targetScale);
                    if (targetScale < screenRatio)
                    {
                        print("targetScale < screenRatio, setting targetScale to screenRatio");
                        targetScale = screenRatio;
                    }

                    cameraImage.transform.localScale = Vector3.one * targetScale;
                }
                else if (cameraImage.GetComponent<AspectRatioFitter>().aspectMode == AspectRatioFitter.AspectMode.FitInParent)
                {
                    cameraImage.transform.localScale = Vector3.one * cameraImage.GetComponent<AspectRatioFitter>().aspectRatio;
                }

                // mirror
	            if(useFrontFacingCamera){ cameraImage.transform.localScale = new Vector3(cameraImage.transform.localScale.x, -cameraImage.transform.localScale.y, cameraImage.transform.localScale.z); }
            }

            /************** Rescale if mirrored **************/
            if (webcamTexture.videoVerticallyMirrored)
            {
                cameraImage.transform.localScale = new Vector3(cameraImage.transform.localScale.x, -cameraImage.transform.localScale.y, cameraImage.transform.localScale.z);
                //cameraImage.transform.localScale = new Vector3(cameraImage.transform.localScale.x, cameraImage.transform.localScale.y, cameraImage.transform.localScale.z);
            }
            //else{ cameraImage.transform.localScale = new Vector3(cameraImage.transform.localScale.x, -cameraImage.transform.localScale.y, cameraImage.transform.localScale.z); }

            print("cameraImage ratio " + cameraImage.GetComponent<AspectRatioFitter>().aspectRatio);
            print("cameraImage scale " + cameraImage.transform.localScale);
            print("Webcam resolution " + webcamTexture.width + " " + webcamTexture.height);
            print("webcamTexture.videoRotationAngle " + webcamTexture.videoRotationAngle);
            print("webcamTexture.videoVerticallyMirrored " + webcamTexture.videoVerticallyMirrored);
            print("cameraImage: " + cameraImage.GetComponent<RectTransform>().rect.width + " " + cameraImage.GetComponent<RectTransform>().rect.height);
        }
        else
        {
            print("Could not load webcamTexture");
        }
    }

    public void DisablePhotoCamera()
    {
        webcamContent.SetActive(false);
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
            webcamTexture = null;
        }
    }

    public Texture2D GetScreenshotFromCamera(bool isSquare = false)
    {
        renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        webcamCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        webcamCamera.Render();

        Rect rect = GetContentRectDimensions();
        if (isSquare)
        {
            rect.y = (rect.height - rect.width) * 0.5f + rect.y;
            rect.height = rect.width;
        }

        photo = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, true);
        photo.ReadPixels(new Rect(rect.x, rect.y, (int)rect.width, (int)rect.height), 0, 0);
        photo.Apply();

        webcamCamera.targetTexture = null;
        RenderTexture.active = null; // JC: added to avoid errors
        Destroy(renderTexture);

        Resources.UnloadUnusedAssets();
        GC.Collect();

        return photo;
    }

    // Get the dimensions of the content holder, because we might have safe area borders, 
    // so we can not use Screen.height for the photo resolution
    private Rect GetContentRectDimensions()
    {

        Rect rect = new Rect(0, 0, Screen.width, Screen.height);

        if (!CanvasController.instance.useSafeAreaBorders) return rect;

        float contentHeight = CanvasController.instance.GetContentHeight();
        float canvasHeight = CanvasController.instance.GetCanvasHeight();

        float percentage = contentHeight / canvasHeight;

        if (percentage < 1)
        {

            float screenAdjustedHeight = percentage * Screen.height;
            float startY = 0;

            if (CanvasController.instance.useSafeAreaBorderBottom)
            {
                startY = CanvasController.instance.GetBorderBottomPixelHeight();
            }

            rect.y = (int)startY;
            rect.height = (int)screenAdjustedHeight;

            if (rect.y + rect.height > Screen.height)
            {
                rect.height = Screen.height - rect.y;
            }
        }

        return rect;
    }
}
