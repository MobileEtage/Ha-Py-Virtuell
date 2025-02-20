// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

using RhythmVisualizatorPro;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace SlideShowScrollViewPro
{

    public class MySongCustomData : MonoBehaviour
    {
        public string title;
        public string artist;
        public string genre;

        public string songPath;

        public void LoadSong(Sprite sprite = null)
        {
            Debug.LogFormat("Loading song: {0}", songPath);

            MusicPlayerPlus.instance.actualSongImage.sprite = sprite;

            MusicPlayerPlus.instance.songTitle.text = title;
            MusicPlayerPlus.instance.songArtist.text = artist;

  
#if UNITY_STANDALONE || UNITY_EDITOR
            ImportSong.instance.GetAudioClip(songPath, OnCorrectSongLoad, OnErrorSongLoad);
#else 
            ImportSong.instance.GetAudioClipMobiles(songPath);
#endif

        }

        public void OnCorrectSongLoad(AudioClip _audioClip, object[] parameters = null)
        {
            ImportSong.instance.SetSongData(_audioClip);
        }

        public void OnErrorSongLoad(string message)
        {
            UnityEngine.Debug.LogWarning("Error loading song: " + message);
        }


    }
}