using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoTrackable : MonoBehaviour
{
	public VideoPlayer videoPlayer;
	public VideoClip videoClip;
	public ImageTarget imageTraget;
	public RawImage videoImage;

	[Space(10)]
	
	public bool playbackEnabled = false;
	public bool initialized = false;
	
	private bool frameIsReady = false;
	private bool videoIsPrepared = false;
	private bool isLoadingVideo = false;
	private string videoURL = "";
	
    void Start()
	{
		videoURL = System.IO.Path.Combine (Application.streamingAssetsPath,"Baum-spricht_2.webm"); 
		
	    if( imageTraget == null ){
	    	imageTraget = GetComponent<ImageTarget>();
	    }
	    
	    videoPlayer.sendFrameReadyEvents = true;
	    videoPlayer.loopPointReached += VideoPlayer_loopPointReached;
	    videoImage.enabled = false;
	    
	    #if UNITY_EDITOR || UNITY_WEBGL
	    videoImage.transform.parent.position = new Vector3(0,3.7f,-5.25f);
	    videoImage.transform.parent.localEulerAngles = Vector3.zero;
	    #endif
    }

    void Update()
    {
	    if( imageTraget == null ) return;
	    if( videoClip == null ) return;
	    
	    if( !playbackEnabled ){
	    	
	    	if( videoPlayer.isPlaying )
			{
	    		//videoPlayer.Stop();
				videoPlayer.Pause();
			}
	    	return;
	    }
	    
	    if( imageTraget.isTracking ){
	    	
	    	if( !videoPlayer.isPlaying ){
	    		
	    		PlayVideo();	    		
	    	}
	    	else{
	    		
	    		if( videoImage.texture == null && videoPlayer.texture != null ){
		    		videoImage.texture = videoPlayer.texture;
	    		}
	    	}
	    }
	    else{
	    	
	    	if( videoPlayer.isPlaying ){
	    		
	    		PauseVideo();
	    	}
	    }
    }
    
	private void VideoPlayer_frameReady(UnityEngine.Video.VideoPlayer vp, long frame)
	{
		frameIsReady = true;
	}
	
	private void VideoPlayer_prepareCompleted(VideoPlayer source)
	{
		videoIsPrepared = true;
	}  

	private void VideoPlayer_loopPointReached(VideoPlayer source)
	{
		playbackEnabled = false;
		videoPlayer.clip = null;
		videoImage.enabled = false;
	}
	
	
	public void PauseVideo(){
		
		#if UNITY_WEBGL
		if( videoPlayer.url != videoURL ) return;
		#else
		if( videoPlayer.clip != videoClip ) return;
		#endif
		videoPlayer.Pause();
	}
	
	public void PlayVideo(){
		
		#if UNITY_WEBGL
		if( videoPlayer.url != videoURL ){;
		#else
		if( videoPlayer.clip != videoClip ){
		#endif
			if( isLoadingVideo ) return;
			isLoadingVideo = true;
			StartCoroutine( PlayVideoCoroutine() );
		}
		else{
			
			if( videoPlayer.isPaused ){
				videoPlayer.Play();
			}else{
				videoPlayer.Play();
				
			}
			
			if( videoImage.texture == null && videoPlayer.texture != null ){
				videoImage.texture = videoPlayer.texture;
			}
			
			if( !isLoadingVideo ){
				videoImage.enabled = true;
			}
		}
	}
	
	IEnumerator PlayVideoCoroutine(){
				
		if (videoPlayer.isPlaying)
		{			
			//videoPlayer.Stop ();
			videoPlayer.Pause();
		}

#if UNITY_WEBGL
		videoPlayer.source = VideoSource.Url;
		videoPlayer.url = System.IO.Path.Combine (Application.streamingAssetsPath,"Baum-spricht_2.webm"); 
#else
		videoPlayer.source = VideoSource.VideoClip;
		videoPlayer.clip = videoClip;
#endif

		videoIsPrepared = false;
		videoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;
		videoPlayer.Prepare();

		// Wait until video is prepared
		float timer = 2;
		while (!videoIsPrepared && timer > 0) {
			yield return null;
			timer -= Time.deltaTime;
		}
		videoPlayer.prepareCompleted -= VideoPlayer_prepareCompleted;

		frameIsReady = false;
		videoPlayer.frameReady += VideoPlayer_frameReady;
		videoPlayer.Play ();

		// Wait for the first video frame, then show video image
		timer = 2;
		while (!frameIsReady && timer > 0) {
			yield return null;
			timer -= Time.deltaTime;
		}
		videoPlayer.frameReady -= VideoPlayer_frameReady;
		
		videoImage.enabled = true;
		videoImage.texture = videoPlayer.texture;
		
		isLoadingVideo = false;
	}


}
