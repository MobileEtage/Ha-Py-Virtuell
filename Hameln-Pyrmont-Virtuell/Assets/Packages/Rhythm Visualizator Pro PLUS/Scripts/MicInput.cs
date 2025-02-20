// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

using UnityEngine;
using UnityEngine.UI;

namespace RhythmVisualizatorPro {

    [RequireComponent(typeof(AudioSource))]
    // This script pass the Mic Data to an Audio Clip
    public class MicInput : MonoBehaviour
    {

        public Text text;

        void Start()
        {
#if !UNITY_WEBGL // Need an external library to use Microphone in WebGL
            AudioSource micAudioSource = GetComponent<AudioSource>();

            micAudioSource.clip = Microphone.Start(null, true, 100, 44100);
            micAudioSource.loop = true;
            while (!(Microphone.GetPosition(null) > 0)) {

            }

            text.text = "Hearing microphone";

            Debug.Log("Microphone activated");

            micAudioSource.Play();
#endif
        }
    }
}