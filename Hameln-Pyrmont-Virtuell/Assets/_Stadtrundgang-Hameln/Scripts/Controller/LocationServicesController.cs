using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LocationServicesController : MonoBehaviour
{
    public UnityEvent OnHasLocationPermissionAndLocationServicesEnabled;
    public UnityEvent OnLocationPermissionDeniedOrLocationServicesDisabled;

    private bool hasForwardedToSettings = false;
    private bool isRequestingLocationPermission = false;

    public static LocationServicesController instance;
    void Awake()
    {
        instance = this; 
    }

    public IEnumerator ValidateLocationPermissionCoroutine()
    {
        bool has_Location_Permission = PermissionController.instance.HasPermissionLocation();
        bool location_ServicesEnabled = PermissionController.instance.LocationServiceEnabled();

        if (has_Location_Permission && location_ServicesEnabled)
        {
            OnHasLocationPermissionAndLocationServicesEnabled.Invoke();
        }
        else
        {
            if (!location_ServicesEnabled)
            {
                InfoController.instance.ShowCommitAbortDialog("Standortfreigabe", "Bitte erlaube in den Einstellungen den Zugriff auf Deinen Standort, damit wir Dir Deine Position in der Karte anzeigen können.", ForwardToLocationServiceSettings, OnEnableLocationServicesDenied);
            }
            else
            {
                ValidateLocationPermission();
            }
        }

        yield return null;
    }

    public void ValidateLocationPermission()
    {
        bool has_Location_Permission = PermissionController.instance.HasPermissionLocation();
        if (has_Location_Permission)
        {
            OnHasLocationPermissionAndLocationServicesEnabled.Invoke();
        }
        else
        {
            bool iOS_Permission_Asked = false;
            bool android_Permission_Permanently_Denied = PlayerPrefs.GetInt("locationPermissionPermanentlyDenied") == 1;

#if UNITY_IOS && !UNITY_EDITOR
	        string iOSPermissionStatus = PermissionController.instance.GetPermissionLocationStatus();
	        if(iOSPermissionStatus != "kCLAuthorizationStatusNotDetermined"){ iOS_Permission_Asked = true; }
	        print("ValidateLocationPermissionCoroutine iOS " + iOSPermissionStatus);
#endif

            bool forwardToSettings =
                (Application.platform == RuntimePlatform.IPhonePlayer && iOS_Permission_Asked) ||
                (Application.platform == RuntimePlatform.Android && android_Permission_Permanently_Denied);

            if (!forwardToSettings)
            {
                InfoController.instance.ShowCommitAbortDialog("Standort freigeben", "Bitte erlaube der App den Zugriff auf Deinen Standort, damit sie Dir Deine Position in der Karte anzeigen kann.", RequestLocationPermission, OnLocationPermissionDenied);
            }
            else
            {
                InfoController.instance.ShowCommitAbortDialog("Standort freigeben", "Bitte erlaube der App den Zugriff auf Deinen Standort, damit sie Dir Deine Position in der Karte anzeigen kann.", ForwardToAppSettings, OnLocationPermissionDenied);
            }
        }
    }

    public void ForwardToLocationServiceSettings()
    {
        StartCoroutine(ForwardToLocationServiceSettingsCoroutine());
    }

    public IEnumerator ForwardToLocationServiceSettingsCoroutine()
    {
        hasForwardedToSettings = true;
        PermissionController.instance.DirectToLocationServiceSettings();

        float timer = 3.0f;
        while (hasForwardedToSettings && timer > 0) { yield return null; timer -= Time.deltaTime; }
        yield return new WaitForSeconds(0.5f);

        if (!PermissionController.instance.LocationServiceEnabled())
        {
            print("ForwardToLocationServiceSettingsCoroutine - location services disabled");
            OnEnableLocationServicesDenied();
        }
        else
        {
            print("ForwardToLocationServiceSettingsCoroutine - location services enabled");
            ValidateLocationPermission();
        }
    }

    public void RequestLocationPermission()
    {
        if (isRequestingLocationPermission) return;
        isRequestingLocationPermission = true;
        StartCoroutine(RequestLocationPermissionCoroutine());
    }

    public IEnumerator RequestLocationPermissionCoroutine()
    {
        bool hasPermissionLocation = false;
        int permissionState = 0;
        yield return StartCoroutine(
            PermissionController.instance.RequestLocationPermissionCoroutine((bool success, int state) => {
                hasPermissionLocation = success;
                permissionState = state;
            })
        );

        if (!hasPermissionLocation)
        {
            print("RequestLocationPermissionCoroutine - location permission denied");
            OnLocationPermissionDenied();
        }
        else
        {
            print("RequestLocationPermissionCoroutine - location permission granted");
            OnHasLocationPermissionAndLocationServicesEnabled.Invoke();
        }

        isRequestingLocationPermission = false;
    }

    public void ForwardToAppSettings()
    {
        StartCoroutine(ForwardToAppSettingsCoroutine());
    }

    public IEnumerator ForwardToAppSettingsCoroutine()
    {
        hasForwardedToSettings = true;
        PermissionController.instance.DirectToAppSettings();

        float timer = 3.0f;
        while (hasForwardedToSettings && timer > 0) { yield return null; timer -= Time.deltaTime; }
        yield return new WaitForSeconds(0.5f);

        if (!PermissionController.instance.HasPermissionLocation())
        {
            print("ForwardToAppSettingsCoroutine - location permission disabled");
            OnLocationPermissionDenied();
        }
        else
        {
            print("ForwardToAppSettingsCoroutine - location permission granted");
            OnHasLocationPermissionAndLocationServicesEnabled.Invoke();
        }
    }

    public void OnApplicationPause(bool pause)
    {
        if (hasForwardedToSettings && !pause)
        {
            hasForwardedToSettings = false;
        }
    }

    public void OnLocationPermissionDenied()
    {
        OnLocationPermissionDeniedOrLocationServicesDisabled.Invoke();
    }

    public void OnEnableLocationServicesDenied()
    {
        OnLocationPermissionDeniedOrLocationServicesDisabled.Invoke();
    }
}
