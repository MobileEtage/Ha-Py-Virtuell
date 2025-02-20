using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CloneText : MonoBehaviour
{
    public TextMeshProUGUI labelToCopy;

    void Update()
    {
        GetComponent<TextMeshProUGUI>().text = labelToCopy.text;
    }
}
