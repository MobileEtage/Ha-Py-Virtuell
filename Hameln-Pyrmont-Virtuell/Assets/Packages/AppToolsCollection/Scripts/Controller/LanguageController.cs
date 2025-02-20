
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

using TMPro;

using RestSharp.Contrib;

public static class LanguageController
{
    public static string cancelCurrentStationText = "Hast Du die Station\nschon ganz erkundet?\nMöchtest Du zurück zur Karte?";

    public static string move_desc = "<b>Verschieben:</b>\nObjekt antippen und ziehen.";
    public static string move_rotate_desc = "<b>Verschieben:</b>\nObjekt antippen und ziehen.<line-height=150%>\n</line-height><b>Drehen:</b>\nNeben Objekt tippen und ziehen.";
    public static string move_scale_desc = "<b>Verschieben:</b>\nObjekt antippen und ziehen.<line-height=150%>\n</line-height><b>Skalieren:</b>\nMit zwei Fingern.";
    public static string move_rotate_scale_desc = "<b>Verschieben:</b>\nObjekt antippen und ziehen.<line-height=150%>\n</line-height><b>Drehen:</b>\nNeben Objekt tippen und ziehen.<line-height=150%>\n</line-height><b>Skalieren:</b>\nMit zwei Fingern.";

    public static string move_guide_desc = "<b>Verschieben:</b>\nStadtführende antippen und ziehen.";
    public static string move_rotate_guide_desc = "<b>Verschieben:</b>\nStadtführende antippen und ziehen.<line-height=150%>\n</line-height><b>Drehen:</b>\nNeben Stadtführende tippen und ziehen.";
    public static string move_scale_guide_desc = "<b>Verschieben:</b>\nStadtführende antippen und ziehen.<line-height=150%>\n</line-height><b>Skalieren:</b>\nMit zwei Fingern.";
    public static string move_rotate_scale_guide_desc = "<b>Verschieben:</b>\nStadtführende antippen und ziehen.<line-height=150%>\n</line-height><b>Drehen:</b>\nNeben Stadtführende tippen und ziehen.<line-height=150%>\n</line-height><b>Skalieren:</b>\nMit zwei Fingern.";

    public static string hashtag_desc = "<b>Hashtag verschieben:</b>\nObjekt antippen und ziehen.<line-height=150%>\n</line-height><b>Tauben platzieren:</b>\nIn die Umgebung tippen.";
    public static string pigeons_desc = "<b>Tauben platzieren:</b>\nIn die Umgebung tippen.";

    private static string langJsonURL = "";   
    private static string editorTestLanguage = "en";
	private static string editorTestCountry = "DE";
	
	private static string resourcesFilePath = "lang";
	private static string languageCode = "de";
	private static string countryCode = "";
	private static JSONNode languageJson;
	private static JSONNode languageJson_WithCategories;
	private static JSONNode newLanguageJson_WithCategories;
	private static string newLangJsonString = "{}";
	private static bool updateJsonOnThread = false;
	
	private static Thread _updateLanguageJsonThread;
	private static string langSavePath;
	private static Thread _loadJsonFilesThread;
	private static Thread mainLoadingThread;
	private static bool languageFileSaveToUse = true;
	private static string languageFileText;

	private static JSONNode languageJsonDrupal;
	private static readonly Dictionary<string, int> definitionIndices = new Dictionary<string, int>();    

	static LanguageController()
	{
		langSavePath = Application.persistentDataPath + "/lang.json";

		languageCode = GetLanguageCode();
		LoadDefinitionIndices();
		//LoadDefinitionIndicesAsync();
	}
	
	private static void LoadDefinitionIndicesAsync(){
		
		languageFileText = Resources.Load<TextAsset>(resourcesFilePath).text;

		mainLoadingThread = new Thread(LoadDefinitionIndicesOnThread);
		mainLoadingThread.Start();
	}
	
	private static void LoadDefinitionIndicesOnThread(){
		
		if( File.Exists( langSavePath ) ){
			languageJson = JSONNode.Parse( File.ReadAllText( langSavePath ) );
		}
		else{
			
			languageJson_WithCategories = JSONNode.Parse(languageFileText);
			languageJson = JSONNode.Parse("{\"translations\":[]}");
			
			JSONClass categories = (JSONClass)languageJson_WithCategories.AsObject;
			foreach (string category in categories.keys){
				
				for( int i = 0; i < languageJson_WithCategories[category].Count; i++ ){
					languageJson["translations"].Add(languageJson_WithCategories[category][i]);
				}
			}
		}
		
		for( int i = 0; i < languageJson["translations"].Count; i++ ){

			if( languageJson["translations"][i]["def"] != null ){
				if( !definitionIndices.ContainsKey( languageJson["translations"][i]["def"].Value ) ){
					definitionIndices.Add( languageJson["translations"][i]["def"].Value, i );
				}
			}
		}
		
		Debug.Log( System.DateTime.Now.ToString("mm:ss:FFF") );
		
		//Start_LoadJsonFiles_Thread();
		
		languageFileSaveToUse = true;
	}
	
	// load json with categories and create simple json with "translations" array for easier reading translations
	// Optional: Maybe this can be done on a thread to avoid the loading on start
	private static void LoadDefinitionIndices(){
				
		if( File.Exists( langSavePath ) ){
			languageJson = JSONNode.Parse( File.ReadAllText( langSavePath ) );
		}
		else{
			
			TextAsset languageFile = Resources.Load<TextAsset>(resourcesFilePath);
			languageJson_WithCategories = JSONNode.Parse(languageFile.text);
			languageJson = JSONNode.Parse("{\"translations\":[]}");
			
			JSONClass categories = (JSONClass)languageJson_WithCategories.AsObject;
			foreach (string category in categories.keys){
				
				for( int i = 0; i < languageJson_WithCategories[category].Count; i++ ){
					languageJson["translations"].Add(languageJson_WithCategories[category][i]);
				}
			}
		}

		for( int i = 0; i < languageJson["translations"].Count; i++ ){

			if( languageJson["translations"][i]["def"] != null ){
				if( !definitionIndices.ContainsKey( languageJson["translations"][i]["def"].Value ) ){
					definitionIndices.Add( languageJson["translations"][i]["def"].Value, i );
				}
			}
		}
				
		//Start_LoadJsonFiles_Thread();
	}
	
	public static void SaveLanguageJson( string jsonString ){
				
		newLangJsonString = jsonString;
		
		if( updateJsonOnThread ){
			Start_SaveLanguageJson_Thread();
		}
		else{
			UpdateLanuageJson();
			Start_SaveLanguageJson_Thread();
		}
				
	}
	
	public static void Start_SaveLanguageJson_Thread(){
		
		_updateLanguageJsonThread = new Thread(UpdateLanuageJsonThreadRoutine);
		_updateLanguageJsonThread.Start();
	}
	
	public static void UpdateLanuageJson(){
				
		newLanguageJson_WithCategories = JSONNode.Parse(newLangJsonString);
		JSONClass categories = (JSONClass)newLanguageJson_WithCategories.AsObject;
		foreach (string category in categories.keys){
				
			for( int i = 0; i < newLanguageJson_WithCategories[category].Count; i++ ){
					
				if( !definitionIndices.ContainsKey( newLanguageJson_WithCategories[category][i]["def"].Value ) ){					
						
					// Add new language definitions							
					languageJson["translations"].Add( newLanguageJson_WithCategories[category][i] );
					definitionIndices.Add( languageJson["translations"][ languageJson["translations"].Count-1 ]["def"].Value, languageJson["translations"].Count - 1 );
						
					//Debug.Log( "New translation added " + languageJson["translations"][ languageJson["translations"].Count-1 ]["def"].Value );
				}
				else{
						
					// Update existing JSONNode languageJson and definitionIndices
					int index = definitionIndices[ newLanguageJson_WithCategories[category][i]["def"].Value ];
					languageJson["translations"][index] = newLanguageJson_WithCategories[category][i];
						
				}
			}
		}
	}
	
	public static void UpdateLanuageJsonThreadRoutine()
	{
		if( updateJsonOnThread ){
			UpdateLanuageJson();
		}
		
		File.WriteAllText( langSavePath, languageJson.ToString() );
	}
	
	/*
	public static void Start_LoadJsonFiles_Thread(){
		
	_loadJsonFilesThread = new Thread(LoadJsonFiles);
	_loadJsonFilesThread.Start();
	}
	
	public static void LoadJsonFiles(){
		
		
	loadingDataProductsJson = true;
		
	if( dataProductsJson == null ){ 
	if( System.IO.File.Exists( savePathProductsJson ) ){
	dataProductsJson = JSONNode.Parse( System.IO.File.ReadAllText( savePathProductsJson ) );
	Debug.Log("dataProductsJson generated");
	}else{
	Debug.Log("dataProductsJson not existing");
	}
	}
		
	loadingDataProductsJson = false;
	}
	
	public static void UpdateDataProductsJson( string jsonText ){
	dataProductsJson = JSONNode.Parse( jsonText );
	Debug.Log( "data products json updated" );
	}
	*/
	
	public static void ChangeAllLabels()
	{
		Text[] unityLabels = Resources.FindObjectsOfTypeAll(typeof(Text)) as Text[];
		foreach (Text t in unityLabels)
		{
			if (t.GetComponent<TextOptions>() != null)
			{
				t.GetComponent<TextOptions>().TranslateText();
			}
		}

		TextMeshProUGUI[] textMeshProLabels = Resources.FindObjectsOfTypeAll(typeof(TextMeshProUGUI)) as TextMeshProUGUI[];
		foreach (TextMeshProUGUI t in textMeshProLabels)
		{
			if (t.GetComponent<TextOptions>() != null)
			{
				t.GetComponent<TextOptions>().TranslateText();
			}
		}
	}


	public static string GetTranslationOrigin(string definition)
	{
		//Debug.Log("GetTranslationOrigin " + definition);

		if( !languageFileSaveToUse ) return definition;
		
		string langCode = languageCode;
		if( languageCode == "ja" && FontController.instance != null && !FontController.instance.JapaneseFontLoaded() ){
			langCode = "en";
		}
		
		if ( languageJson != null )
		{
			if( definitionIndices.ContainsKey(definition) ){
				
				int definitionIndex = definitionIndices[definition];
				if( definitionIndex >= 0 ){
					if ( languageJson["translations"][definitionIndex][langCode] != null )
					{
						return languageJson["translations"][definitionIndex][langCode];
					}
					else if ( languageJson["translations"][definitionIndex]["en"] != null )
					{
						return languageJson["translations"][definitionIndex]["en"];
					}
				}
				
			}else{
				string tmpDefinition = definition.Replace("\\n", "\n");
				if( TranslationExists(tmpDefinition) ){
					return GetTranslation(tmpDefinition);
				}
			}
		}

		return definition;
	}
	
	public static bool TranslationExists(string definition){
		
		if( string.IsNullOrEmpty(definition) ) return false;
		
		try{
			
			string langCode = languageCode;
			if( languageCode == "ja" && FontController.instance != null && !FontController.instance.JapaneseFontLoaded() ){
				langCode = "en";
			}
			
			if ( languageJson != null )
			{
				if( definitionIndices.ContainsKey(definition) ){
					
					int definitionIndex = definitionIndices[definition];
					if( definitionIndex >= 0 ){
						if ( languageJson["translations"][definitionIndex][langCode] != null )
						{
							return true;
						}
						else if ( languageJson["translations"][definitionIndex]["en"] != null )
						{
							return true;
						}
					}
					
				}
			}
			
		}
		catch( Exception e ){
			Debug.Log("TranslationExists error " + e.Message);
		}
		
		return false;
	}

	public static void TranslateAllTextOptionLabels(){
		
		TextOptions[] labels = Resources.FindObjectsOfTypeAll<TextOptions>();
		for( int i = 0; i < labels.Length; i++ ){
			labels[i].TranslateText();
		}
	}

	// Custom functions to get translation using json from Drupal and ServerBackendController
	public static string GetTranslation(string definition, bool shouldParse = false)
	{
		//definition = "AAAAAAAAAA";

        if ( string.IsNullOrEmpty(definition) ) return definition;

		if (shouldParse) { definition = HttpUtility.HtmlDecode(definition); }
        //definition = definition.Replace("&amp;", "&");
        //definition = definition.Replace("&nbsp;", " ");
        //definition = definition.Replace("&Auml;", "Ä");
        //definition = definition.Replace("&Ouml;", "Ö");
        //definition = definition.Replace("&Uuml;", "Ü");
        //definition = definition.Replace("&auml;", "ä");
        //definition = definition.Replace("&ouml;", "ö");
        //definition = definition.Replace("&uuml;", "ü");
        //definition = definition.Replace("&szlig;", "ß");

        try
        {
			
			InitBackendJson();
			if( languageJsonDrupal == null ) return definition;

            definition = System.Text.RegularExpressions.Regex.Unescape(definition);
            string langCode = languageCode;
			for( int i = 0; i < languageJsonDrupal["translations"].Count; i++ ){
	
				if( languageJsonDrupal["translations"][i]["def"] != null ){

                    string unescapedDef = System.Text.RegularExpressions.Regex.Unescape(languageJsonDrupal["translations"][i]["def"].Value);

                    if ( unescapedDef == definition ){

                        if ( languageJsonDrupal["translations"][i][langCode] != null )
						{
							string text = languageJsonDrupal["translations"][i][langCode].Value;
							text = text.Replace("\\n", "\n");
							return text;
						}
						else if ( languageJsonDrupal["translations"][i]["en"] != null )
						{
							string text = languageJsonDrupal["translations"][i]["en"].Value;
							text = text.Replace("\\n", "\n");
							return text;
						}
                        else if (languageJsonDrupal["translations"][i]["de"] != null)
                        {
                            string text = languageJsonDrupal["translations"][i]["de"].Value;
                            text = text.Replace("\\n", "\n");
                            return text;
                        }
						break;
					}
				}
			}

            if ( TranslationExists(definition) ){
				return GetTranslationOrigin(definition);
			}
		}
		catch( Exception e ){
			Debug.Log("TranslationExists error " + e.Message);
		}
		
		return definition;
	}
	
    public static string GetTranslationFromNode(JSONNode node, bool shouldParse = false)
    {
        string langCode = languageCode;
        string returnText = "";

        if (node[langCode] != null)
        {
            string text = node[langCode].Value;
            text = text.Replace("\\n", "\n");
            if (shouldParse) { text = HttpUtility.HtmlDecode(text); }
            //return text;

            returnText = text;
        }
        else if (node["en"] != null)
        {
            string text = node["en"].Value;
            text = text.Replace("\\n", "\n");
            if (shouldParse) { text = HttpUtility.HtmlDecode(text); }
            //return text;

            returnText = text;
        }
        else if (node["de"] != null)
        {
            string text = node["de"].Value;
            text = text.Replace("\\n", "\n");
            if (shouldParse) { text = HttpUtility.HtmlDecode(text); }
            //return text;

            returnText = text;
        }
        else if (node["def"] != null)
        {
            string text = node["def"].Value;
            text = text.Replace("\\n", "\n");
            if (shouldParse) { text = HttpUtility.HtmlDecode(text); }
            //return text;

            returnText = text;
        }
        else
        {
            try
            {
                string text = node.Value;
                if (text == "null") return "";
                if (shouldParse) { text = HttpUtility.HtmlDecode(text); }
                //return text;

                returnText = text;
            }
            catch (Exception e) { Debug.Log("GetTranslationFromNode error " + e.Message); }
        }

        return ToolsController.instance.ConvertHTMLToValidTextMeshProText(returnText);
        //return returnText;
    }
	
	public static void InitBackendJson(){

		if (languageJsonDrupal != null) { return; }
		
		languageJsonDrupal = JSONNode.Parse("{\"translations\":[]");
		JSONNode langJson = ServerBackendController.instance.GetJson("_translations");
        if ( langJson["userInterfaceTexts"] == null ) return;

		for( int i = 0; i < langJson["userInterfaceTexts"].Count; i++ )
		{
			JSONNode langText = langJson["userInterfaceTexts"][i];
			if (langText["default"] != null) { langText["def"] = langText["default"].Value; }
            languageJsonDrupal["translations"].Add(langText);
		}
    }

    public static void UpdateBackendJson(){

        languageJsonDrupal = JSONNode.Parse("{\"translations\":[]");
        JSONNode langJson = ServerBackendController.instance.GetJson("_translations");
        if (langJson["userInterfaceTexts"] == null) return;

        for (int i = 0; i < langJson["userInterfaceTexts"].Count; i++)
        {
            JSONNode langText = langJson["userInterfaceTexts"][i];
            if (langText["default"] != null) { langText["def"] = langText["default"].Value; }
            languageJsonDrupal["translations"].Add(langText);
        }
    }





    private static readonly Dictionary<SystemLanguage, string> CountryCodes = new Dictionary<SystemLanguage, string>
	{
		{ SystemLanguage.Afrikaans, "af-ZA"},
		{ SystemLanguage.Arabic, "ar-SA"},
		{ SystemLanguage.Basque, "eu-ES"},
		{ SystemLanguage.Belarusian, "be-BY"},
		{ SystemLanguage.Bulgarian, "bg-BG"},
		{ SystemLanguage.Catalan, "ca-ES"},
		{ SystemLanguage.Chinese, "zh-CN"},
		{ SystemLanguage.Czech, "cs-CZ"},
		{ SystemLanguage.Danish, "da-DK"},
		{ SystemLanguage.Dutch, "nl-NL"},
		{ SystemLanguage.English, "en-US"},
		{ SystemLanguage.Estonian, "et-EE"},
		{ SystemLanguage.Faroese, "fo-FO"},
		{ SystemLanguage.Finnish, "fi-FI"},
		{ SystemLanguage.French, "fr-FR"},
		{ SystemLanguage.German, "de-DE"},
		{ SystemLanguage.Greek, "el-GR"},
		{ SystemLanguage.Hebrew, "he-IL"},
		{ SystemLanguage.Hungarian, "hu-HU"},
		{ SystemLanguage.Icelandic, "is-IS"},
		{ SystemLanguage.Indonesian, "id-ID"},
		{ SystemLanguage.Italian, "it-IT"},
		{ SystemLanguage.Japanese, "ja-JP"},
		{ SystemLanguage.Korean, "ko-KR"},
		{ SystemLanguage.Latvian, "lv-LV"},
		{ SystemLanguage.Lithuanian, "lt-LT"},
		{ SystemLanguage.Norwegian, "no-NO"},
		{ SystemLanguage.Polish, "pl-PL"},
		{ SystemLanguage.Portuguese, "pt-PT"},
		{ SystemLanguage.Romanian, "ro-RO"},
		{ SystemLanguage.Russian, "ru-RU"},
		{ SystemLanguage.SerboCroatian, "sr-RS"}, //HR for Croatia
		{ SystemLanguage.Slovak, "sk-SK"},
		{ SystemLanguage.Slovenian, "sl-SI"},
		{ SystemLanguage.Spanish, "es-ES"},
		{ SystemLanguage.Swedish, "sv-SE"},
		{ SystemLanguage.Thai, "th-TH"},
		{ SystemLanguage.Turkish, "tr-TR"},
		{ SystemLanguage.Ukrainian, "uk-UA"},
		{ SystemLanguage.Vietnamese, "vi-VN"},
		{ SystemLanguage.Unknown, "zz-US" }
    };
	
	public static string GetLanguageCode(){
		
		#if UNITY_EDITOR
		if( editorTestLanguage != "" ) return editorTestLanguage;
		#endif
		
		string result;
		if (CountryCodes.TryGetValue(Application.systemLanguage, out result))
		{
			return result.Substring(0, 2);
		}
		else
		{
			//return "Unknown";
			return "en";
		}
	}
	
    public static string GetAppLanguageCode()
    {
        string lang = GetLanguageCode();
		return lang;

        //if (lang == "de") { return "de"; }
        //else { return "en"; }
    }

	public static string GetCountryCode(){
		return countryCode;
	}
	
	public static void SetCountryCode( string myCountryCode ){
		countryCode = myCountryCode;
	}
	
	public static string GetEditorTestCountry(){
		return editorTestCountry;
	}
}


