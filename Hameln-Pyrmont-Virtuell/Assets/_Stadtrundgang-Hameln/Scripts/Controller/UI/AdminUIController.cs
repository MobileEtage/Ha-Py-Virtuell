using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleJSON;

public class AdminUIController : MonoBehaviour
{
	public bool adminMenuEnabled = false;

	[Space( 10 )]

	public GameObject adminPasswordMenu;
	public TMP_InputField passwordInputfield;
	public TextMeshProUGUI adminMenuLabel;

	public GameObject buttonHolder;
	public GameObject offCanvas;
	public Toggle planesToggle;
	public Toggle backendPositionsToggle;
	public GameObject mainCamera;
	
	[Space(10)]
	public List<GameObject> adminSettings = new List<GameObject>();

	private JSONNode dataJson;
	private string password = "hamelnglashuette123";

	public static AdminUIController instance;
	void Awake()
    {
	    instance = this;
    }

	void Start(){
		
		/*
		dataJson = JSONNode.Parse(Resources.Load<TextAsset>("_adminMenu").text);

		if( adminMenuEnabled ){
			
			EnableDisableAdminSettings(true);
			InitAdminMenu();
		}
		else{
			EnableDisableAdminSettings(false);
		}
		*/
	}

	public void CommitPassword()
	{
		if( passwordInputfield.text == password )
		{			
			passwordInputfield.text = "";
			GlasObjectController.instance.productionBuild = !GlasObjectController.instance.productionBuild;

			if ( GlasObjectController.instance.productionBuild )
			{
				adminMenuLabel.text = "Admin-Funktionen\nfreischalten";
				InfoController.instance.ShowMessage( "Admin-Funktionen\ndeaktiviert!" );
			}
			else
			{
				adminMenuLabel.text = "Admin-Funktionen\ndeaktivieren";
				InfoController.instance.ShowMessage( "Admin-Funktionen\nfreigeschaltet!" );
			}

			adminPasswordMenu.SetActive(false);
		}
		else
		{
			StopCoroutine( "ShowWrongPasswordInfoCoroutine" );
			StartCoroutine( "ShowWrongPasswordInfoCoroutine" );
		}
	}

	public IEnumerator ShowWrongPasswordInfoCoroutine()
	{
		bool unlocked = GlasObjectController.instance.productionBuild;

		if ( GlasObjectController.instance.productionBuild ){ adminMenuLabel.text = "Admin-Funktionen\nfreischalten\n<size=75%><color=#dd0000>Falsches Passwort"; }
		else{ adminMenuLabel.text = "Admin-Funktionen\ndeaktivieren\n<size=75%><color=#dd0000>Falsches Passwort"; }

		yield return new WaitForSeconds(2.0f);

		if ( GlasObjectController.instance.productionBuild == unlocked )
		{
			if ( GlasObjectController.instance.productionBuild ) { adminMenuLabel.text = "Admin-Funktionen\nfreischalten"; }
			else { adminMenuLabel.text = "Admin-Funktionen\ndeaktivieren"; }
		}
	}

	public void ShowAdminPasswordMenu()
	{
		adminPasswordMenu.SetActive(true);
	}
	
	public void EnableDisableAdminSettings(bool enable){
		
		for( int i = 0; i < adminSettings.Count; i++ ){
			adminSettings[i].SetActive(enable);
		}
	}
    
	public void InitAdminMenu(){
				
		for( int i = 0; i < dataJson["features"].Count; i++ ){
			
			if( dataJson["features"][i]["enabled"] != null && !dataJson["features"][i]["enabled"].AsBool ) continue;
			
			int index = i;
			
			GameObject obj = ToolsController.instance.InstantiateObject("UI/OffCanvasButtonPrefab", buttonHolder.transform);
			GameObject stationButton = ToolsController.instance.FindGameObjectByName(obj, "ButtonStation");
			stationButton.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation( dataJson["features"][i]["button"].Value );
			stationButton.GetComponentInChildren<Button>().onClick.AddListener(() => 
				TestController.instance.TestARFeature(
				dataJson["features"][index]["id"].Value,
				dataJson["features"][index]["markerId"].Value
				));
				
			if( dataJson["features"][i]["editable"] != null && dataJson["features"][i]["editable"].AsBool ){
				
				GameObject editButton = ToolsController.instance.FindGameObjectByName(obj, "ButtonEdit");
				editButton.SetActive(true);
				editButton.GetComponentInChildren<Button>().onClick.AddListener(() => 
					TestController.instance.TestARFeature(dataJson["features"][index]["id"].Value + "Edit"));
			}

		}		
	}
	
	public bool NeedsPermission( string id, string permission ){
		
		for( int i = 0; i < dataJson["features"].Count; i++ ){
			
			if( 
				dataJson["features"][i]["id"].Value == id && 
				dataJson["features"][i][permission] != null && 
				dataJson["features"][i][permission].AsBool )
			{
				return true;
			}
		}
		return false;
	}
	
	public bool ShouldScan( string id ){
		
		for( int i = 0; i < dataJson["features"].Count; i++ ){
			
			if( 
				dataJson["features"][i]["id"].Value == id && 
				dataJson["features"][i]["shouldScan"] != null && 
				dataJson["features"][i]["shouldScan"].AsBool )
			{
				return true;
			}
		}
		return false;
	}
	
	public void ToggleARPlanes( Toggle toggle ){
		
		ARController.instance.ShowHidePlanes( toggle.isOn );
	}
}
