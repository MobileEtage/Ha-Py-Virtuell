
using System.Collections;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VideoTarget : MonoBehaviour {

	public enum VideoType { URL, VideoClip };
	public VideoType videoType;
	
	public string videoURL = "";
    public bool useFilePath = false;
    public bool useNewTimerUI = false;
    public bool hideNavigation = true;
    public bool hasShadow = false;

    public bool isLoaded = false;
    public bool isFullscreen = false;
    public bool fullScreenOverrideSorting = false;

    public VideoClip videoClip;
    public GameObject videoMask;
    public Canvas videoCanvas;
    public GameObject videoContainer;
	public GameObject videoContainerBackground;
	public RawImage targetImage;

	public GameObject videoNavigationHeader;
	public GameObject videoNavigationFooter;
	public Slider timerSlider;
	public TextMeshProUGUI currentTimeLabel;
	public TextMeshProUGUI leftTimeLabel;
	public GameObject playButton;
	public GameObject pauseButton;
	public GameObject switchToFullscreenButton;
	public GameObject disableFullscreenButton;

	public ScrollRect parentScrollRect;

	private Vector2 videoContainerStartPosition;
	private Vector2 videoContainerStartSize;
	
	private bool videoContainerIsActive = true;
	
	void Awake(){
		
		StartCoroutine( InitStartValuesCoroutine() );
	}

	IEnumerator InitStartValuesCoroutine(){

		yield return new WaitForEndOfFrame();
		videoContainerStartPosition = videoContainer.GetComponent<RectTransform>().anchoredPosition;
		videoContainerStartSize = new Vector2( videoContainer.GetComponent<RectTransform>().rect.width, videoContainer.GetComponent<RectTransform>().rect.height );
	}
	
	public Vector2 GetVideoContainerStartPosition(){
		return videoContainerStartPosition;
	}
	
	public Vector2 GetVideoContainerStartSize(){
		return videoContainerStartSize;
	}
	
	public void SetVideoContainerIsActive( bool videoContainerIsActive ){
		this.videoContainerIsActive = videoContainerIsActive;
	}
	
	public bool GetVideoContainerIsActive(){
		return videoContainerIsActive;
	}
	
	public void Reset(){
		
		print("Reset VideoTarget");
		
		targetImage.gameObject.SetActive(false);
		timerSlider.value = 0;
		currentTimeLabel.text = "0:00";
		leftTimeLabel.text = "0:00";
		pauseButton.SetActive(false);
		playButton.SetActive(true);
		videoNavigationHeader.gameObject.SetActive(false);
		videoNavigationHeader.GetComponent<CanvasGroup>().alpha = 0;
		videoNavigationFooter.gameObject.SetActive(false);
		videoNavigationFooter.GetComponent<CanvasGroup>().alpha = 0;
		isLoaded = false;
	}
}

#if UNITY_EDITOR

[CustomEditor(typeof(VideoTarget))]
public class VideoTargetEditor : Editor
{
	public override void OnInspectorGUI()
	{
		VideoTarget videoTarget = (VideoTarget)target;

		EditorGUILayout.PropertyField(serializedObject.FindProperty("videoType"));
		
		if (videoTarget.videoType == VideoTarget.VideoType.URL)
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("videoURL"));
			serializedObject.ApplyModifiedProperties();
		}else{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("videoClip"));
			serializedObject.ApplyModifiedProperties();
		}
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useFilePath"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useNewTimerUI"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fullScreenOverrideSorting"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("hideNavigation"));

		EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("videoMask"));
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("videoCanvas"));
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("videoContainer"));
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("videoContainerBackground"));
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("targetImage"));
		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.Space();

		EditorGUILayout.PropertyField(serializedObject.FindProperty("videoNavigationHeader"));
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("videoNavigationFooter"));
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("timerSlider"));
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("currentTimeLabel"));
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("leftTimeLabel"));
		serializedObject.ApplyModifiedProperties();
		
		EditorGUILayout.PropertyField(serializedObject.FindProperty("playButton"));
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("pauseButton"));
		serializedObject.ApplyModifiedProperties();
		
		EditorGUILayout.PropertyField(serializedObject.FindProperty("switchToFullscreenButton"));
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("disableFullscreenButton"));
		serializedObject.ApplyModifiedProperties();
		
		EditorGUILayout.Space();

		EditorGUILayout.PropertyField(serializedObject.FindProperty("parentScrollRect"));
		serializedObject.ApplyModifiedProperties();
	}

}

#endif