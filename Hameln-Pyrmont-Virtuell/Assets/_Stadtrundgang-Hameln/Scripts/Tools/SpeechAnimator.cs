using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SpeechAnimator : MonoBehaviour
{
	public bool shouldRecord = false;
	public bool shouldPlay = false;
	public SpeechAnimation speechAnimation;
	private float lerpFactor = 10.0f;

	[Space(10)]

	public TextAsset speechRecording;
	public AudioSource audioSource;
	public SkinnedMeshRenderer skinnedMeshRenderer;

	private int currentFrame = 0;
	private int ratePerSecond = 25;
	private float intervall = 1;
	private float timer = 0f;

	void Start()
    {
		//if(speechRecording != null ){ speechAnimation = JsonUtility.FromJson<SpeechAnimation>( speechRecording.text ); }	
	}

	void LateUpdate()
    {
		if( shouldPlay && !shouldRecord && speechAnimation != null && audioSource.isPlaying) { Play(); }

#if UNITY_EDITOR

		if ( shouldRecord )
		{
			Record();
			if ( Input.GetKeyDown( KeyCode.S ) ) { Save(); }
		}
#endif

	}

	public void Init(GameObject avatar, string id)
	{

#if UNITY_EDITOR
		if ( shouldRecord ) return;
#endif
		speechAnimation = null;

		string speechFile = "SpeechRecordings/speechAnimation_" + id;
		speechRecording = Resources.Load<TextAsset>( speechFile );
		if ( speechRecording != null ) { speechAnimation = JsonUtility.FromJson<SpeechAnimation>( speechRecording.text ); }

		string audioFile = "speech_" + id;
		AudioClip clip = Resources.Load<AudioClip>( audioFile );
		if ( clip != null ) { audioSource.clip = clip; }

		skinnedMeshRenderer = avatar.GetComponentInChildren<SkinnedMeshRenderer>();
		shouldPlay = true;
	}

	public void Play()
	{
		float time = audioSource.time;
		for (int i = 0; i < speechAnimation.speechFrames.Count; i++ )
		{
			if( time > speechAnimation.speechFrames[i].time )
			{
				if( (i+1) < speechAnimation.speechFrames.Count && time < speechAnimation.speechFrames[i+1].time )
				{
					//LerpBlendShapeValues( speechAnimation.speechFrames[i].blendShapeValues );

					if ( currentFrame == i ) break;
					currentFrame = i;
					SetBlendShapeValues(speechAnimation.speechFrames[i].blendShapeValues);
					break;
				}
			}
		}
	}

	public void SetBlendShapeValues(List<float> values)
	{
		for ( int i = 0; i < speechAnimation.blendShapeNames.Count; i++ )
		{
			int index = GetBlendShapeIndex( skinnedMeshRenderer, speechAnimation.blendShapeNames[i] );
			if ( index < 0 ) { Debug.LogError( speechAnimation.blendShapeNames[i] + " not found" ); continue; }
			if ( i >= values.Count ) { Debug.LogError( "More values than blendshapes" ); continue; }
			skinnedMeshRenderer.SetBlendShapeWeight(index, values[i]);
		}
	}

	public void LerpBlendShapeValues(List<float> values)
	{
		for ( int i = 0; i < speechAnimation.blendShapeNames.Count; i++ )
		{
			int index = GetBlendShapeIndex( skinnedMeshRenderer, speechAnimation.blendShapeNames[i] );
			if ( index < 0 ) { Debug.LogError( speechAnimation.blendShapeNames[i] + " not found" ); continue; }
			float val = skinnedMeshRenderer.GetBlendShapeWeight( index );
			float targetVal = Mathf.Lerp(val, values[i], Time.deltaTime* lerpFactor );
			skinnedMeshRenderer.SetBlendShapeWeight( index, targetVal );
		}
	}

	public void Record()
	{
		if ( !audioSource.isPlaying ) return;

		float deltaTime = Time.deltaTime;
		timer += deltaTime;

		intervall = 1f / ratePerSecond;
		if ( intervall > 0 )
		{
			while ( timer >= intervall )
			{
				timer -= intervall;
				RecordFrame();
			}
		}
	}

	public void RecordFrame()
	{
		if( speechAnimation == null ) { return; }

		SpeechFrame speechFrame = new SpeechFrame();
		speechFrame.time = audioSource.time;
		for ( int i = 0; i < speechAnimation.blendShapeNames.Count; i++ )
		{
			int index = GetBlendShapeIndex(skinnedMeshRenderer, speechAnimation.blendShapeNames[i]);
			if ( index < 0 ) { Debug.LogError( speechAnimation.blendShapeNames[i] + " not found" ); continue; }

			float val = skinnedMeshRenderer.GetBlendShapeWeight(index);
			speechFrame.blendShapeValues.Add(val);
		}
		speechAnimation.speechFrames.Add( speechFrame );
	}

	public void Save()
	{
		string json = JsonUtility.ToJson( speechAnimation );
		string path = Application.dataPath + "/Resources/SpeechRecordings";
		string file = path + "/speechAnimation_" + audioSource.clip.name + ".json";
		if ( !Directory.Exists( path ) ) { Directory.CreateDirectory(path); }
		File.WriteAllText(file, json);
	}

	public int GetBlendShapeIndex(SkinnedMeshRenderer skinnedMeshRenderer, string blendShapeName)
	{
		if ( skinnedMeshRenderer == null ){ return -1; }
		if ( skinnedMeshRenderer.sharedMesh == null ){ return -1; }

		for ( int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++ )
		{
			if ( skinnedMeshRenderer.sharedMesh.GetBlendShapeName( i ) == blendShapeName ){ return i; }
		}

		for ( int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++ )
		{
			if ( skinnedMeshRenderer.sharedMesh.GetBlendShapeName( i ).Contains( blendShapeName ) ){ return i; }
		}
		return -1;
	}
}

[Serializable]
public class SpeechAnimation
{
	// https://developer.oculus.com/documentation/unity/audio-ovrlipsync-viseme-reference/
	public List<string> blendShapeNames = new List<string>();
	[SerializeField] public List<SpeechFrame> speechFrames = new List<SpeechFrame>();
}

[Serializable]
public class SpeechFrame
{
	public float time = 0;
	public List<float> blendShapeValues = new List<float>();
}
