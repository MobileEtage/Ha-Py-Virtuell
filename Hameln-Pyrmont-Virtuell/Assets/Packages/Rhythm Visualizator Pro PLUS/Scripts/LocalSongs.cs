// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

using RhythmVisualizatorPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Example usage of loading a song included in the project.
/// </summary>
public class LocalSongs : MonoBehaviour
{
    public string artist;
    public string title;
    public Sprite sprite;

    public AudioClip audioClip;

    void Start()
    {
        // Send song data
        MusicPlayerBasic.instance.songArtist.text = artist;
        MusicPlayerBasic.instance.songTitle.text = title;

        if (sprite != null)
            MusicPlayerBasic.instance.actualSongImage.sprite = sprite;
        
        // Send audioclip
        MusicPlayerBasic.instance.SendSong(audioClip);

    }
}
