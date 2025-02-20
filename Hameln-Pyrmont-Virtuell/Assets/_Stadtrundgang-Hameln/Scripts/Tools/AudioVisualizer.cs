using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioVisualizer : MonoBehaviour
{
    public Camera myCamera;
    public RawImage myImage;
    public GameObject myImageRoot;
    public RhythmVisualizatorPro.RhythmVisualizatorPro rhythmVisualizatorPro;

    public bool initialized = false;

    public static AudioVisualizer instance;
    void Awake()
    {
        instance = this;
    }

    public void SetSensibility(float bassSensibility, float trebleSensibility, float soundBarsHeight = 1)
    {
        rhythmVisualizatorPro.bassSensibility = bassSensibility;
        rhythmVisualizatorPro.trebleSensibility = trebleSensibility;
        rhythmVisualizatorPro.soundBarsTransform.localScale = new Vector3(1, soundBarsHeight, 1);
    }

    public void ResetSensibility()
    {
        rhythmVisualizatorPro.bassSensibility = 60;
        rhythmVisualizatorPro.trebleSensibility = 120;
        rhythmVisualizatorPro.soundBarsTransform.localScale = new Vector3(1, 1, 1);
    }

    public void EnableAudioVisualizer(AudioSource audioSource)
    {
        SetRenderTexture();
        SetAudioSource(audioSource);
        rhythmVisualizatorPro.gameObject.SetActive(true);
        myImageRoot.gameObject.SetActive(true);
    }

    public void DisableAudioVisualizer()
    {
        myImageRoot.gameObject.SetActive(false);
        rhythmVisualizatorPro.gameObject.SetActive(false);
    }

    public void SetCameraPositionY(float y)
    {
        myCamera.transform.localPosition = new Vector3(myCamera.transform.localPosition.x, y, myCamera.transform.localPosition.z);
    }

    public void SetRenderTexture()
    {
        if (initialized) return;
        initialized = true;

        int width = CanvasController.instance.GetContentPixelWidth();
        int height = CanvasController.instance.GetContentPixelHeight();
        int w = Mathf.Min(width, height);
        int h = Mathf.Max(width, height);

        //float ratio = VideoController.instance.GetVideoRatio();
        //h = (int)(w / ratio);

        //RenderTexture rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32);

        RenderTexture rt = new RenderTexture(w, h, 16, RenderTextureFormat.RGB565);
        //RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        //RenderTexture rt = new RenderTexture(w, h, 16, RenderTextureFormat.RGB111110Float);
        //RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);

        rt.Create();
        myImage.texture = rt;
        myImage.material.mainTexture = rt;
        myCamera.targetTexture = rt;
        myCamera.Render();

    }

    public void SetAudioSource(AudioSource audioSource)
    {
        rhythmVisualizatorPro.audioSource = audioSource;
    }
}
