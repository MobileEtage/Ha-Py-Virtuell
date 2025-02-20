
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using DieEtagen;

/*

This controller class controls the site navigation. 
You can call "SwitchToSite" to open the desired app site
Each site has an unique site id
You can store a site in a different scene to avoid unnecessary memory usage
The SwitchToSite function will load the given site scene and append it to the main canvas

*/

public class SiteController : MonoBehaviour
{

    //[HideInInspector]public Site currentSite;
    public Site currentSite;
    public Site previousSite;
    public float siteFadingTime = 0.2f; // Time to fade between sites

    public enum OpenSiteAnimationType { FadeIn, MoveFromBottom, MoveFromRight, MoveFromLeft };
    private OpenSiteAnimationType showSiteAnimationType = OpenSiteAnimationType.FadeIn;

    private int loadedScenes = 0;
    private List<Site> sites = new List<Site>();
    private bool isLoading = false;

    public static SiteController instance;
    void Awake()
    {

        instance = this;
        LoadAllSites();
    }

    void Start()
    {

    }

    // Save all available Site objects in list
    public void LoadAllSites()
    {

        sites.Clear();

        // Load all available sites in scene
        Site[] sitesTmp = Resources.FindObjectsOfTypeAll<Site>();
        for (int i = 0; i < sitesTmp.Length; i++)
        {

            sites.Add(sitesTmp[i]);

#if UNITY_EDITOR
            /*
			if( sitesTmp[i].siteID == "ScanSite" ){
				
				foreach(Transform child in sitesTmp[i].transform ){
					
					if( child.name != "BackButton" ){ 
						child.gameObject.SetActive(false); 
					}
				}
			}
			sitesTmp[i].gameObject.SetActive(false);
			*/
#endif
        }

#if UNITY_EDITOR
        if (currentSite != null) { currentSite.gameObject.SetActive(true); }
#endif
    }

    public void OpenSiteFromBottom(string targetSiteID)
    {
        showSiteAnimationType = OpenSiteAnimationType.MoveFromBottom;
        SwitchToSite(targetSiteID);
    }

    public void OpenSiteFromRight(string targetSiteID)
    {
        showSiteAnimationType = OpenSiteAnimationType.MoveFromRight;
        SwitchToSite(targetSiteID);
    }

    public void OpenSiteFromLeft(string targetSiteID)
    {
        showSiteAnimationType = OpenSiteAnimationType.MoveFromLeft;
        SwitchToSite(targetSiteID);
    }

    public void FadeInSite(string targetSiteID)
    {
        showSiteAnimationType = OpenSiteAnimationType.FadeIn;
        SwitchToSite(targetSiteID);
    }

    public void SetCurrentSite(string targetSiteID)
    {

        if (targetSiteID == "Site_Selection") targetSiteID = "Site_Downhill_Tutorial1";

        Site targetSite = GetSite(targetSiteID);

        if (currentSite != null)
        {
            targetSite.transform.SetParent(currentSite.transform.parent);
            targetSite.transform.SetSiblingIndex(currentSite.transform.GetSiblingIndex() + 1);
        }

        ToolsController.instance.ResetScrollRects(targetSite.gameObject);

        previousSite = currentSite;
        currentSite = targetSite;
        if (previousSite != null) previousSite.gameObject.name = previousSite.gameObject.name.Replace("---Current---", "");
        currentSite.gameObject.name = "---Current---" + currentSite.gameObject.name;

        ToolsController.instance.SetScreenPosition(targetSite.GetComponent<RectTransform>(), 0f, 0f, true);
        targetSite.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(targetSite.GetComponent<RectTransform>().anchoredPosition3D.x, targetSite.GetComponent<RectTransform>().anchoredPosition3D.y, 0);
        targetSite.GetComponent<RectTransform>().localEulerAngles = Vector3.zero;

        if (targetSite.GetComponent<Canvas>() != null) { previousSite.GetComponent<Canvas>().enabled = true; }
        else { targetSite.gameObject.SetActive(true); }
        if (targetSite.GetComponent<CanvasGroup>() != null) { previousSite.GetComponent<CanvasGroup>().alpha = 1; }

        if (previousSite != null)
        {
            if (previousSite.GetComponent<Canvas>() != null) { previousSite.GetComponent<Canvas>().enabled = false; }
            else { previousSite.gameObject.SetActive(false); }
            if (previousSite.GetComponent<CanvasGroup>() != null) { previousSite.GetComponent<CanvasGroup>().alpha = 0; }
        }
    }

    public void SetCurrentSite(Site targetSite)
    {

        if (currentSite != null)
        {
            targetSite.transform.SetParent(currentSite.transform.parent);
            targetSite.transform.SetSiblingIndex(currentSite.transform.GetSiblingIndex() + 1);
        }

        ToolsController.instance.ResetScrollRects(targetSite.gameObject);

        previousSite = currentSite;
        currentSite = targetSite;
        if (previousSite != null) previousSite.gameObject.name = previousSite.gameObject.name.Replace("---Current---", "");
        currentSite.gameObject.name = "---Current---" + currentSite.gameObject.name;

        ToolsController.instance.SetScreenPosition(targetSite.GetComponent<RectTransform>(), 0f, 0f, true);
        targetSite.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(targetSite.GetComponent<RectTransform>().anchoredPosition3D.x, targetSite.GetComponent<RectTransform>().anchoredPosition3D.y, 0);
        targetSite.GetComponent<RectTransform>().localEulerAngles = Vector3.zero;

        if (targetSite.GetComponent<Canvas>() != null) { previousSite.GetComponent<Canvas>().enabled = true; }
        else { targetSite.gameObject.SetActive(true); }
        if (targetSite.GetComponent<CanvasGroup>() != null) { previousSite.GetComponent<CanvasGroup>().alpha = 1; }

        if (previousSite != null)
        {
            if (previousSite.GetComponent<Canvas>() != null) { previousSite.GetComponent<Canvas>().enabled = false; }
            else { previousSite.gameObject.SetActive(false); }

            if (previousSite.GetComponent<CanvasGroup>() != null) { previousSite.GetComponent<CanvasGroup>().alpha = 0; }
        }
    }


    // Main function to switch between sites
    public void SwitchToSite(string targetSiteID)
    {

        //print("SwitchToSite " + targetSiteID);

        if ((currentSite != null && currentSite.siteID == targetSiteID) || isLoading) return;
        isLoading = true;
        CanvasController.instance.eventSystem.enabled = false;
        StartCoroutine(SwitchToSiteCoroutine(targetSiteID));
    }

    private IEnumerator SwitchToSiteCoroutine(string targetSiteID)
    {

        Site site = GetSite(targetSiteID);

        if (site != null)
        {

            yield return StartCoroutine(LoadSiteCoroutine(site));
            yield return StartCoroutine(SwitchToSiteCoroutine(site));

            //FreeMemory();
        }
        else
        {
            Debug.LogError(targetSiteID + " not found");
        }

        isLoading = false;
        CanvasController.instance.eventSystem.enabled = true;
    }

    private bool SiteIsLoaded(Site site)
    {

        for (int i = 0; i < sites.Count; i++)
        {
            if (sites[i].siteID == site.siteID)
            {
                return sites[i].isLoaded || !sites[i].isAssetScene;
            }
        }
        return false;
    }

    private IEnumerator LoadSiteCoroutine(Site site)
    {

        if (site == null || SiteIsLoaded(site)) yield break;

        while (site.isUnloading)
        {
            yield return null;
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(site.siteID, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Scene scene = SceneManager.GetSceneByName(site.siteID);
        if (scene != null)
        {

            GameObject[] rootObjects = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {

                //site.rootObjects.Add(rootObjects[i]);
                //rootObjects[i].transform.SetParent(site.transform);

                if (rootObjects[i].GetComponent<RectTransform>() != null)
                {

                    site.rootObjects.Add(rootObjects[i]);
                    rootObjects[i].transform.SetParent(site.transform);

                    rootObjects[i].transform.localScale = Vector3.one;
                    RectTransform rt = rootObjects[i].GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.anchoredPosition3D = new Vector3(rt.anchoredPosition3D.x, rt.anchoredPosition3D.y, 0);
                    rt.localEulerAngles = Vector3.zero;

                    if (rootObjects[i].transform.childCount > 0) { rootObjects[i].transform.GetChild(0).gameObject.SetActive(true); }
                }
            }

            site.isLoaded = true;
            site.gameObject.name = "[Loaded]_" + site.siteID;
            loadedScenes++;
        }
    }

    public Site GetSite(string siteID)
    {

        for (int i = 0; i < sites.Count; i++)
        {
            if (sites[i].siteID == siteID)
            {
                return sites[i];
            }
        }
        return null;
    }

    private void FreeMemory()
    {

        if (loadedScenes > DieEtagen.Constants.maxLoadedSiteScenes)
        {
            Site oldestUsedSite = GetOldestUsedSite();
            UnloadSite(oldestUsedSite);
        }
    }

    private Site GetOldestUsedSite()
    {

        // Todo: count use of sites and assign usage value to remove most less used sites from memory
        // also check keepload parameter of site which should not be removed from memory
        return sites[0];
    }

    public void UnloadSite(Site site)
    {

        if (site.isLoaded) { StartCoroutine(UnloadSiteCoroutine(site)); }
    }

    // Todo: unloading not working, for some reason it stucks here, but does not freeze the game
    public IEnumerator UnloadSiteCoroutine(Site site)
    {

        if (!site.isLoaded) { yield break; }

        site.isUnloading = true;

        Scene scene = SceneManager.GetSceneByName(site.siteID);
        for (int i = 0; i < site.rootObjects.Count; i++)
        {

            site.rootObjects[i].transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(site.rootObjects[i], scene);
            site.rootObjects[i].SetActive(false);
        }

        AsyncOperation asyncLoad = SceneManager.UnloadSceneAsync(site.siteID);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        yield return null;

        site.isLoaded = false;
        site.gameObject.name = "[UnLoaded]_" + site.siteID;
        loadedScenes--;

        site.rootObjects.Clear();
        site.isUnloading = false;
    }

    public IEnumerator SwitchToSiteCoroutine(Site targetSite, bool activateHeader = true)
    {

        if (currentSite != null && currentSite.siteID == targetSite.siteID) yield break;

        CanvasController.instance.eventSystem.enabled = false;

        //VideoController.instance.ResetVideoPlayer();

        if (targetSite == null)
        {
            yield break;
        }

        if (currentSite != null)
        {
            targetSite.transform.SetParent(currentSite.transform.parent);
            targetSite.transform.SetSiblingIndex(currentSite.transform.GetSiblingIndex() + 1);
        }

        ToolsController.instance.ResetScrollRects(targetSite.gameObject);

        previousSite = currentSite;
        currentSite = targetSite;
        if (previousSite != null) previousSite.gameObject.name = previousSite.gameObject.name.Replace("---Current---", "");
        currentSite.gameObject.name = "---Current---" + currentSite.gameObject.name;

        ToolsController.instance.SetScreenPosition(targetSite.GetComponent<RectTransform>(), 0f, 0f, true);
        targetSite.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(targetSite.GetComponent<RectTransform>().anchoredPosition3D.x, targetSite.GetComponent<RectTransform>().anchoredPosition3D.y, 0);
        targetSite.GetComponent<RectTransform>().localEulerAngles = Vector3.zero;

        yield return StartCoroutine(ShowSiteWithAnimation(targetSite, activateHeader));
        showSiteAnimationType = OpenSiteAnimationType.FadeIn;   // Reset to default site animation
    }

    private IEnumerator ShowSiteWithAnimation(Site targetSite, bool activateHeader = true)
    {

        if (showSiteAnimationType == OpenSiteAnimationType.FadeIn)
        {

            /*
			if( previousSite != null ){
				StartCoroutine( 
					ToolsController.instance.FadeInCanvasGroupCoroutine( previousSite.GetComponent<CanvasGroup>(), siteFadingTime, 0, 0) 
				);	
			}
			*/

            yield return StartCoroutine(
                ToolsController.instance.FadeInCanvasGroupCoroutine(targetSite.GetComponent<CanvasGroup>(), siteFadingTime, 1, 0, previousSite != null ? previousSite.gameObject : null)
            );

        }
        else if (showSiteAnimationType == OpenSiteAnimationType.MoveFromBottom)
        {
            ToolsController.instance.SetScreenPosition(targetSite.GetComponent<RectTransform>(), 0f, -1f, false);
            yield return StartCoroutine(ToolsController.instance.MoveCoroutine(targetSite.GetComponent<RectTransform>(), 0, 0, 0.5f, 3f, true, false));
        }
        else
        {

            // Default animation, fade in
            yield return StartCoroutine(
                ToolsController.instance.FadeInCanvasGroupCoroutine(targetSite.GetComponent<CanvasGroup>(), siteFadingTime, 1, 0, previousSite != null ? previousSite.gameObject : null)
            );
        }

        if (previousSite != null)
        {

            if (previousSite.isAssetScene && !previousSite.keepLoaded)
            {
                yield return StartCoroutine(UnloadSiteCoroutine(previousSite));
                previousSite = null;
            }
        }

        CanvasController.instance.eventSystem.enabled = true;
    }

    /******************** Overload methods ********************/

    public IEnumerator SwitchToSiteCoroutine(string targetSiteID, bool activateHeader = true)
    {
        if (currentSite != null && currentSite.siteID == targetSiteID) yield break;

        CanvasController.instance.eventSystem.enabled = false;

        Site targetSite = GetSite(targetSiteID);

        Scene scene = SceneManager.GetSceneByName(targetSite.siteID);
        if (scene == null || !scene.isLoaded)
        {
            yield return StartCoroutine(LoadSiteCoroutine(targetSite));
        }

        yield return StartCoroutine(SwitchToSiteCoroutine(targetSite, activateHeader));

        CanvasController.instance.eventSystem.enabled = true;
    }

	public void ShowHideSite(string targetSiteID, bool shouldShow, float alpha)
	{
		Site targetSite = GetSite( targetSiteID );
		if(targetSite != null )
		{
			targetSite.gameObject.SetActive( shouldShow );
			if ( targetSite.GetComponent<CanvasGroup>() != null ) { targetSite.GetComponent<CanvasGroup>().alpha = alpha; }
		}
	}

	public IEnumerator LoadSiteCoroutine(string targetSiteID)
    {

        Site targetSite = GetSite(targetSiteID);
        yield return StartCoroutine(LoadSiteCoroutine(targetSite));
    }

    public IEnumerator ActivateSiteCoroutine(string targetSiteID)
    {

        CanvasController.instance.eventSystem.enabled = false;

        Site targetSite = GetSite(targetSiteID);

        Scene scene = SceneManager.GetSceneByName(targetSite.siteID);
        if (scene == null || !scene.isLoaded)
        {
            yield return StartCoroutine(LoadSiteCoroutine(targetSite));
        }

        targetSite.transform.SetSiblingIndex(1000);

        yield return StartCoroutine(
            ToolsController.instance.FadeInCanvasGroupCoroutine(targetSite.GetComponent<CanvasGroup>(), siteFadingTime, 1, 0)
        );

        CanvasController.instance.eventSystem.enabled = true;
    }

    public IEnumerator DeActivateSiteCoroutine(string targetSiteID)
    {

        CanvasController.instance.eventSystem.enabled = false;

        Site targetSite = GetSite(targetSiteID);

        Scene scene = SceneManager.GetSceneByName(targetSite.siteID);
        if (scene == null || !scene.isLoaded)
        {
            yield return StartCoroutine(LoadSiteCoroutine(targetSite));
        }

        yield return StartCoroutine(
            ToolsController.instance.FadeOutCanvasGroupCoroutine(targetSite.GetComponent<CanvasGroup>(), siteFadingTime)
        );

        if (targetSite.GetComponent<Canvas>() != null)
        {
            targetSite.GetComponent<Canvas>().enabled = false;
        }
        else
        {
            targetSite.gameObject.SetActive(false);
        }

        CanvasController.instance.eventSystem.enabled = true;
    }

    public void DeActivateSite(string targetSiteID)
    {

        Site targetSite = GetSite(targetSiteID);
        if (targetSite != null)
        {

            if (targetSite.GetComponent<Canvas>() != null)
            {
                targetSite.GetComponent<Canvas>().enabled = false;
            }
            else
            {
                targetSite.gameObject.SetActive(false);
            }
        }
    }

    public bool SiteIsLoading()
    {

        if (isLoading) return true;
        if (!CanvasController.instance.eventSystem.enabled) return true;
        return false;
    }
}
