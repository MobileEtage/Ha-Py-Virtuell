using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class ConfigureSite : MonoBehaviour
{
	public string id = "";
	public enum ConfigureType{ Input, List };
	public ConfigureType configureType;
	public bool valueEntered = false;
	public string currentDisplayValue = "";
	
	[Space(10)]
	public TextMeshProUGUI inputValue;
	public GameObject descriptionImage;
	public GameObject descriptionElements;
	public GameObject descriptionLabel1;
	public GameObject descriptionLabel2;
	public GameObject inputElement;
	
	[Space(10)]
	public GameObject mainContent;
	public GameObject elementsHolder;
	public GameObject elementPrefab;
	
	public void Reset(){
		
		if( configureType == ConfigureType.List ){		
			foreach( Transform child in elementsHolder.transform ){			
				Destroy( child.gameObject );
			}
			mainContent.GetComponent<CanvasGroup>().alpha = 1;
		}
		else{
			
			if( id == "height" ){ 
				GetComponentInChildren<TMP_InputField>(true).text = "";
				GetComponentInChildren<TMP_InputField>(true).placeholder.GetComponentInChildren<TextMeshProUGUI>().text = "z. B. 300";
			}
			else{
				inputElement.GetComponentInChildren<TextMeshProUGUI>(true).text = "Bitte auswählen";
			}
			
			descriptionImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,0);
			descriptionElements.GetComponent<CanvasGroup>().alpha = 1;
		}
		valueEntered = false;
	}
}

#if UNITY_EDITOR

[CustomEditor(typeof(ConfigureSite))]
public class ConfigureSiteEditor : Editor
{
	public override void OnInspectorGUI()
	{
		ConfigureSite configureSite = (ConfigureSite)target;

		//EditorGUILayout.Space();
		//EditorGUILayout.Space();
		//EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		
		EditorGUILayout.Space();
		EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("configureType"));
		
		if( configureSite.configureType == ConfigureSite.ConfigureType.Input ){
			EditorGUILayout.PropertyField(serializedObject.FindProperty("inputValue"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("descriptionImage"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("descriptionElements"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("descriptionLabel1"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("descriptionLabel2"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("inputElement"));
		}else{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("mainContent"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("elementsHolder"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("elementPrefab"));
		}

		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.Space();
		EditorGUILayout.Space();
	}

}

#endif
