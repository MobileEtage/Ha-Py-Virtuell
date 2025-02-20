using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioIcon : MonoBehaviour
{
	public Transform imageHolder;
	public AnalyzeAudio audioData;
	
	[Space(10)]
	
	public float width = 20;
	public float height = 100;
	public float sensitivity = 1;
	public float lerpSpeed = 1;
	public bool useBuffer = true;
	public enum ScaleDriver
	{
		Amplitude,
		AudioBand
	}
	public ScaleDriver scaleDriver;
	
	//private List<Transform> images = new List<Transform>();
	private List<RectTransform> images = new List<RectTransform>();
	private float drivenValue = 0;
	private float amplitudeValue = 0;

	void Awake()
    {
	    foreach( Transform child in imageHolder ){
	    	images.Add( child.GetComponent<RectTransform>() );
	    }  
	    
	    DisablePlayback();
    }

    void Update()
    {
	    //if( audioData.audioSource == null || audioData.audioSource.clip == null ){
		if( audioData.audioSource == null ){
	    	
	    	DisablePlayback();
	    }
	    else{
	    	
	    	UpdateScale();
	    }
    }
    
	public void DisablePlayback(){
		
		for( int i = 0; i < images.Count; i++ ){
			
			images[i].sizeDelta = new Vector2(0,0);
		}
	}
	
	void UpdateScale ()
	{
		for( int i = 0; i < images.Count; i++ ){
			
			if(scaleDriver == ScaleDriver.AudioBand)
			{
				drivenValue = useBuffer ? audioData.AudioBandBuffer[i] * sensitivity : audioData.AudioBand[i] * sensitivity;
			}
			else if(scaleDriver == ScaleDriver.Amplitude)
			{
				drivenValue = useBuffer ? audioData.AmplitudeBuffer * sensitivity : audioData.Amplitude * sensitivity;
			}
	
			//transform.localScale = 
			//	new Vector3(Axis.x * drivenValue, Axis.y * drivenValue, Axis.z * drivenValue) + startScale;
			
			if( drivenValue >= 0 ){
				amplitudeValue = Mathf.Lerp(amplitudeValue, drivenValue, Time.deltaTime*lerpSpeed);
				amplitudeValue = Mathf.Clamp(amplitudeValue, 0, height);
				images[i].sizeDelta = new Vector2(width,amplitudeValue);
			}
		
		}
	}
}

