using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SimpleJSON;

public class NavPoint : MonoBehaviour
{
    public JSONNode nodeData;
    public string navPointType = "switchPosition";
    public string id = "";

    public List<GameObject> otherToActivate = new List<GameObject>();
    public int targetIndex = 0;

    public void ActivateOtherNavPoints()
    {
        for (int i = 0; i<otherToActivate.Count; i++) { otherToActivate[i].SetActive(true); }
    }
}
