using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using neoludic.QuickHyphenation;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using TMPro;

public class TextOptions : MonoBehaviour
{
    //public FontWeight fontWeight = FontWeight.Regular;

    // Language
    public bool useLanguage = true;
    public string languageTextDefinition = "";

    // Links
    public bool useLinks = false;

    // FontScaler
    private bool useFontScaler = false;
    public float minFontSize = 24f;
    public float maxFontSize = 36f;
    public float editorTestHeight = 3.0f;
    private float maxInches = 7.76f; // iPad Air (10.35f for iPad Pro)
    private float minInches = 2.91f; // iPhone 4

    // Options
    public bool toUpper = false;

    // Labels
    [HideInInspector]
    public TextMeshProUGUI myTextMeshProLabel;
	public HyphenationAsset hyphenationAsset;
	[HideInInspector]
    public Text myUnityLabel;

    public bool shouldScaleFont = true;
    public bool limitFontScale = false;
    public float maxFontScale = 2.0f;
    private float defaultFontSize = -1;
    private float textScale = 1.0f;

    private string pressedLink = "";

    void Awake()
    {
        if (GetComponent<TextMeshProUGUI>() != null)
        {

            myTextMeshProLabel = GetComponent<TextMeshProUGUI>();
            UpdateTextScale();
        }

        if (GetComponent<Text>() != null) { myUnityLabel = GetComponent<Text>(); }
    }

    private void Start()
    {
        if (useLanguage) TranslateText();
        if (useFontScaler) scaleFontSize();
    }

    private void LateUpdate()
    {
        if (useLinks) CheckLinkHit();
    }

    void OnEnable()
    {
        UpdateTextScale();
    }

    public void UpdateTextScale()
    {
        if (!shouldScaleFont) return;

        if (TextController.instance != null && TextController.instance.shouldScaleFont)
        {
            if (PlayerPrefs.HasKey("FontScale")) { UpdateTextScale(PlayerPrefs.GetFloat("FontScale", 1.0f)); }
        }
    }

    public void UpdateTextScale(float targetScale)
    {
        if (!shouldScaleFont) return;

        if (TextController.instance != null && TextController.instance.shouldScaleFont)
        {
            if (myTextMeshProLabel != null)
            {
                if (defaultFontSize == -1) { defaultFontSize = myTextMeshProLabel.fontSizeMax; }

                if (defaultFontSize > 0)
                {
                    myTextMeshProLabel.fontSize = defaultFontSize * targetScale;
                    myTextMeshProLabel.fontSizeMax = defaultFontSize * targetScale;
                    if (limitFontScale) { myTextMeshProLabel.fontSizeMax = defaultFontSize * Mathf.Clamp(targetScale, 0, maxFontScale); }
                }
            }
        }
    }


	public void TranslateText()
    {
        if (!useLanguage) return;

        if (myTextMeshProLabel != null)
        {
            if (languageTextDefinition == "") languageTextDefinition = myTextMeshProLabel.text;
			//languageTextDefinition = languageTextDefinition.Replace("\\n", "\n");

			if ( toUpper ) { myTextMeshProLabel.text = LanguageController.GetTranslation( languageTextDefinition ).ToUpper(); }
			else
			{
				myTextMeshProLabel.text = LanguageController.GetTranslation( languageTextDefinition );
				if ( myTextMeshProLabel.text == "Okay, Verstanden" ) { myTextMeshProLabel.text = "Okay, verstanden"; }
			}

			string currentText = myTextMeshProLabel.text;
			try
			{
				if ( hyphenationAsset != null )
				{
					// We need to hyphenate per lines, because for some reason there is an error in the Hyphenation plugin if we are using long texts
					string[] lines = currentText.Split( new[] { "\r\n", "\n", "\\n" }, StringSplitOptions.None );
					myTextMeshProLabel.text = "";
					foreach ( string line in lines )
					{
						string text = line.Hyphenate();
						myTextMeshProLabel.text += text + "\n";
					}

					//string text = hyphenationAsset.HyphenateText( myTextMeshProLabel.text, null );
					//string text = myTextMeshProLabel.text.Hyphenate();
					//myTextMeshProLabel.text = text;
					//myTextMeshProLabel.text = text.Replace( "\\n", "\n" );

				}
			}
			catch (Exception e)
			{
				Debug.LogError( "Could not Hyphenate text" );
				myTextMeshProLabel.text = currentText;
			}
		}
        if (myUnityLabel != null)
        {
            if (languageTextDefinition == "") languageTextDefinition = myUnityLabel.text;

            if (toUpper) myUnityLabel.text = LanguageController.GetTranslation(languageTextDefinition).ToUpper();
            else myUnityLabel.text = LanguageController.GetTranslation(languageTextDefinition);
        }

		
    }

    // use link, <link="https://unity3d.com/legal/privacy-policy"><color=#0000ff><u>https://unity3d.com/legal/privacy-policy</u></color></link>
    private void CheckLinkHit()
    {
		if ( myTextMeshProLabel != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
				if ( AdminUIController.instance != null && AdminUIController.instance.adminPasswordMenu.activeInHierarchy ) return;
				else if ( InfoController.instance != null && InfoController.instance.messageDialog.activeInHierarchy ) return;
				else if ( InfoController.instance != null && InfoController.instance.commitAbortDialog.activeInHierarchy ) return;

				int wordIndex = TMP_TextUtilities.FindIntersectingLink(myTextMeshProLabel, Input.mousePosition, null);
                if (wordIndex >= 0)
                {
                    if (!IsVisible()) return;

                    TMP_LinkInfo linkInfo = myTextMeshProLabel.textInfo.linkInfo[wordIndex];
                    pressedLink = linkInfo.GetLinkID();
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                int wordIndex = TMP_TextUtilities.FindIntersectingLink(myTextMeshProLabel, Input.mousePosition, null);
                if (wordIndex >= 0)
                {
                    if (!IsVisible()) return;

                    TMP_LinkInfo linkInfo = myTextMeshProLabel.textInfo.linkInfo[wordIndex];

                    if (linkInfo.GetLinkID() == pressedLink)
                    {
                        print("pressedLink " + pressedLink);

                        if (pressedLink == "Privacy")
                        {
                            FirebaseController.instance.ShowPrivacySite();
                        }
                        else if (pressedLink == "PrivacySettings")
                        {
                            StartCoroutine(ActivateCoroutine(FirebaseController.instance.privacySettingsBanner));
                            //FirebaseController.instance.privacySettingsBanner.SetActive(true);
                        }
                        else if (pressedLink.Contains("mailto"))
                        {
                            Application.OpenURL(pressedLink);
                        }
                        else
                        {
                            ToolsController.instance.OpenWebView(pressedLink);
                        }
                    }
                    pressedLink = "";
                }
            }
        }
    }

    public IEnumerator ActivateCoroutine(GameObject obj)
    {
        yield return null;
        obj.SetActive(true);
    }

    private bool IsVisible()
    {

        Transform myParent = transform.parent;
        while (myParent != null)
        {

            Canvas canvas = myParent.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                if (!canvas.enabled)
                {
                    return false;
                }
                else
                {
                    myParent = canvas.transform.parent;
                }
            }
            else
            {
                break;
            }
        }

        return true;
    }

    private void scaleFontSize()
    {
        float screenHeightInches = minInches;
#if UNITY_EDITOR
        screenHeightInches = Mathf.Clamp(editorTestHeight, minInches, maxInches);
#else
        if (Screen.dpi > 0)
        {
            screenHeightInches = (float)Screen.height / Screen.dpi;
            screenHeightInches = Mathf.Clamp(screenHeightInches, minInches, maxInches);
        }
        else
        {
            return;
        }
#endif
        float targetFontSize = minFontSize + (1 - (screenHeightInches - minInches) / (maxInches - minInches)) * (maxFontSize - minFontSize);

        if (myTextMeshProLabel != null)
        {
            myTextMeshProLabel.fontSize = Mathf.Clamp(targetFontSize, minFontSize, maxFontSize);
        }
        else if (myUnityLabel != null)
        {
            myUnityLabel.fontSize = (int)Mathf.Clamp(targetFontSize, minFontSize, maxFontSize);
        }

    }

}

#if UNITY_EDITOR

[CustomEditor(typeof(TextOptions))]
public class TextOptionsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TextOptions textOptions = (TextOptions)target;

        EditorGUILayout.Space();

		EditorGUILayout.PropertyField( serializedObject.FindProperty( "hyphenationAsset" ) );
		EditorGUILayout.Space();

		//EditorGUILayout.Space();
		//EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		//EditorGUILayout.Space();

		/*
	    EditorGUILayout.PropertyField(serializedObject.FindProperty("fontWeight"));
        switch ( textOptions.fontWeight ){
            case FontWeight.Black : textOptions.fontWeight = FontWeight.Black; break;
            case FontWeight.Bold : textOptions.fontWeight = FontWeight.Bold; break;
            case FontWeight.ExtraLight : textOptions.fontWeight = FontWeight.ExtraLight; break;
            case FontWeight.Heavy : textOptions.fontWeight = FontWeight.Heavy; break;
            case FontWeight.Light : textOptions.fontWeight = FontWeight.Light; break;
            case FontWeight.Medium : textOptions.fontWeight = FontWeight.Medium; break;
            case FontWeight.Regular : textOptions.fontWeight = FontWeight.Regular; break;
            case FontWeight.SemiBold : textOptions.fontWeight = FontWeight.SemiBold; break;
            case FontWeight.Thin : textOptions.fontWeight = FontWeight.Thin; break;
            default : textOptions.fontWeight = FontWeight.Regular; break;
        }
        if( textOptions.myTextMeshProLabel != null ){
            //textOptions.myTextMeshProLabel.fontWeight = textOptions.fontWeight;
        }else if( textOptions.GetComponent<TextMeshProUGUI>() != null ){
            //textOptions.GetComponent<TextMeshProUGUI>().fontWeight = textOptions.fontWeight;
        }
        serializedObject.ApplyModifiedProperties();
	    */

		EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Set a language definition text.", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("The LanguageHandler will search und set the translation", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        //textOptions.useLanguage = EditorGUILayout.Toggle("Use Language", textOptions.useLanguage);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useLanguage"));
        EditorGUILayout.Space();

        if (textOptions.useLanguage)
        {
            //textOptions.languageTextDefinition = EditorGUILayout.TextField("Language Text Definition", textOptions.languageTextDefinition);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("languageTextDefinition"));
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Enable to check URL link clicks and open them in browser.", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Use \"<link=\"URL\">...</link>\". Only with TextMeshProUGUI", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        //textOptions.useLinks = EditorGUILayout.Toggle("Use Links", textOptions.useLinks);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useLinks"));

        /*
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scale the font depending on the physical device size", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        //textOptions.useFontScaler = EditorGUILayout.Toggle("Use FontScaler", textOptions.useFontScaler);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useFontScaler"));

        if (textOptions.useFontScaler)
        {
            //textOptions.minFontSize = EditorGUILayout.FloatField("Min FontSize", textOptions.minFontSize);
            //textOptions.maxFontSize = EditorGUILayout.FloatField("Max FontSize", textOptions.maxFontSize);
            //textOptions.editorTestHeight = EditorGUILayout.FloatField("EditorScreen TestHeight", textOptions.editorTestHeight);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minFontSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxFontSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("editorTestHeight"));
            serializedObject.ApplyModifiedProperties();
        }
        */

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Additional options", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("toUpper"));
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Should scale font", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shouldScaleFont"));
        serializedObject.ApplyModifiedProperties();

        if (textOptions.shouldScaleFont)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("limitFontScale"));
            serializedObject.ApplyModifiedProperties();

            if (textOptions.limitFontScale)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxFontScale"));
                serializedObject.ApplyModifiedProperties();
            }
        }

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

}

#endif
