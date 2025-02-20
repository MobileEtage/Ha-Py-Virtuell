using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QualitySettingsController : MonoBehaviour
{
	public TMP_Dropdown qualityLevelsDropDown;
	
    void Start()
    {
	    if( AdminUIController.instance.adminMenuEnabled ){
	    	LoadQualityLevels();
	    }
    }

	public void LoadQualityLevels()
	{
		qualityLevelsDropDown.ClearOptions();
		List<string> m_DropOptions = new List<string>();

		string[] names = QualitySettings.names;
		for (int i = 0; i < names.Length; i++)
		{
			m_DropOptions.Add(names[i]);
		}
		qualityLevelsDropDown.AddOptions(m_DropOptions);
					
		print("Current quality level " + QualitySettings.GetQualityLevel() + " " + names[QualitySettings.GetQualityLevel()]);
		qualityLevelsDropDown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
	}
    
	public void SelectQuality(){
		
		string captionText = qualityLevelsDropDown.captionText.text;
		string[] names = QualitySettings.names;
		for (int i = 0; i < names.Length; i++)
		{
			if (captionText == names[i])
			{
				QualitySettings.SetQualityLevel(i, true);
				print("Current quality level " + QualitySettings.GetQualityLevel() + " " + names[QualitySettings.GetQualityLevel()]);
				break;
			}
		}
	}
}
