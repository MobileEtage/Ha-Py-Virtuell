// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Linq;
using SlideShowScrollViewPro;

namespace RhythmVisualizatorPro
{
    // Complete version of Music Player, use it to manage imported music with the song selection
    public class MusicPlayerPlus : MonoBehaviour
    {

        [HideInInspector]
        public bool PrimaryMusicPlayer;

        [Header("Assignation")]
        public AudioSource audioSource;
        public Sprite playSprite;
        public Sprite pauseSprite;

        [Header("Music Player UI")]
        public Image actualSongImage;
        public Image playPauseButton;
        public Button openScrollListButton;
        public Button openScrollListImageButton;

        public Text songTitle;
        public Text songArtist;

        public Image playerBar;
        public Slider sliderBar;
        public Text actualTime;
        public Text totalTime;

        int actualPos = 0;

        float amount = 0f;
        bool playing;
        bool active;

        public bool playNextSong = true;

        public bool animateSearch = true;

        public static MusicPlayerPlus instance;

        void Awake()
        {

            if (audioSource == null) {
                audioSource = FindObjectOfType<RhythmVisualizatorPro>().audioSource;
            }

            if (audioSource == null) {
                Debug.Log("Assign an Audio Source to the Music Player script");
                enabled = false;
                return;
            }

            if (instance == null) {
                PrimaryMusicPlayer = true;
                instance = this;
            }
            else {
                PrimaryMusicPlayer = false;
                return;
            }

            active = false;
            playing = false;
        }

        private void Start()
        {
            try {
                actualPos = SlideShowScrollViewPro_Scroll.instance.selectedElementPos;
                openScrollListButton.onClick.AddListener(SlideShowScrollViewPro_Scroll.instance.OpenScrollList);
                openScrollListImageButton.onClick.AddListener(SlideShowScrollViewPro_Scroll.instance.OpenScrollList);
            } catch {
                Debug.LogError("Please assign the prefabs Images Presentation World Space and Downside Canvas to the scene");
            }

            //            ChangeSong(actualPos);
        }

        /// <summary>
        /// Change the song using his ID
        /// </summary>
        /// <param name="pos">Position.</param>
        public void ChangeSong(int pos)
        {
            if (!PrimaryMusicPlayer) {
                instance.ChangeSong(pos);
                return;
            }

            StopSong();

            actualPos = pos;

            SlideShowScrollViewPro_Scroll.instance.activeButtons[pos].GetComponent<MySongElementUI>().mySongCustomData.LoadSong(SlideShowScrollViewPro_Scroll.instance.activeButtons[pos].GetComponent<MySongElementUI>().image.sprite);
        }

        void Update()
        {

            if (!PrimaryMusicPlayer) {
                playPauseButton.sprite = instance.playPauseButton.sprite;
                songArtist.text = instance.songArtist.text;
                songTitle.text = instance.songTitle.text;
                actualTime.text = instance.actualTime.text;
                totalTime.text = instance.totalTime.text;
                playerBar.fillAmount = instance.playerBar.fillAmount;
                active = instance.active;
                playing = instance.playing;
                amount = instance.amount;
                actualPos = instance.actualPos;
                return;
            }

            //if (Input.GetKeyDown(KeyCode.Space)) {
            //    PlayOrPauseSong();
            //}
            //else if (Input.GetKeyDown(KeyCode.Backspace)) {
            //    StopSong();
            //}

            if (active) {
                if (playing) {
                    if (audioSource.isPlaying) {
                        amount = (audioSource.time / audioSource.clip.length);
                        playerBar.fillAmount = amount;

                        actualTime.text = SecondsToMinutesAndSeconds(audioSource.time);

                    }
                    else {
                        active = false;
                        playing = false;

                        if (playNextSong)
                        {
                            NextSong();
                        } else
                        {
                            StopSong();
                        }
                    }
                }
            }
        }
        public string SecondsToMinutesAndSeconds(float value)
        {
            // Calculate duration to show in 00:00 format
            var totalSeconds = value;
            int seconds = (int)(totalSeconds % 60f);
            int minutes = (int)((totalSeconds / 60f) % 60f);

            return minutes + ":" + seconds.ToString("D2");
        }

        /// <summary>
        /// Changes the playback position in the song.
        /// </summary>
        public void ChangePosition()
        {
            if (audioSource.clip != null) {
                active = false;
                audioSource.time = sliderBar.value * audioSource.clip.length;
                playerBar.fillAmount = sliderBar.value;
                active = true;

                actualTime.text = SecondsToMinutesAndSeconds(audioSource.time);
            }
        }

        /// <summary>
        /// Stops the song.
        /// </summary>
        public void StopSong()
        {
            if (!PrimaryMusicPlayer) {
                instance.StopSong();
                return;
            }

            Debug.Log("Stop");
            StopAllCoroutines();
            active = false;
            playing = false;

            actualTime.text = "0:00";

            audioSource.Stop();
            audioSource.time = 0;

            playPauseButton.sprite = playSprite;
            amount = 0f;
            sliderBar.value = 0f;
            playerBar.fillAmount = 0f;
        }

        /// <summary>
        /// Play or pause the song.
        /// </summary>
        public void PlayOrPauseSong()
        {
            if (!PrimaryMusicPlayer) {
                instance.PlayOrPauseSong();
                return;
            }

            if (playing) {
                //	Debug.Log ("Pause");

                active = false;
                playing = false;
                audioSource.Pause();
                playPauseButton.sprite = playSprite;

            }
            else {

                //	Debug.Log ("Play");

                audioSource.Play();
                playPauseButton.sprite = pauseSprite;
                playing = true;
                active = true;
            }
        }

        /// <summary>
        /// Plays the next song.
        /// </summary>
        public void NextSong()
        {
            if (!PrimaryMusicPlayer) {
                instance.NextSong();
                return;
            }

            
            actualPos = --SlideShowScrollViewPro_Scroll.instance.selectedElementPos;

            if (actualPos < 0) {
                actualPos = SlideShowScrollViewPro_Scroll.instance.activeButtons.Count - 1;
                SlideShowScrollViewPro_Scroll.instance.selectedElementPos = SlideShowScrollViewPro_Scroll.instance.activeButtons.Count - 1;
            }

            SlideShowScrollViewPro_Scroll.instance.selectedElementID = SlideShowScrollViewPro_Scroll.instance.activeButtons[actualPos].GetComponent<MySongElementUI>().elementID;

            ChangeSong(actualPos);        
        }

        /// <summary>
        /// Plays the previous song.
        /// </summary>
        public void PreviousSong()
        {
            if (!PrimaryMusicPlayer) {
                instance.PreviousSong();
                return;
            }

            actualPos = ++SlideShowScrollViewPro_Scroll.instance.selectedElementPos;

            if (actualPos > SlideShowScrollViewPro_Scroll.instance.activeButtons.Count - 1) {
                actualPos = 0;
                SlideShowScrollViewPro_Scroll.instance.selectedElementPos = 0;
            }

            SlideShowScrollViewPro_Scroll.instance.selectedElementID = SlideShowScrollViewPro_Scroll.instance.activeButtons[actualPos].GetComponent<MySongElementUI>().elementID;

            ChangeSong(actualPos);
        }   

        public void OpenScrollList ()
        {
            SlideShowScrollViewPro_Scroll.instance.OpenScrollList();
        }
    }

}

public class OrderSongs : IComparer {

	int IComparer.Compare (System.Object x, System.Object y) {
		return((new CaseInsensitiveComparer ()).Compare (((GameObject)x).name, ((GameObject)y).name));
	}
}
