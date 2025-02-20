using RhythmVisualizatorPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Example usage for Stream Songs
public class StreamSongFromURL : MonoBehaviour
{
    public string streamURL = "http://amachamusic.chagasi.com/mp3/suisounishizumutsuki.mp3";
    public AudioSource outputAudioSource;

    void Start()
    {

#if UNITY_WINDOWS || UNITY_EDITOR
        // Import song from any platform [1-2 sec to convert the song to MP3, doesn't not freeze the game]
        ImportSong.instance.GetAudioClip(streamURL, OnSucess, OnError, null);
#else     
         // Import from any platform (except Windows because it doesn't support MP3 import) [Fastest but freeze the game 0.2 sec]
        ImportSong.instance.GetAudioClipMobiles(streamURL, OnSucess, OnError, null);
#endif
    }

    private void OnSucess(AudioClip audioClip, object[] parameters)
    {
        FindObjectOfType<MusicPlayerBasic>().SendSong(audioClip);
    }

    private void OnError(string error)
    {
        Debug.LogWarning(error);
    }

}
