using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class UIVideo : MonoBehaviour
{
	public bool externVideoPlayerObject = false;
	public bool hideBeforeLoaded = false;
	public VideoPlayer videoPlayer;
	public RawImage rawImage;
	public Texture2D previewImage;

	private bool frameIsReady = false;
    private bool videoIsPrepared = false;

    void Awake()
    {
		if ( rawImage == null ) { rawImage = GetComponent<RawImage>(); }
		if ( videoPlayer == null ) { videoPlayer = GetComponent<VideoPlayer>(); }
		if ( videoPlayer != null ) { videoPlayer.sendFrameReadyEvents = true; }
    }

    private void VideoPlayer_frameReady(UnityEngine.Video.VideoPlayer vp, long frame)
    {
        //print("VideoPlayer_frameReady " + frameIsReady);

        if (frameIsReady && rawImage != null) {

			rawImage.texture = vp.texture;
            StartCoroutine("ShowVideoCoroutine");
        }

        frameIsReady = true;
    }

    public IEnumerator ShowVideoCoroutine()
    {
        //print("ShowVideoCoroutine");

        yield return new WaitForEndOfFrame();
		if ( rawImage != null ) { rawImage.enabled = true; }
    }

    private void VideoPlayer_prepareCompleted(VideoPlayer source)
    {
        videoIsPrepared = true;
    }

    private void OnEnable()
    {
		//print("OnEnable");

		if ( videoPlayer == null ) { videoPlayer = GetComponent<VideoPlayer>(); }

		if ( videoPlayer != null )
		{
			videoPlayer.sendFrameReadyEvents = true;
			if ( hideBeforeLoaded ) { rawImage.enabled = false; }

			frameIsReady = false;
			videoPlayer.frameReady += VideoPlayer_frameReady;
			if ( previewImage != null ) { rawImage.texture = previewImage; }

			if ( externVideoPlayerObject )
			{
				videoPlayer.time = 0;
				videoPlayer.Play();
			}
		}
	}

	private void OnDisable()
    {
		if ( videoPlayer != null )
		{
			frameIsReady = false;
			if ( externVideoPlayerObject ) { videoPlayer.Pause(); }
			videoPlayer.frameReady -= VideoPlayer_frameReady;
			if ( previewImage != null ) { rawImage.texture = previewImage; }
		}
    }
}
