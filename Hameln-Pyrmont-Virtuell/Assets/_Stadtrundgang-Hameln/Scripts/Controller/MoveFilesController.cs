using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Diagnostics;

public class MoveFilesController : MonoBehaviour
{
    private List<string> sourceFiles = new List<string>() { 
        //"HowToBodenmarker.mp4",
        //"Images/5c7a3ad4f808818b129fd2e8366e6e8e_256_listPreviewD.png",
        //"Images/5f786dad4a77fe09c2af8a9c9698e498_256_listPreviewH.png",
        //"Images/7fa3277663839a222895a1b21aa0e06d_256_listPreviewF.png",
        //"Images/8b674c8c5953682192e8cd0f8a942678_256_listPreviewA.png",
        //"Images/9a07f85d3db1eb15c91ea163f325246d_256_listPreviewE.png",
        //"Images/24cee4c393c4196164ad8e713aa69b36_256_listPreview6.png",
        //"Images/385e1f4e60c2272ca8e08cc6ced3ef19_256_listPreview2.png",
        //"Images/853bb82aea9dbdb7a1006b522ffad8ec_256_listPreviewC.png",
        //"Images/974a139c56330a33a787f8d4debb7295_256_listPreview14.png",
        //"Images/1038d0b24584bacd9a23addf279a0244_256_listPreview15.png",
        //"Images/6435ae7c6467b38f82f2b76573a81519_256_listPreviewG.png",
        //"Images/7694d117c67aa0f6dbcee0b2fd3bb9c2_256_listPreview13.png",
        //"Images/83071a23ccb008c28a391f4c280e6ec9_256_listPreview11.png",
        //"Images/296643ee6cd8c35f9b606dbd4602def0_256_listPreview7.png",
        //"Images/a52d7a977902768dde94919f7edafcce_256_listPreview4.png",
        //"Images/b1d0fc8831c9c3e138bbee6233f84d7b_256_listPreview3.png",
        //"Images/b6d7354dfa31d9c137eef7eb3fa89fd2_256_listPreview10.png",
        //"Images/b262f7422a92c743d08ee468b0aecb00_256_listPreviewB.png",
        //"Images/b662b3c223d8466cc835042951d0a306_256_listPreview12.png",
        //"Images/c7a7d7344dc1d9bae981b38b94d3601a_1024_AdobeStock_431550300.png",
        //"Images/dae0cb943b2b8e74a851943305a5702f_256_listPreview9.png",
        //"Images/e9c495cf3f9049c4986eb36e7ccab519_256_listPreview8.png",
        //"Images/ed9b10303f41c027f51ffa3e7fefb5ab_256_listPreview5.png",
        //"Images/fdedc9fa25b2a1f6937f23de58aac75a_256_listPreview1.png",
        // "Images/1e41c52ac768d21c2cfba681bfd9e107_2048_7.jpeg",
        // "Images/2f1d761106f5a8190ad49f063a114cdb_1024_mapPreview8.png",
        // "Images/3aed5f009e23d9d5d0cb512fb2439010_1024_mapPreview15.png",
        // "Images/3fbb8cea6ce80580ab73fae9afb77494_1024_mapPreviewH.png",
        // "Images/4bebb55b48ef7b3c5e7a772974dcf0ad_2048_4.jpeg",
        // "Images/5dd05de43adf36eaf359bd34d007fcc7_2048_1.jpeg",
        // "Images/6d6767602b3b46975ecf38f898000604_1024_mapPreviewG.png",
        // "Images/9f56de81078fcc4799ce8ff5e8796de6_1024_mapPreviewD.png",
        // "Images/13fb14a7910d8af055115eee932b28dd_1024_mapPreview3.png",
        // "Images/25cf9b5fbb90283c014e6ac9e4c3c3a2_1024_mapPreviewC.png",
        // "Images/31feb86bd2bf61e7776366f2a32f5e0d_2048_3.jpeg",
        // "Images/122dc02b3c5cb354dfbc99caedd9acfa_2048_2.jpeg",
        // "Images/383c7268eca7384d40dfe5ca71d16890_1024_mapPreview14.png",
        // "Images/700ba4c80654b30a1a29d5425fcd228a_1024_mapPreview9.png",
        // "Images/971d2b39e94b089af96da49608139646_1024_mapPreviewB.png",
        // "Images/05332dec10c9144b57a5c5d032ede03a_1024_mapPreview4.png",
        // "Images/7051eab3bd303f708f9d0c59218c2723_1024_mapPreview10.png",
        // "Images/33754e6e4b63b88d2a86bae73cdfd508_2048_8.jpeg",
        // "Images/45528c8edc5502599ad388360e6f9d72_2048_9.jpeg",
        // "Images/68011d2560c0a42a6ef4e154ffcc3029_1024_mapPreview11.png",
        // "Images/228357fa43170683feda523668b6e0b0_1024_mapPreview7.png",
        // "Images/412550d86840fe82274857aca01b9416_1024_mapPreview13.png",
        // "Images/7716979b24645c23ba88a07c19f27b58_4096_Restaurant.jpg",
        // "Images/9380559d41c64b9a06c88854e2a5f5d3_1024_mapPreview2.png",
        // "Images/668857408e488e3a906dfc6a5f23a246_1024_mapPreviewF.png",
        // "Images/b84d135143f4a78ae5635524ed96e0a8_1024_mapPreview1.png",
        // "Images/b784c052b75f6656773539edaadb67b5_1024_mapPreviewA.png",
        // "Images/c0d033935cd48f5d820b960b16e723a2_1024_mapPreview6.png",
        // "Images/c7a7d7344dc1d9bae981b38b94d3601a_1024_AdobeStock_431550300.png",
        // "Images/cbe948c9cadd279bc9e79832359b39e0_1024_mapPreview5.png",
        // "Images/cbeaf20accca6b328cd4ec2e4f7b3a95_2048_5.jpeg",
        // "Images/dbafa7f9cb36566b34e6927621c0fb6e_2048_6.jpeg",
        // "Images/e3557767380516d46220b151be26e07d_2048_10.jpeg",
        // "Images/fc4b13189ba299ffa509c6dac067c785_1024_mapPreview12.png",
        // "Images/107ec1293e8e54937c910a3deb7c99e9_1024_mapPreviewE.png"
    };

    public static MoveFilesController instance;
    void Awake()
    {
        instance = this;
    }

    public IEnumerator SaveFilesToPersitDataPathCoroutine()
    {
        for (int i = 0; i < sourceFiles.Count; i++)
        {
            string source = Application.streamingAssetsPath + "/" + sourceFiles[i];
            string filename = Path.GetFileName(sourceFiles[i]);
            string destination = Application.persistentDataPath + "/" + filename;

            bool fileExists = File.Exists(source);
#if UNITY_ANDROID
            fileExists = true;
#endif

            if (!File.Exists(destination) && fileExists)
            {
                yield return StartCoroutine(MoveFileFromStreamingAssetsToPersistDataPathCoroutine(source, destination));
            }
            else { print("File already exists " + destination); }
        }
    }

    private void SaveFilesToPersitDataPath()
    {
        for (int i = 0; i < sourceFiles.Count; i++)
        {
            string source = Application.streamingAssetsPath + "/" + sourceFiles[i];
            string filename = Path.GetFileName(sourceFiles[i]);
            string destination = Application.persistentDataPath + "/" + filename;

            bool fileExists = File.Exists(source);
#if UNITY_ANDROID
            fileExists = true;
#endif

            if (!File.Exists(destination) && fileExists)
            {
                MoveFileFromStreamingAssetsToPersistDataPathCoroutine(source, destination);
            }
            else { print("File already exists " + destination); }
        }
    }

    public void MoveFileFromStreamingAssetsToPersistDataPath(string sourcePath, string destinationPath)
    {
        StartCoroutine(MoveFileFromStreamingAssetsToPersistDataPathCoroutine(sourcePath, destinationPath));
    }

    public IEnumerator MoveFileFromStreamingAssetsToPersistDataPathCoroutine(string sourcePath, string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            print("File already exists " + destinationPath);
            yield break;
        }

        bool fileExists = File.Exists(sourcePath);
#if UNITY_ANDROID
        fileExists = true;
#endif

        if (!fileExists)
        {
            print("File not exists in StreamingAssets " + sourcePath);
            yield break;
        }

#if UNITY_IOS || UNITY_EDITOR

        if (File.Exists(sourcePath) && !File.Exists(destinationPath))
        {
            print("Moving file from StreamingAssets to PersistDataPath");
            
			try { File.Copy(sourcePath, destinationPath); }
            catch (Exception e) { print("Could not copy file: " + e.Message); }
        }

        yield return new WaitForEndOfFrame();

        if (!File.Exists(destinationPath))
        {
            print("Could not move file to PersistDataPath " + sourcePath);
            
			try { File.Copy(sourcePath, destinationPath); }
            catch (Exception e) { print("Could not copy file: " + e.Message); }
        }
        else
        {
            print("Successfully moved file to " + destinationPath);
        }

#elif UNITY_ANDROID
					
		UnityWebRequest uwr = UnityWebRequest.Get( sourcePath );
		uwr.downloadHandler = new DownloadHandlerFile( destinationPath );
		uwr.timeout = 30;

		uwr.SendWebRequest();
		while( !uwr.isDone ){
			yield return new WaitForEndOfFrame();
		}
 
		if(uwr.isNetworkError || uwr.isHttpError) {			
			print( "<color=#RR0000>Error " + uwr.error + "</color>" );
		}
		else {		
			print( "<color=#00RR00>File saved " + File.Exists( destinationPath ) + "</color>");
		}
		
		// Same with WWW class
		
		/*
		WWW www = new WWW( sourcePath );
		
		float timer = 30;
		while( !www.isDone && timer > 0 ){
			timer -= Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		
		if( string.IsNullOrEmpty( www.error ) ){			
			File.WriteAllBytes( destinationPath, www.bytes);			
			print( "<color=#00RR00>File saved " + File.Exists( destinationPath ) + "</color>");
		}else{
			print( "<color=#RR0000>Error " + www.error + "</color>" );
		}
		
		www.Dispose ();		
		*/
		
#endif
    }

    /*
    void Start()
    {
        StartCoroutine(MoveFilesFromStreamingAssetsToPersitDataPathCoroutine());
    }

    private IEnumerator MoveFilesFromStreamingAssetsToPersitDataPathCoroutine()
    {
        foreach (var filename in sourceVideos)
        {
            string uri = Application.streamingAssetsPath + "/" + filename;
            string outputFilePath = Application.persistentDataPath + "/" + filename;
            var www = new WWW(uri);
            yield return www;

            Save(www, outputFilePath);
            yield return new WaitForEndOfFrame();
        }
    }

    private void Save(WWW www, string outputPath)
    {
        File.WriteAllBytes(outputPath, www.bytes);

        // Verify that the File has been actually stored
        if (File.Exists(outputPath))
        {
            Debug.Log("File successfully saved at: " + outputPath);
        }
        else
        {
            Debug.Log("Failure!! - File does not exist at: " + outputPath);
        }
    }
    */
}
