using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[InitializeOnLoad]
#endif

public class StartUp
{

#if UNITY_EDITOR

    static StartUp()
    {
        PlayerSettings.Android.keystoreName = Application.dataPath + "/_Stadtrundgang-Hameln/Keystore/hameln-keystore.keystore";
		PlayerSettings.keystorePass = "vx34jzu6hbsdsd";
		PlayerSettings.keyaliasPass = "vx34jzu6hbsdsd";
    }

#endif
}
