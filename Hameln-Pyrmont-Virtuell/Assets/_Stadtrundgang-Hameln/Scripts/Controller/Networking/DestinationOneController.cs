using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;

public class DestinationOneController : MonoBehaviour
{
    private bool isDownloading = false;
    public string currentDestinationOneId = "";

    public static DestinationOneController instance;
    void Awake()
    {
        instance = this;
    }

    public IEnumerator GetDataCoroutine(string id, Action<bool, string> Callback)
    {
        print("GetDataCoroutine " + id);
        currentDestinationOneId = id;

        bool isSuccess = false;
        yield return StartCoroutine(
            GetDataByIdCoroutine(id, LanguageController.GetAppLanguageCode(), (bool success, string data) => {

                isSuccess = success;
            })
        );

        if(!isSuccess)
        {
            // Fallback get de data if we are not using de
            if(LanguageController.GetAppLanguageCode() != "de")
            {
                yield return StartCoroutine(
                    GetDataByIdCoroutine(id, "de", (bool success, string data) => {

                        isSuccess = success;
                    })
                );
            }
        }

        string filePath = Application.persistentDataPath + "/data_" + id + ".json";
        if (File.Exists(filePath)) { 
            Callback(true, File.ReadAllText(filePath)); 
        }
        else
        {
            Callback(false, "");
        }
    }

    public IEnumerator GetDataByIdCoroutine(string id, string lang, Action<bool, string> Callback)
    {
        //http://developer.et4.de/explorer/?samples=true
        //string url = "https://meta.et4.de/rest.ashx/search/?experience=osnabruecker-land&q=id%3A" + id + "&template=ET2014A.json" + "&mkt=" + lang;
        string url = "https://meta.et4.de/rest.ashx/search/?experience=" + Params.destinationOneExperience + "&q=id%3A" + id + "&template=ET2014A.json" + "&mkt=" + lang;


        print("GetDataByIdCoroutine " + url);

        bool isSuccess = false;
        string responseData = "";
        yield return StartCoroutine(
            GetCoroutine(url, (bool success, string data) => {

                isSuccess = success;
                responseData = data;
            })
        );

        if (!isSuccess)
        {
            print("Failed get data, error: " + responseData);
        }
        else
        {

            print("Success get data " + responseData);

            if (string.IsNullOrEmpty(responseData)) { Callback(false, ""); yield break; }

            JSONNode dataJson = JSONNode.Parse(responseData);
            if (dataJson == null) { Callback(false, ""); yield break; }

            if (dataJson["items"] == null) { Callback(false, ""); yield break; }
            if (dataJson["items"].Count <= 0) { Callback(false, ""); yield break; }

            string filePath = Application.persistentDataPath + "/data_" + id + ".json";
            File.WriteAllText(filePath, responseData);
            Callback(true, responseData);
        }
    }

    public IEnumerator GetCoroutine(string backendURL, Action<bool, string> Callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(backendURL))
        {
            /********** Authorization **********/
            //www.SetRequestHeader("Authorization", "123456");


            /********** Use an UploadHandler **********/
            // Drupal Post solution
            //UploadHandler uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes("{}"));
            //uploadHandler.contentType = "application/x-www-form-urlencoded";
            //www.uploadHandler = uploadHandler;



            /********** User-Agent **********/
            //www.SetRequestHeader("User-Agent", "DefaultBrowser");


            /********** Content-Type **********/

            // default
            //www.SetRequestHeader("Content-Type", "application/octet-stream");

            // json header
            www.SetRequestHeader("Content-Type", "application/json");

            // if using base64 string
            //www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");		

            // if using bytes --> form.AddBinaryData 
            //www.SetRequestHeader("Content-Type", "multipart/form-data");			

            // ???
            //www.SetRequestHeader("Content-Type", "text/plain;charset=UTF-8");

            /********** Additonal settings **********/
            //www.timeout = 3600;
            //www.chunkedTransfer = false;
            //www.SetRequestHeader("Accept", "application/json");

            www.SendWebRequest();
            float timer = 180;
            while (!www.isDone && timer > 0)
            {

                timer -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            if (timer <= 0)
            {

                Debug.LogError("Error GetCoroutine, Timeout");
                Callback(false, "");
            }
            else
            {
                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.LogError("Error GetCoroutine: " + www.error);
                    Callback(false, www.error);
                }
                else
                {
                    print("Response: " + www.downloadHandler.text);
                    Callback(true, www.downloadHandler.text);
                }
            }
        }

        isDownloading = false;
    }

    public string GetHotelLink(JSONNode dataJson)
    {
        if (Params.hotelLink == "") return "";

        if (dataJson["type"] != null && dataJson["type"].Value == "Hotel" && dataJson["id"] != null )
        {
            string id = dataJson["id"].Value;
            Guid guidOutput;
            bool isValid = Guid.TryParse(id, out guidOutput);
            if (isValid)
            {
                return Params.hotelLink + id;
            }
            else
            {
                print("Not a valid UUID " + id);
                if (dataJson["web"] != null) { return dataJson["web"].Value; }
            }
        }

        return "";
    }


    public string GetGastroTeaserText(JSONNode dataJson)
    {
        if (dataJson["type"] != null && dataJson["type"].Value == "Gastro" && dataJson["texts"] != null)
        {
            for (int i = 0; i < dataJson["texts"].Count; i++)
            {
                JSONNode nodeData = dataJson["texts"][i];
                if (nodeData["rel"] != null && nodeData["value"] != null)
                {
                    if (nodeData["rel"].Value == "teaser")
                    {
                        if (nodeData["type"] != null && nodeData["type"].Value == "text/plain") return nodeData["value"].Value;
                        else if(nodeData["type"] != null && nodeData["type"].Value == "text/html")
                        {
                            string value = Regex.Replace(nodeData["value"].Value, @"<[^>]+>| ", " ").Trim();
                            return value;
                        }
                    }
                }
            }
        }
        return "";
    }
}
