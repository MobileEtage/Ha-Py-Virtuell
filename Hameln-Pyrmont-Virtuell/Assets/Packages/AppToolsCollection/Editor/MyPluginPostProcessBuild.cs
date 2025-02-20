using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

using System.IO;
using System.Collections;
using System.Collections.Generic;

public class MyPluginPostProcessBuild
{
#if UNITY_IOS
	[PostProcessBuild(1000)]
	public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
	{
		if ( buildTarget == BuildTarget.iOS )
		{
			string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
			PBXProject proj = new PBXProject();
			proj.ReadFromString(File.ReadAllText(projPath));
			//string targetGUID = proj.GetUnityMainTargetGuid();
			string targetGUID = proj.GetUnityMainTargetGuid();
			string targetGUIDUnityFramework = proj.GetUnityFrameworkTargetGuid();

			// Add ProjectCapabilities PushNotifications
			//var projCapability = new ProjectCapabilityManager(projPath, "ios.entitlements", "Unity-iPhone", targetGUID);
			/*
			var projCapability = new ProjectCapabilityManager(projPath, "ios.entitlements", "Unity-iPhone");
			projCapability.AddPushNotifications(false);			
			projCapability.AddSignInWithApple();			
			projCapability.AddAccessWiFiInformation();			
			projCapability.WriteToFile();
			
			// Add ProjectCapabilities BackgroundModes
			proj = new PBXProject();
			proj.ReadFromString(File.ReadAllText(projPath));
			proj.AddCapability(targetGUID, PBXCapabilityType.BackgroundModes);
			projCapability.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);		
			projCapability.WriteToFile();
			*/
			
			// Add frameworks
			proj.AddFrameworkToProject(targetGUIDUnityFramework, "Photos.framework", true);
			proj.AddFrameworkToProject(targetGUIDUnityFramework, "MediaPlayer.framework", true);

			// Add privacy settings
			string plistPath = pathToBuiltProject + "/Info.plist";
			PlistDocument plist = new PlistDocument();
			plist.ReadFromString(File.ReadAllText(plistPath));
			PlistElementDict rootDict = plist.root;
			rootDict.SetString("CFBundleDisplayName", "Ha-Py Virtuell");	// Leerzeichen <0x2007> oder &#x2007;
			rootDict.SetString("NSCameraUsageDescription", "Der Zugriff auf die Kamera ist erforderlich, damit Du die Augmented-Reality-Funktion nutzen kannst.");
			rootDict.SetString("NSLocationWhenInUseUsageDescription", "Bitte erlaube der App den Zugriff auf Deinen Standort, damit sie Dir Deine Position in der Karte anzeigen kann.");
			rootDict.SetString("NSLocationAlwaysAndWhenInUseUsageDescription", "Bitte erlaube der App den Zugriff auf Deinen Standort, damit sie Dir Deine Position in der Karte anzeigen kann.");
			rootDict.SetString("NSPhotoLibraryAddUsageDescription", "Der Zugriff auf die Fotobibliothek ist notwendig, um Fotos zu speichern.");
			rootDict.SetString("NSPhotoLibraryUsageDescription", "Der Zugriff auf die Fotobibliothek ist notwendig, um Fotos zu speichern.");
			rootDict.SetString("NSMicrophoneUsageDescription", "Der Zugriff auf das Mikrofon ist erforderlich, damit Sie ein Video mit Ton aufnehmen können.");
			rootDict.SetString("NSUserTrackingUsageDescription", "Deine Daten werden verwendet, um Absturzberichte und In-App-Aktivitäten zu sammeln, um die Funktionalitäten und die Benutzerinteraktion zu verbessern.");

			//Firebase
			rootDict.SetBoolean("FIREBASE_ANALYTICS_COLLECTION_ENABLED", false);
			
			// remove exit on suspend if it exists.
			string exitsOnSuspendKey = "UIApplicationExitsOnSuspend";
			if(rootDict.values.ContainsKey(exitsOnSuspendKey)){ rootDict.values.Remove(exitsOnSuspendKey); }	
	
			// Disable Bitcode, because Pods also has it disabled and otherwise we get Build error
			proj.SetBuildProperty(targetGUID, "ENABLE_BITCODE", "false");
			proj.SetBuildProperty(targetGUIDUnityFramework, "ENABLE_BITCODE", "false");
			

			// Add all http allowed domains
			// Firebase is adding NSAllowsArbitraryLoadsInWebContent (which will only allow the defined http domains)
			// Because of that NSAllowsArbitraryLoads does not work	(which will allow all http domains)
			// We need to remove NSAppTransportSecurity node and add only NSAllowsArbitraryLoads
			if (rootDict.values.ContainsKey("NSAppTransportSecurity")) { rootDict.values.Remove("NSAppTransportSecurity"); }	
			if (rootDict ["NSAppTransportSecurity"] == null) { rootDict.CreateDict ("NSAppTransportSecurity"); }
			rootDict ["NSAppTransportSecurity"].AsDict ().SetBoolean ("NSAllowsArbitraryLoads", true);
			
			// Add Custom http allowed domains
			/*
			// Add NSAppTransportSecurity settings
			if (rootDict ["NSAppTransportSecurity"] == null) { rootDict.CreateDict ("NSAppTransportSecurity"); }
			rootDict ["NSAppTransportSecurity"].AsDict ().SetBoolean ("NSAllowsArbitraryLoads", true);
			rootDict ["NSAppTransportSecurity"].AsDict ().SetBoolean ("NSAllowsArbitraryLoadsInWebContent", true);

			var exceptionDomains = rootDict ["NSAppTransportSecurity"].AsDict().CreateDict ("NSExceptionDomains");
			var domain = exceptionDomains.CreateDict ("meta.et4.de");
			domain.SetBoolean ("NSExceptionAllowsInsecureHTTPLoads", true);
			domain.SetBoolean ("NSIncludesSubdomains", true);
			*/
			
			
			// background modes
			//PlistElementArray bgModes = rootDict.CreateArray("UIBackgroundModes");
			//bgModes.AddString("location");
			//bgModes.AddString("fetch");


			// Add localizations (for visible supported languages in app store)			
			string [] languages = { "de", "en" };
			PlistElementArray localizations = rootDict.CreateArray("CFBundleLocalizations");
			for( int i = 0; i < languages.Length; i++ ){ localizations.AddString( languages[i] ); }
			
			// Add custom language plist files
			string localizationFolder = pathToBuiltProject + "/Localizations";
			if( !Directory.Exists(localizationFolder) ){ 
				
				Directory.CreateDirectory( localizationFolder );				
				//proj.AddFileToBuild(targetGUIDUnityFramework, proj.AddFolderReference(localizationFolder, "Localizations", PBXSourceTree.Group));

			}
			
			
			for( int i = 0; i < languages.Length; i++ ){
				
				string folder = Application.dataPath+"/Plugins/iOS/Localizations/" + languages[i] + ".lproj";
				string file = Application.dataPath+"/Plugins/iOS/Localizations/" + languages[i] + ".lproj/InfoPlist.strings";
				if( Directory.Exists( folder ) && File.Exists( file ) ){
					
					string xcodeFolderPath = pathToBuiltProject + "/Localizations/" + languages[i] + ".lproj";
					string xcodeFilePath = pathToBuiltProject + "/Localizations/" + languages[i] + ".lproj/InfoPlist.strings";
					if( !Directory.Exists(xcodeFolderPath) ){ 
						Directory.CreateDirectory( xcodeFolderPath ); 
						//proj.AddFileToBuild(targetGUIDUnityFramework, proj.AddFolderReference(xcodeFolderPath, languages[i] + ".lproj"));
					}
					File.Copy( file, xcodeFilePath, true);
										
					if( File.Exists( xcodeFilePath ) ){
						//proj.AddFileToBuild(targetGUIDUnityFramework, proj.AddFile( xcodeFilePath, languages[i] + ".lproj/InfoPlist.strings"));
					}
				}
			}
			
			
			// Set a custom link flag
			proj.AddBuildProperty(targetGUID, "OTHER_LDFLAGS", "-ObjC");
			
			// Write changes
			File.WriteAllText(projPath, proj.WriteToString());
			File.WriteAllText(plistPath, plist.WriteToString());
		}
	}
#endif
}