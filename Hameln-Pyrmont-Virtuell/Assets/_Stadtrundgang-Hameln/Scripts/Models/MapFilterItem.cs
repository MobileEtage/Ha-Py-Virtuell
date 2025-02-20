using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapFilterItem : MonoBehaviour
{
    public Image buttonBackground;
    public Image buttonIcon;
    public GameObject selection;
    public string filterType = "";
    public string filterTitle = "";
    public bool isActive = false;
    public bool isLoaded = false;
}
