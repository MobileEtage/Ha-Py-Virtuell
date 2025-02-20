using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleJSON;

public class PoiInfoController : MonoBehaviour
{
    //public DragMenuController dragMenu;
    public ScrollRect infoScrollRect;
    public GameObject infoHolder;
    public GameObject bookButton;
    public List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();

    [Space(10)]

    public Image infoSelectionImage;
    public TextMeshProUGUI infoSelectionDescription;
    public GameObject infoSelection;
    public GameObject infoSelectionButtonHolder;
    public GameObject infoSite;
    public GameObject infoSiteBackButton;

    [Space(10)]

    public GameObject fullScreenView;
    public GameObject fullScreenRoot;
    public GameObject fullScreenImage;

    private List<string> furnishing = new List<string>() 
    {
        "WiFi", "Haustiere willkommen", "Haustiere nicht erlaubt", "PKW-Parkplatz", "Einstellplatz für Fahrräder", "Lift / Aufzug", "Rollstuhlgerecht", "Behindertengerecht", "Fahrradverleih", "Sauna",
        "WiFi", "pets welcome", "pets not allowed", "car parking lot", "parking space for bicycles", "lift / elevator", "wheelchair accessible", "disabled accessible", "bike rental", "sauna",
        "Wifi", "Huisdieren welkom", "Huisdieren niet toegestaan", "Parkeerplaats voor auto´s", "Fietsenstalling", "Lift", "Rolstoel-toegankelijk", "Toegankelijk voor mindervaliden", "Fietsverhuur", "Sauna"
    };

    private Dictionary<string, List<string>> furnishingDict = new Dictionary<string, List<string>>()
    {
        { "Einrichtungen Betrieb", new List<string>(){ "WiFi", "Haustiere willkommen", "Haustiere nicht erlaubt", "PKW-Parkplatz", "Einstellplatz für Fahrräder", "Lift / Aufzug", "Rollstuhlgerecht", "Behindertengerecht", "WiFi", "pets welcome", "pets not allowed", "car parking lot", "parking space for bicycles", "lift / elevator", "wheelchair accessible", "disabled accessible", "Wifi", "Huisdieren welkom", "Huisdieren niet toegestaan", "Parkeerplaats voor auto´s", "Fietsenstalling", "Lift", "Rolstoel-toegankelijk", "Toegankelijk voor mindervaliden"} },
        { "Verleih", new List<string>(){ "Fahrradverleih", "bike rental", "Fietsverhuur" } },
        { "Wellness", new List<string>(){ "Sauna", "sauna", "Sauna" } }
    };

    public bool shouldOpenMenu = false;
    private string bookWebLink = "";
    private bool isLoading = false;
    private bool stripHTML = true;

    public static PoiInfoController instance;
    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        for (int i = 0; i < furnishing.Count; i++){ furnishing[i] = furnishing[i].ToLower(); }
    }

    public IEnumerator LoadInfoCoroutine()
    {
        // Load selection site if more than one info
        JSONNode featureData  = StationController.instance.GetStationFeature("info");
        if (featureData != null && featureData["destinationOneIds"] != null && featureData["destinationOneIds"].Count > 1)
        {
            foreach (Transform child in infoSelectionButtonHolder.transform) { Destroy(child.gameObject); }
            for (int i = 0; i < featureData["destinationOneIds"].Count; i++)
            {
                int index = i;
                GameObject obj = ToolsController.instance.InstantiateObject("Infosite/InfoButton", infoSelectionButtonHolder.transform);
                obj.GetComponentInChildren<Button>().onClick.AddListener(() => LoadInfo(featureData["destinationOneIds"][index]["destinationOneId"].Value));
                obj.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslationFromNode(featureData["destinationOneIds"][index]["title"]);

                // Text
                infoSelectionDescription.text = "";
                if (featureData["description"] != null )
                {
                    infoSelectionDescription.text = LanguageController.GetTranslationFromNode(featureData["description"]);
                }

                // Image
                infoSelectionImage.gameObject.transform.parent.gameObject.SetActive(true);
                if (featureData["imageURL"] != null && featureData["imageURL"].Value != "")
                {
                    ToolsController.instance.ApplyOnlineImage(infoSelectionImage, featureData["imageURL"].Value, true);
                }
                else if (featureData["image"] != null && featureData["image"].Value != "")
                {
                    Sprite sprite = Resources.Load<Sprite>(featureData["image"].Value);
                    infoSelectionImage.sprite = sprite;

                    float height = infoSelectionImage.GetComponent<RectTransform>().rect.height;
                    float aspectRatio = sprite.bounds.size.x / sprite.bounds.size.y;
                    infoSelectionImage.GetComponent<RectTransform>().sizeDelta = new Vector2(aspectRatio * height, height);
                }
                else
                {
                    infoSelectionImage.gameObject.transform.parent.gameObject.SetActive(false);
                }
            }

            infoSite.SetActive(false);
            infoSelection.SetActive(true);
            infoSiteBackButton.SetActive(true);
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("InfoSite"));
            yield return new WaitForSeconds(0.5f);
            ARController.instance.StopARSession();
            ARMenuController.instance.DisableMenu(true);
        }
        else
        {
            if (featureData != null && featureData["destinationOneId"] != null && featureData["destinationOneId"].Value != "")
            {
                infoSiteBackButton.SetActive(false);
                yield return StartCoroutine(LoadInfoCoroutine(featureData["destinationOneId"].Value));
            }
            else if(featureData["title"] != null)
            {
                infoSiteBackButton.SetActive(false);
                yield return StartCoroutine(LoadInfoCoroutine(featureData));
            }
            else
            {
                /*
                JSONNode stationData = StationController.instance.GetStationData();
                if (stationData == null) { OnFailedRetrieveData(); yield break; }
                if (stationData["destinationOneId"] == null) { OnFailedRetrieveData(); yield break; }

                infoSiteBackButton.SetActive(false);
                yield return StartCoroutine(LoadInfoCoroutine(stationData["destinationOneId"].Value));
                */

                OnFailedRetrieveData();
            }
        }
    }

    public IEnumerator LoadInfoCoroutine(JSONNode featureData)
    {
        InfoController.instance.loadingCircle.SetActive(true);
        yield return new WaitForSeconds(0.25f);

        infoSite.SetActive(true);
        infoSelection.SetActive(false);
        InfoController.instance.loadingCircle.SetActive(false);

        LoadFeatureInfo(featureData);

        SpeechController.instance.Init();
        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("InfoSite"));

        yield return new WaitForSeconds(0.5f);
        ARController.instance.StopARSession();
        ARMenuController.instance.DisableMenu(true);

        if (shouldOpenMenu && !ARMenuController.instance.menuIsOpen)
        {
            shouldOpenMenu = false;
            StartCoroutine(ARMenuController.instance.OpenCloseMenuCoroutine());
        }

        isLoading = false;
    }

    public void LoadInfo(string destinationOneId)
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(LoadInfoCoroutine(destinationOneId));
    }

    public IEnumerator LoadInfoCoroutine(string destinationOneId)
    {
        InfoController.instance.loadingCircle.SetActive(true);
        yield return new WaitForSeconds(0.25f);

        bool isSuccess = false;
        string responseData = "";
        yield return StartCoroutine(
            DestinationOneController.instance.GetDataCoroutine(destinationOneId, (bool success, string data) => {
                isSuccess = success;
                responseData = data;
            })
        );

        infoSite.SetActive(true);
        infoSelection.SetActive(false);
        InfoController.instance.loadingCircle.SetActive(false);

        if (isSuccess)
        {
            JSONNode infoData = JSONNode.Parse(responseData);
            if (infoData == null) { OnFailedRetrieveData(); yield break; }

            infoData = infoData["items"][0];
            LoadInfo(infoData);

            SpeechController.instance.Init();
            yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("InfoSite"));

            yield return new WaitForSeconds(0.5f);
            ARController.instance.StopARSession();
            ARMenuController.instance.DisableMenu(true);

            if (shouldOpenMenu && !ARMenuController.instance.menuIsOpen)
            {
                shouldOpenMenu = false;
                StartCoroutine(ARMenuController.instance.OpenCloseMenuCoroutine());
            }
        }
        else
        {
            InfoController.instance.ShowMessage("Es konnten keine Informationen zu dieser Station abgerufen werden. Versuche es später erneut.");
        }

        isLoading = false;
    }

    public void LoadInfo(JSONNode dataJson)
    {
        infoSite.SetActive(true);
        infoSelection.SetActive(false);

        foreach (Transform child in infoHolder.transform){ 
            if (child.name != "AudioSpeechPlayer" && child.name != "BookButton") { Destroy(child.gameObject); } 
        }

        labels.Clear();

        // Title
        if (dataJson["title"] != null) { LoadListElement("title", dataJson["title"].Value); }

        // Main image
        if (dataJson["media_objects"] != null)
        {
            for (int i = 0; i < dataJson["media_objects"].Count; i++)
            {
                JSONNode nodeData = dataJson["media_objects"][i];
                if (nodeData["rel"] != null && nodeData["url"] != null && nodeData["rel"].Value == "default" ) {

                    JSONNode imageData = JSONNode.Parse("{}");
                    imageData["url"] = nodeData["url"].Value;
                    if (nodeData["source"] != null) { imageData["source"] = nodeData["source"].Value; }
                    if (nodeData["license"] != null) { imageData["license"] = nodeData["license"].Value; }
                    if (nodeData["description"] != null) { imageData["description"] = nodeData["description"].Value; }
					else if (nodeData["value"] != null) { imageData["description"] = nodeData["value"].Value; }

                    //LoadListElement("image", nodeData["url"].Value);
                    LoadListElementImage(imageData);
                    if (nodeData["description"] != null || nodeData["value"] != null) { GameObject separator = ToolsController.instance.InstantiateObject("UI/Separator", infoHolder.transform); }
                }
            }
        }

        SpeechController.instance.audioSpeechPlayers[0].transform.SetAsLastSibling();

        // Teaser
        if (dataJson["texts"] != null)
        {
            for (int i = 0; i < dataJson["texts"].Count; i++)
            {
                JSONNode nodeData = dataJson["texts"][i];
                //if (nodeData["type"] != null && nodeData["type"].Value != "text/plain") continue;

                if (nodeData["rel"] != null && nodeData["value"] != null)
                {
                    if (nodeData["rel"].Value == "teaser")
                    {
                        if (nodeData["type"] != null && nodeData["type"].Value != "text/plain") continue;
                        LoadListElement("teaser", nodeData["value"].Value);
                    }
                }
            }
        }

        // Texts
        if (dataJson["texts"] != null) {

            for (int i = 0; i < dataJson["texts"].Count; i++)
            {
                JSONNode nodeData = dataJson["texts"][i];
                //if (nodeData["type"] != null && nodeData["type"].Value != "text/plain") continue;

                if (nodeData["rel"] != null && nodeData["value"] != null ) {

                    if (nodeData["rel"].Value == "details")
                    {
                        //if (nodeData["type"] != null && nodeData["type"].Value != "text/plain") continue;
                        //LoadListElement("text", nodeData["value"].Value);

                        if (nodeData["type"] != null)
                        {
                            if (nodeData["type"].Value == "text/html")
                            {
                                string val = ToolsController.instance.ConvertHTMLToValidTextMeshProText(nodeData["value"].Value);
                                LoadListElement("text", val, false);
                                break;
                            }
                        }
                    }
                    else if (nodeData["rel"].Value == "barrierfree")
                    {
                        //LoadListElementBarrierfree(nodeData["value"].Value);
                    }
                }
            }
        }

        // Address
        if (dataJson["zip"] != null && dataJson["city"] != null && dataJson["street"] != null && dataJson["title"] != null) {

            GameObject line = ToolsController.instance.InstantiateObject("UI/LinePrefab", infoHolder.transform);

            string text = "";
            text += "<b>" + dataJson["title"].Value + "</b>";
            text += "\n" + dataJson["street"].Value + "\n" + dataJson["zip"].Value + " " + dataJson["city"].Value;
            if (dataJson["country"] != null) { text += "\n" + dataJson["country"].Value; }
            if (dataJson["phone"] != null || dataJson["email"] != null) { text += "\n"; }
            if (dataJson["phone"] != null) { text += "\n" + "Tel.: " + dataJson["phone"].Value; }
            if (dataJson["email"] != null) {
                text += "\n" + "E-Mail: " +
                    "<link=\"" + dataJson["email"].Value + "\"><color=#238CAA>" + dataJson["email"].Value + "</color></link>";
            }

            LoadListElementAddress(text); 
        }

        // Features
        LoadFeaturesCategory(dataJson);

        /*
        if (dataJson["features"] != null && dataJson["features"].Count > 0)
        {
            GameObject line = ToolsController.instance.InstantiateObject("UI/LinePrefab", infoHolder.transform);
            GameObject textTmp = ToolsController.instance.InstantiateObject("UI/InfoTeaserPrefab", infoHolder.transform);
            textTmp.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Features");

            string text = "";
            for (int i = 0; i < dataJson["features"].Count; i++)
            {
                text += ("<sprite=1>" + LanguageController.GetTranslation(dataJson["features"][i].Value));
                if (i < (dataJson["features"].Count - 1)) text += "\n";
            }

            LoadListElementFeatures(text);
        }
        */

        // Images
        if (dataJson["media_objects"] != null)
        {
            List<string> images = new List<string>();
            JSONNode imageData = JSONNode.Parse("[]");
            for (int i = 0; i < dataJson["media_objects"].Count; i++)
            {
                JSONNode nodeData = dataJson["media_objects"][i];
                if (nodeData["rel"] != null && nodeData["url"] != null
                    //&& nodeData["rel"].Value == "imagegallery"
                    && nodeData["rel"].Value != "default"
                )
                {

                    images.Add(nodeData["url"].Value);

                    JSONNode node = JSONNode.Parse("{}");
                    node["url"] = nodeData["url"].Value;
                    if (nodeData["source"] != null) { node["source"] = nodeData["source"].Value; }
                    if (nodeData["license"] != null) { node["license"] = nodeData["license"].Value; }
                    if (nodeData["description"] != null) { node["description"] = nodeData["description"].Value; }
					else if (nodeData["value"] != null) { node["description"] = nodeData["value"].Value; }
					
                    imageData.Add(node);

                    if (images.Count >= 10) break;
                }
            }

            if(images.Count > 0)
            {
                GameObject line = ToolsController.instance.InstantiateObject("UI/LinePrefab", infoHolder.transform);
                GameObject textTmp = ToolsController.instance.InstantiateObject("UI/InfoTeaserPrefab", infoHolder.transform);
                textTmp.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Bilder");
            }

            if (images.Count == 1)
            {
                //LoadListElement("image", images[0]);
                LoadListElementImage(imageData[0]);
            }
            else if ( images.Count > 1 )
            {
                //LoadListElementImages(images);
                LoadListElementImages(imageData);
            }
        }

        // Weblink
        bookButton.SetActive(false);
        if (dataJson["type"] != null && dataJson["type"].Value == "Hotel")
        {
            string weblink = DestinationOneController.instance.GetHotelLink(dataJson);
            if(weblink != "") {

                GameObject line = ToolsController.instance.InstantiateObject("UI/LinePrefab", infoHolder.transform);
                line.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(70,70,90,0);
                bookButton.SetActive(true);
                bookButton.transform.SetAsLastSibling();
                bookWebLink = weblink;

                /*
                GameObject textTmp = ToolsController.instance.InstantiateObject("UI/InfoTeaserPrefab", infoHolder.transform);
                textTmp.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Buchung");

                GameObject text = ToolsController.instance.InstantiateObject("UI/InfoTextPrefab", infoHolder.transform);
                text.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Jetzt") + " " +
                    "<link=\"" + weblink + "\"><u><color=#238CAA>" + LanguageController.GetTranslation("hier") + "</color></u></link>" + " " + LanguageController.GetTranslation("buchen");
                */
            }
        }


        infoScrollRect.verticalNormalizedPosition = 1;
    }

    public void LoadFeatureInfo(JSONNode featureData)
    {
        infoSite.SetActive(true);
        infoSelection.SetActive(false);

        foreach (Transform child in infoHolder.transform)
        {
            if (child.name != "AudioSpeechPlayer" && child.name != "BookButton") { Destroy(child.gameObject); }
        }

        labels.Clear();

        // Title
        //if (featureData["title"] != null) { LoadListElement("title", featureData["title"].Value); }
        if (featureData["title"] != null) { LoadListElement("title", LanguageController.GetTranslationFromNode(featureData["title"])); }

        // Main image
        if (featureData["imageURL"] != null)
        {  
            JSONNode imageData = JSONNode.Parse("{}");
            imageData["url"] = featureData["imageURL"].Value;
            if (featureData["copyright"] != null) { imageData["license"] = featureData["copyright"].Value; }
            if (featureData["mainImageDescription"] != null) { imageData["description"] = featureData["mainImageDescription"].Value; }

            LoadListElementImage(imageData);
            if (featureData["mainImageDescription"] != null) { GameObject separator = ToolsController.instance.InstantiateObject("UI/Separator", infoHolder.transform); }              
        }

        SpeechController.instance.audioSpeechPlayers[0].transform.SetAsLastSibling();

        // Teaser
        if (featureData["subTitle"] != null)
        {         
            //LoadListElement("teaser", featureData["subTitle"].Value);
            LoadListElement("teaser", LanguageController.GetTranslationFromNode(featureData["subTitle"]));
        }

        // Description
        if (featureData["description"] != null)
        {
            //LoadListElement("text", featureData["description"].Value);
            LoadListElement("text", LanguageController.GetTranslationFromNode(featureData["description"]));
        }

        // Images
        if (featureData["images"] != null)
        {
            List<string> images = new List<string>();
            JSONNode imageData = JSONNode.Parse("[]");
            for (int i = 0; i < featureData["images"].Count; i++)
            {
                JSONNode nodeData = featureData["images"][i];

                images.Add(nodeData["url"].Value);

                JSONNode node = JSONNode.Parse("{}");
                node["url"] = nodeData["url"].Value;

                if (nodeData["copyright"] != null) { node["license"] = nodeData["copyright"].Value; }

                if (nodeData["description"] != null) {

                    //node["description"] = nodeData["description"].Value;
                    node["description"] = LanguageController.GetTranslationFromNode(nodeData["description"]);
                }

                imageData.Add(node);

                if (images.Count >= 10) break;            
            }

            if (images.Count > 0)
            {
                GameObject line = ToolsController.instance.InstantiateObject("UI/LinePrefab", infoHolder.transform);
                GameObject textTmp = ToolsController.instance.InstantiateObject("UI/InfoTeaserPrefab", infoHolder.transform);
                textTmp.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Bilder");
            }

            if (images.Count == 1)
            {
                LoadListElementImage(imageData[0]);
            }
            else if (images.Count > 1)
            {
                LoadListElementImages(imageData);
            }
        }

        infoScrollRect.verticalNormalizedPosition = 1;
    }

    public void LoadListElement(string infoType, string value, bool shouldStripHTML = true)
    {
        //print(infoType + " " + value);
        
        if(shouldStripHTML) value = Regex.Replace(value, @"<[^>]+>| ", " ").Trim();

        switch (infoType)
        {

            case "title":

                GameObject title = ToolsController.instance.InstantiateObject("UI/InfoTitlePrefab", infoHolder.transform);
                title.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(value, true);
                //labels.Add(title.GetComponentInChildren<TextMeshProUGUI>());

                break;

            case "teaser":

                GameObject teaser = ToolsController.instance.InstantiateObject("UI/InfoTeaserPrefab", infoHolder.transform);
                teaser.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(value, true);
                labels.Add(teaser.GetComponentInChildren<TextMeshProUGUI>());

                break;

            case "text":

                GameObject text = ToolsController.instance.InstantiateObject("UI/InfoTextPrefab", infoHolder.transform);
                text.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(value, true);
                labels.Add(text.GetComponentInChildren<TextMeshProUGUI>());

                break;

            case "image":

                GameObject image = ToolsController.instance.InstantiateObject("UI/InfoImagePrefab", infoHolder.transform);
                GameObject myImage = ToolsController.instance.FindGameObjectByName(image, "Image");
                myImage.GetComponent<UIImage>().url = value;
                image.GetComponentInChildren<Button>().onClick.AddListener(() => SwitchToFullscreen(myImage));

                myImage.GetComponent<UIImage>().adjustHeightOfParent = true;
                //StartCoroutine( HideBeforeLoadedCoroutine( myImage.GetComponent<UIImage>() ) );

                break;
        }
    }

    public void LoadListElementBarrierfree(string value)
    {
        if (stripHTML) value = Regex.Replace(value, @"<[^>]+>| ", " ").Trim();

        GameObject title =
            ToolsController.instance.InstantiateObject("UI/InfoTeaserPrefab", infoHolder.transform);
        title.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Barrierefreiheit");
        //labels.Add(title.GetComponentInChildren<TextMeshProUGUI>());

        GameObject text =
            ToolsController.instance.InstantiateObject("UI/InfoTextPrefab", infoHolder.transform);
        text.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(value, true);
        //labels.Add(text.GetComponentInChildren<TextMeshProUGUI>());
    }

    public void LoadListElementAddress(string value)
    {
        //if (stripHTML) value = Regex.Replace(value, @"<[^>]+>| ", " ").Trim();

        GameObject title =
            ToolsController.instance.InstantiateObject("UI/InfoTeaserPrefab", infoHolder.transform);
        title.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation("Adresse");
        //labels.Add(title.GetComponentInChildren<TextMeshProUGUI>());

        GameObject text =
            ToolsController.instance.InstantiateObject("UI/InfoTextPrefab", infoHolder.transform);
        text.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(value, true);
        //labels.Add(text.GetComponentInChildren<TextMeshProUGUI>());
    }

    public void LoadListElementFeatures(string value)
    {
        if (furnishing.Contains(value.ToLower())) return;

        GameObject text =
            ToolsController.instance.InstantiateObject("UI/InfoTextPrefab", infoHolder.transform);
        text.GetComponentInChildren<TextMeshProUGUI>().text = value;
    }

    public void LoadFeaturesCategory(JSONNode dataJson)
    {
        Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
        dict.Add("Einrichtungen Betrieb", new List<string>());
        dict.Add("Verleih", new List<string>());
        dict.Add("Wellness", new List<string>());

        if (dataJson["features"] != null && dataJson["features"].Count > 0)
        {
            for (int i = 0; i < dataJson["features"].Count; i++)
            {
                string category = GetFurnishingCategory(dataJson["features"][i].Value);
                if(category != "" && dict.ContainsKey(category)) { dict[category].Add(dataJson["features"][i].Value); }
            }
        }

        foreach (KeyValuePair<string, List<string>> pair in dict)
        {
            if(pair.Value.Count > 0)
            {
                GameObject line = ToolsController.instance.InstantiateObject("UI/LinePrefab", infoHolder.transform);
                GameObject textTmp = ToolsController.instance.InstantiateObject("UI/InfoTeaserPrefab", infoHolder.transform);
                textTmp.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(pair.Key);

                string text = "";
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    text += ("<sprite=1>" + LanguageController.GetTranslation(pair.Value[i], true));
                    if (i < (pair.Value.Count - 1)) text += "\n";
                }

                GameObject listElement = ToolsController.instance.InstantiateObject("UI/InfoTextPrefab", infoHolder.transform);
                listElement.GetComponentInChildren<TextMeshProUGUI>().text = text;
            }
        }
    }

    public void LoadFeaturesCategory(JSONNode dataJson, string category)
    {
        List<string> furnishingItems = new List<string>();

        if (dataJson["features"] != null && dataJson["features"].Count > 0)
        {
            for (int i = 0; i < dataJson["features"].Count; i++)
            {
                if (furnishing.Contains(dataJson["features"][i].Value.ToLower())) { furnishingItems.Add(dataJson["features"][i].Value); }
            }
        }

        if (furnishingItems.Count > 0)
        {
            GameObject line = ToolsController.instance.InstantiateObject("UI/LinePrefab", infoHolder.transform);
            GameObject textTmp = ToolsController.instance.InstantiateObject("UI/InfoTeaserPrefab", infoHolder.transform);
            textTmp.GetComponentInChildren<TextMeshProUGUI>().text = LanguageController.GetTranslation(category);

            string text = "";
            for (int i = 0; i < furnishingItems.Count; i++)
            {
                text += ("<sprite=1>" + LanguageController.GetTranslation(furnishingItems[i], true));
                if (i < (furnishingItems.Count - 1)) text += "\n";
            }

            GameObject listElement = ToolsController.instance.InstantiateObject("UI/InfoTextPrefab", infoHolder.transform);
            listElement.GetComponentInChildren<TextMeshProUGUI>().text = text;
        }
    }

    public string GetFurnishingCategory(string item)
    {
        foreach (KeyValuePair<string, List<string>> pair in furnishingDict)
        {
            for (int i = 0; i < pair.Value.Count; i++)
            {
                if (pair.Value[i].ToLower() == item.ToLower()) return pair.Key;
            }
        }
        return "";
    }

    public void LoadListElementImage(JSONNode imageData)
    {
        GameObject image = ToolsController.instance.InstantiateObject("UI/InfoImagePrefab", infoHolder.transform);
        GameObject myImage = ToolsController.instance.FindGameObjectByName(image, "Image");
        myImage.GetComponent<UIImage>().url = imageData["url"].Value; ;
        image.GetComponentInChildren<Button>().onClick.AddListener(() => SwitchToFullscreen(myImage));

        // Add other infos to image
        string copyrightText = "";
        if (imageData["license"] != null && imageData["license"].Value != ""){ copyrightText = imageData["license"].Value + " "; }
        if (imageData["source"] != null && imageData["source"].Value != ""){ copyrightText += imageData["source"].Value; }

        if(copyrightText != "")
        {
            if(image.GetComponentInChildren<GalleryImage>() != null)
            {
                image.GetComponentInChildren<GalleryImage>().SetCopyRight(copyrightText);
            }
        }

        if (imageData["description"] != null && imageData["description"].Value != "") {

            GameObject descriptionLabel = ToolsController.instance.FindGameObjectByName(image, "DescriptionLabel");
            if(descriptionLabel != null) { descriptionLabel.GetComponent<TextMeshProUGUI>().text = imageData["description"].Value; }
        }


        myImage.GetComponent<UIImage>().adjustHeightOfParent = true;
        myImage.GetComponent<UIImage>().shouldEnvelopeParent = true;
        //StartCoroutine( HideBeforeLoadedCoroutine( myImage.GetComponent<UIImage>() ) );
    }

    public void LoadListElementImages(JSONNode imageData)
    {
        GameObject imageSlider = ToolsController.instance.InstantiateObject("UI/InfoImageSlider", infoHolder.transform);
        GameObject imageHolder = ToolsController.instance.FindGameObjectByName(imageSlider, "Content");

        for (int i = 0; i < imageData.Count; i++)
        {
            GameObject image = ToolsController.instance.InstantiateObject("UI/InfoImagePrefab-Slider", imageHolder.transform);
            GameObject myImage = ToolsController.instance.FindGameObjectByName(image, "Image");
            myImage.GetComponent<UIImage>().url = imageData[i]["url"].Value;
            image.GetComponentInChildren<Button>().onClick.AddListener(() => SwitchToFullscreen(myImage));

            // Add other infos to image
            string copyrightText = "";
            if (imageData[i]["license"] != null && imageData[i]["license"].Value != "") { copyrightText = imageData[i]["license"].Value + " "; }
            if (imageData[i]["source"] != null && imageData[i]["source"].Value != "") { copyrightText += imageData[i]["source"].Value; }

            if (copyrightText != "")
            {
                if (image.GetComponentInChildren<GalleryImage>() != null)
                {
                    image.GetComponentInChildren<GalleryImage>().SetCopyRight(copyrightText);
                }
            }

            if (imageData[i]["description"] != null && imageData[i]["description"].Value != "")
            {
                GameObject descriptionLabel = ToolsController.instance.FindGameObjectByName(image, "DescriptionLabel");
                if (descriptionLabel != null) { descriptionLabel.GetComponent<TextMeshProUGUI>().text = imageData[i]["description"].Value; }
            }

            myImage.GetComponent<UIImage>().shouldEnvelopeParent = true;
	        //myImage.GetComponent<UIImage>().adjustHeightOfParent = true;
	        //StartCoroutine( HideBeforeLoadedCoroutine( myImage.GetComponent<UIImage>() ) );
        }
    }

    public void LoadListElementImages(List<string> images)
    {
        GameObject imageSlider = ToolsController.instance.InstantiateObject("UI/InfoImageSlider", infoHolder.transform);
        GameObject imageHolder = ToolsController.instance.FindGameObjectByName(imageSlider, "Content");

        for (int i = 0; i < images.Count; i++)
        {
            GameObject image =
                ToolsController.instance.InstantiateObject("UI/InfoImagePrefab-Slider", imageHolder.transform);
            GameObject myImage = ToolsController.instance.FindGameObjectByName(image, "Image");
            myImage.GetComponent<UIImage>().url = images[i];
            image.GetComponentInChildren<Button>().onClick.AddListener(() => SwitchToFullscreen(myImage));

            myImage.GetComponent<UIImage>().adjustHeightOfParent = true;
            //StartCoroutine( HideBeforeLoadedCoroutine( myImage.GetComponent<UIImage>() ) );
        }
    }

    public void SwitchToFullscreen(GameObject imageObj)
    {
        if (imageObj.GetComponent<Image>() == null) return;
        if (imageObj.GetComponent<Image>().sprite == null) return;

        fullScreenView.SetActive(true);
        fullScreenImage.GetComponent<Image>().sprite = imageObj.GetComponent<Image>().sprite;

        float aspect = fullScreenImage.GetComponent<Image>().sprite.rect.width / fullScreenImage.GetComponent<Image>().sprite.rect.height;
        fullScreenImage.GetComponent<AspectRatioFitter>().aspectRatio = aspect;
        fullScreenRoot.GetComponent<AspectRatioFitter>().aspectRatio = aspect;

        if(imageObj.GetComponentInParent<GalleryImage>(true) != null)
        {
            fullScreenView.GetComponentInChildren<GalleryImage>().SetCopyRight(imageObj.GetComponentInParent<GalleryImage>(true).copyrightText);
        }
    }

    public void OpenBookLink()
    {
        ToolsController.instance.OpenWebView(bookWebLink);
    }

    public IEnumerator HideBeforeLoadedCoroutine(UIImage uiImage)
    {
        uiImage.checkSpriteExits = true;
        uiImage.spriteLoaded = false;

        yield return new WaitForEndOfFrame();

        uiImage.HideBeforeLoaded();
    }

    public void OnFailedRetrieveData()
    {
        InfoController.instance.ShowMessage("Informationen konnten nicht abgerufen werden. Versuche es später nochmal.");
    }

    public void Back()
    {
        //InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation);

        if (isLoading) return;
        isLoading = true;
        StartCoroutine(BackCoroutine());
    }

    public IEnumerator BackCoroutine()
    {
        print("BackCoroutine");

        SpeechController.instance.Reset();
        if (MapFilterController.instance.didClickedOnFilterStation)
        {
            MapFilterController.instance.didClickedOnFilterStation = false;
	        yield return StartCoroutine(SiteController.instance.SwitchToSiteCoroutine("MapSite"));
	        ARMenuController.instance.StopFeatures();
        }
        else
        {
            InfoController.instance.ShowCommitAbortDialog("STATION VERLASSEN", LanguageController.cancelCurrentStationText, ScanController.instance.CommitCloseStation);
        }

        isLoading = false;
    }

    public void Reset()
	{
        foreach (Transform child in infoHolder.transform)
        {
            if (child.name != "AudioSpeechPlayer" && child.name != "BookButton") { Destroy(child.gameObject); }
        }
        
        labels.Clear();
        infoSite.SetActive(false);
        infoSelection.SetActive(false);
        infoSiteBackButton.SetActive(false);

        SpeechController.instance.Reset();
    }
}
