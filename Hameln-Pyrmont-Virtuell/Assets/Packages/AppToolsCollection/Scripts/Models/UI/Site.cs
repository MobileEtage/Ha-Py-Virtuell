
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Site : MonoBehaviour {
	
	public string siteID = "";
	public bool isAssetScene = false;
	public bool keepLoaded = true;
	
	public bool isLoaded = false;
	[HideInInspector] public bool isUnloading = false;
	[HideInInspector] public List<GameObject> rootObjects = new List<GameObject>();	
	
	public string previousSite;
	public UnityEvent backAction;
}

#if UNITY_EDITOR
[CustomEditor(typeof(Site), true)]
public class SiteEditor : Editor
{
	public override void OnInspectorGUI()
	{		
		Site siteOptions = (Site)target;

		EditorGUILayout.PropertyField(serializedObject.FindProperty("siteID"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("isAssetScene"));			
		if (siteOptions.isAssetScene ){ EditorGUILayout.PropertyField(serializedObject.FindProperty("keepLoaded"));	}
		
		serializedObject.ApplyModifiedProperties();
	}

}
#endif

