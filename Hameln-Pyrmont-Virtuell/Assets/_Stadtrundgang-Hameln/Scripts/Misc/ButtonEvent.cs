using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonEvent : MonoBehaviour
{
    public string eventName = "";

    public void ExecuteEvent()
    {
        if(eventName == "OpenMenu")
        {
            MenuController.instance.OpenMenu();
        }
        else if (eventName == "TestFeatures")
        {
            TestController.instance.Tap("TestFeatures");
        }
		else if ( eventName == "UnlockAdmin" )
		{
			TestController.instance.Tap( "UnlockAdmin" );
		}
		else if (eventName == "TestAudiothek")
        {
            TestController.instance.Tap("TestAudiothek");
        }
        else if (eventName == "TestAvatarGuide")
        {
            TestController.instance.Tap("TestAvatarGuide");
        }
        else if (eventName == "guide_move_desc")
        {
            if (ARController.instance.arSession.enabled)
            {
                InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.move_guide_desc);
            }
            else
            {
                InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.move_scale_guide_desc);
            }
        }
        else if (eventName == "2d_move_scale_desc")
        {
            InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.move_scale_desc);
        }
        else if (eventName == "2d_move_desc")
        {
            if (ARController.instance.arSession.enabled)
            {
                InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.move_desc);
            }
            else
            {
                InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.move_scale_desc);
            }
        }
        else if (eventName == "3d_move_rotate_scale_desc")
        {
            InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.move_rotate_scale_desc);
        }
        else if (eventName == "3d_move_scale_desc")
        {
            InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.move_scale_desc);
        }
        else if (eventName == "pigeons_desc")
        {
            InfoController.instance.ShowMessage("PLATZIERUNG", LanguageController.hashtag_desc);
        }


        //
        if (eventName == "PlayPauseSpeak")
        {
            if (SpeechController.instance != null) { SpeechController.instance.PlayPauseSpeak(); }
        }
        else if (eventName == "StopSpeak")
        {
            if (SpeechController.instance != null) { SpeechController.instance.StopSpeak(); }
        }
    }
}
