using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightController : MonoBehaviour
{
	public Light directionalLight;
	private Material skyboxMaterial;
	
	private string currentMarkerId = "";
	private float updateRotationTime = 0.1f;
	private Vector3 targetLightRotation = Vector3.zero;

	private bool fixedGlobalLightRotation = false;
	
	public static LightController instance;
	void Awake()
    {
	    instance = this;
    }

	public void LateUpdate(){

        //UpdateLight();
	}
	
	public void UpdateLight(){
			
		if( fixedGlobalLightRotation ) return;
		
		List<string> markerIds = new List<string>(){ "universal", currentMarkerId };
		ImageTarget currentTrackedImageTarget = ImageTargetController.Instance.GetTrackedImageTarget(markerIds);
		
		if( currentTrackedImageTarget != null ){
	    	
			if( updateRotationTime <= 0 ){
				
				updateRotationTime = ARController.instance.onMarkerTrackedUpdatePositionInterval;
				ImageTargetController.Instance.PlaceObjectToMarker(
					directionalLight.gameObject, currentTrackedImageTarget.transform, 
					Vector3.zero,
					targetLightRotation
				);
			}
			else{
				updateRotationTime -= Time.deltaTime;
			}
		}
	}
	
	public void SetLightRotation( List<string> markerIds  ){
		
		ImageTargetController.Instance.PlaceObjectToMarker(
			directionalLight.gameObject, markerIds, 
			Vector3.zero,
			targetLightRotation
		);
	}
	
	public void SetLightSettings( string stationId, string markerId )
	{
		currentMarkerId = markerId;
		targetLightRotation = new Vector3(90, 0, 0);
		directionalLight.transform.eulerAngles = targetLightRotation;
		fixedGlobalLightRotation = false;
		directionalLight.intensity = 1.0f;
		updateRotationTime = 0.1f;
		
	    switch(stationId){

        case "boar":

            skyboxMaterial = Resources.Load<Material>("SkyboxMaterials/FOREST HDRI");
            RenderSettings.skybox = skyboxMaterial;
            targetLightRotation = new Vector3(45, 0, 0);
            break;

        case "weasel":
	    	
	    	skyboxMaterial = Resources.Load<Material>("SkyboxMaterials/FOREST HDRI");
		    RenderSettings.skybox = skyboxMaterial;
		    targetLightRotation = new Vector3(45, 0, 0);
		    break;
		    
	    case "tree":
	    	
	    	skyboxMaterial = Resources.Load<Material>("SkyboxMaterials/FOREST HDRI");
		    RenderSettings.skybox = skyboxMaterial;
		    
		    // The tree has some weird occlusion shader which only 
		    // works if we set a custom global rotation for the light
		    fixedGlobalLightRotation = true;
		    targetLightRotation = new Vector3(0, 0, 0);
		    directionalLight.transform.eulerAngles = targetLightRotation;

		    break;
		    
	    case "treeHealth":
	    	
	    	skyboxMaterial = Resources.Load<Material>("SkyboxMaterials/FOREST HDRI");
		    RenderSettings.skybox = skyboxMaterial;
		    
		    fixedGlobalLightRotation = true;
		    targetLightRotation = new Vector3(0, 0, 0);
		    directionalLight.transform.eulerAngles = targetLightRotation;
		    
		    break;
		    
	    case "woodpecker":
	    	
	    	skyboxMaterial = Resources.Load<Material>("SkyboxMaterials/FOREST HDRI");
		    RenderSettings.skybox = skyboxMaterial;
		    
		    //fixedGlobalLightRotation = true;
		    directionalLight.intensity = 0.75f;
		    targetLightRotation = new Vector3(0, 0, 0);
		    directionalLight.transform.eulerAngles = targetLightRotation;
		    
		    break;
		    
	    case "Rosalotta":
	    	
		    skyboxMaterial = Resources.Load<Material>("SkyboxMaterials/DefaultSky");
		    RenderSettings.skybox = skyboxMaterial;
		    targetLightRotation = new Vector3(45, 0, 0);
		    directionalLight.intensity = 0.8f;
		    break;
		  
        case "viewingPigeons":
	    	
	        skyboxMaterial = Resources.Load<Material>("SkyboxMaterials/DefaultSky");
	        RenderSettings.skybox = skyboxMaterial;
	        directionalLight.intensity = 0.8f;
	        break;
	        
        case "squirrel":
	    	
	        skyboxMaterial = Resources.Load<Material>("SkyboxMaterials/neurathen_rock_castle_8k");
	        RenderSettings.skybox = skyboxMaterial;
	        directionalLight.intensity = 0.5f;
		    //targetLightRotation = new Vector3(150, 0, 0);
	        //targetLightRotation = new Vector3(53, -71, -78);
	        targetLightRotation = new Vector3(11.3f, -144.53f, -183.376f);
	        directionalLight.transform.eulerAngles = targetLightRotation;

	        break;
	        
	    default:
	    
		    skyboxMaterial = Resources.Load<Material>("SkyboxMaterials/DefaultSky");
		    RenderSettings.skybox = skyboxMaterial;
		    break;
	    }
	    		
	    DynamicGI.UpdateEnvironment();
    }

	public void SetLightSettings(string id)
	{
		StartCoroutine(SetLightSettingsCoroutine(id));
	}

	public IEnumerator SetLightSettingsCoroutine(string id)
    {
		//targetLightRotation = new Vector3(45, -10, 0);
		//directionalLight.transform.eulerAngles = targetLightRotation;
		//fixedGlobalLightRotation = false;
		//directionalLight.intensity = 1.0f;
		//updateRotationTime = 0.1f;

		print("SetLightSettings " + id);

		switch (id)
		{

			case "Default":

				skyboxMaterial = Resources.Load<Material>("SkyboxMaterials/DefaultSky");
				RenderSettings.skybox = skyboxMaterial;
				print("Loading default Sky");
				break;

			case "Soldier":

				skyboxMaterial = Resources.Load<Material>("SkyboxMaterials/GreySky");
				RenderSettings.skybox = skyboxMaterial;
				print("Loading Soldier Sky");
				break;

			default:

				skyboxMaterial = Resources.Load<Material>("SkyboxMaterials/DefaultSky");
				RenderSettings.skybox = skyboxMaterial;
				break;
		}

		yield return null;
		yield return new WaitForEndOfFrame();

		DynamicGI.UpdateEnvironment();
	}
}
