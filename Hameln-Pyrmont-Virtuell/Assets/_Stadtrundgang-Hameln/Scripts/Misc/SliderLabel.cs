using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderLabel : MonoBehaviour
{
	public Slider slider;
	public int decimalPlaces = 2;
	public string suffix = "";
	public float factor = 1;

	void Start()
    {
        
    }

    void Update()
    {
		if ( slider == null || GetComponent<TextMeshProUGUI>() == null) return;
		GetComponent<TextMeshProUGUI>().text = (slider.value*factor).ToString("F" + decimalPlaces ) + suffix;
    }
}
