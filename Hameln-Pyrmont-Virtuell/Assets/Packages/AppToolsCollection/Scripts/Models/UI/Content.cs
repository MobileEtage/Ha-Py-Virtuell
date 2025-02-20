
using System.Collections.Generic;
using UnityEngine;

public class Content : MonoBehaviour
{
	public string contentID = "";
	public bool isAssetScene = false;
	
	[Space(10)]
	
	public string firstSiteID = "";
	
	[HideInInspector] public bool isLoaded = false;
	[HideInInspector] public bool isUnloading = false;
	[HideInInspector] public List<Site> sites = new List<Site>();	
	[HideInInspector] public List<GameObject> rootObjects = new List<GameObject>();	

}
