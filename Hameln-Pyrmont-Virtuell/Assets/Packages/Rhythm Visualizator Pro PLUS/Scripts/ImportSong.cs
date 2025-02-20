// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

//using NAudio.Wave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;

namespace RhythmVisualizatorPro
{

    [DisallowMultipleComponent]
    public class ImportSong : MonoBehaviour
    {

        public static string lastCompletePath;
        public static string lastAudioName;

        static bool converted = false;

        public static ImportSong instance;


        private void Awake()
        {      
            instance = this;
        }

        /// <summary>
        /// Loads MP3 Song directly (use for mobiles only, doesn't work with MP3 in Windows) [very fast but causes freeze during import]
        /// Invokes specified method OnSucess when the audioclip was loaded correctly and OnError when the audioclip failed to load
        /// </summary>
        /// <param name="path"></param>
        public void GetAudioClipMobiles(string SongPath,
                             Action<AudioClip, object[]> OnSucess,
                             Action<string> OnError,
                             params object[] parameters)
        {
            StopAllCoroutines();
            StartCoroutine(IE_GetAudioClipMobiles(SongPath, OnSucess, OnError, parameters));
        }

        IEnumerator IE_GetAudioClipMobiles(string SongPath,
                             Action<AudioClip, object[]> OnSucess,
                             Action<string> OnError,
                             params object[] parameters)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!SongPath.Contains("http://")) {
                SongPath = string.Format("file://{0}", SongPath);
            }
#endif
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(SongPath, AudioType.MPEG)) {

                AsyncOperation request = www.SendWebRequest();

                while (!www.isDone) {
                    Debug.Log("Loading/Downloading song...");
                    yield return null;
                }

                if (!string.IsNullOrEmpty(www.error)) {
                    Debug.Log(www.error);
                    MusicPlayerPlus.instance.audioSource.clip = null;
                    MusicPlayerPlus.instance.songTitle.text = "Error at load MP3 (WWW)";
                    MusicPlayerPlus.instance.songArtist.text = "";
                    OnError(www.error);
                }
                else {
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                    while (myClip.loadState == AudioDataLoadState.Loading) {

                        Debug.Log("Loading");
                        yield return null;
                    }

                    if (myClip.loadState != AudioDataLoadState.Loaded) {
                        MusicPlayerPlus.instance.songTitle.text = "Failed to load MP3";
                        MusicPlayerPlus.instance.songArtist.text = "";

                        yield break;
                    }

                    OnSucess(myClip, parameters);
                    yield break;
                }
            }
        }

        private float[] ConvertByteToFloat(byte[] array)
        {
            float[] floatArr = new float[array.Length / 4];
            for (int i = 0; i < floatArr.Length; i++) {
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(array, i * 4, 4);
                floatArr[i] = BitConverter.ToSingle(array, i * 4);
            }
            return floatArr;
        }

        /// <summary>
        /// Loads MP3 Song directly (use for mobiles only, doesn't work with MP3 in Windows) [very fast but causes freeze during import]
        /// Invokes specified method OnSucess when the audioclip was loaded correctly and OnError when the audioclip failed to load
        /// </summary>
        /// <param name="path"></param>
        public void GetAudioClipMobiles(string SongPath)
        {
            StopAllCoroutines();
            StartCoroutine(IE_GetAudioClipMobiles(SongPath));
        }

        IEnumerator IE_GetAudioClipMobiles(string SongPath)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!SongPath.Contains("http://")) {
                SongPath = string.Format("file://{0}", SongPath);
            }
#endif
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(SongPath, AudioType.MPEG)) {

                AsyncOperation request = www.SendWebRequest();

                while (!www.isDone) {
                    Debug.Log("Loading/Downloading song...");
                    yield return null;
                }

                if (!string.IsNullOrEmpty(www.error)) {
                    Debug.Log(www.error);
                    MusicPlayerPlus.instance.audioSource.clip = null;
                    MusicPlayerPlus.instance.songTitle.text = "Error at load MP3 (WWW)";
                    MusicPlayerPlus.instance.songArtist.text = "";

                }
                else {
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                    while (myClip.loadState == AudioDataLoadState.Loading) {

                        Debug.Log("Loading");
                        yield return null;
                    }

                    if (myClip.loadState != AudioDataLoadState.Loaded) {
                        MusicPlayerPlus.instance.songTitle.text = "Failed to load MP3";
                        MusicPlayerPlus.instance.songArtist.text = "";

                        yield break;
                    }

                    SetSongData(myClip);

                }
            }
        }

        public void SetSongData(AudioClip clip)
        {
            MusicPlayerPlus.instance.StopSong();

            if (clip == null) {
                MusicPlayerPlus.instance.audioSource.clip = null;
                MusicPlayerPlus.instance.songTitle.text = "AudioClip not received";
                MusicPlayerPlus.instance.songArtist.text = "";


                return;
            }
            MusicPlayerPlus.instance.audioSource.clip = clip;

            // Calculate duration to show in 00:00 format
            var totalSeconds = clip.length;
            int seconds = (int)(totalSeconds % 60f);
            int minutes = (int)((totalSeconds / 60f) % 60f);

            MusicPlayerPlus.instance.totalTime.text = minutes + ":" + seconds.ToString("D2");
            MusicPlayerPlus.instance.PlayOrPauseSong();
            Debug.Log("Song changed");

        }

        Coroutine getSongCoroutine;

        /// <summary>
        /// Load song from specified location [any platform] [not very fast but doesn't freeze the application]
        /// Invokes specified method OnSucess when the audioclip was loaded correctly and OnError when the audioclip failed to load
        /// </summary>
        /// <param name = "SongPath"></param>
        /// <param name="OnSucess"></param>
        /// <param name = "OnError"></param>
        /// <param name = "parameters"></param>
        public void GetAudioClip(string SongPath,
                             Action<AudioClip, object[]> OnSucess,
                             Action<string> OnError,
                             params object[] parameters)
        {
            StopAllCoroutines();

            if (getSongCoroutine != null) {
                StopCoroutine(getSongCoroutine);
            }

            getSongCoroutine = StartCoroutine(IE_GetAudioClip(SongPath, OnSucess, OnError, parameters));
        }

        IEnumerator IE_GetAudioClip(string SongPath,
                                Action<AudioClip, object[]> OnSucess,
                                Action<string> OnError,
                                params object[] parameters)
        {
            converted = false;

            string filePath = SongPath;

            if (filePath.Contains("http://")) {
#pragma warning disable CS0618 // El tipo o el miembro están obsoletos
                using (WWW www = new WWW(filePath)) {
#pragma warning restore CS0618 // El tipo o el miembro están obsoletos
                    string @tempPath = Path.GetTempPath() + @"\Rhythm Visualizator Pro\" + Path.GetFileName(filePath);

                    while (!www.isDone) {
                        Debug.Log("Downloading song...");
                        yield return null;
                    }

                    Debug.Log("Song downloaded");

                    Directory.CreateDirectory(Path.GetTempPath() + @"\Rhythm Visualizator Pro\");

                    File.WriteAllBytes(@tempPath, www.bytes);

                    while (IsFileLocked(tempPath)) {
                        Debug.Log("Writing");
                    }

                    Debug.Log("Song Writed");

                    filePath = tempPath;
                }
            }

            if (File.Exists(filePath)) {

                char[] chars = { filePath[filePath.Length - 3], filePath[filePath.Length - 2], filePath[filePath.Length - 1] };

                string ext = new string(chars);

                string songSaveLocation = Path.GetTempPath() + @"\Rhythm Visualizator Pro\" + Path.GetFileNameWithoutExtension(filePath);

                try {
                    if (filePath[filePath.Length - 3] == "mp3"[0]) {
                        songSaveLocation += ".wav";
                        Directory.CreateDirectory(Path.GetTempPath() + @"\Rhythm Visualizator Pro");
                        UnityThreadHelper.CreateThread(() => Mp3ToWav(filePath, songSaveLocation));
                    }
                    else {
                        songSaveLocation += "." + ext;
                        Directory.CreateDirectory(Path.GetTempPath() + @"\Rhythm Visualizator Pro");
                        UnityThreadHelper.CreateThread(() => System.IO.File.WriteAllBytes(songSaveLocation, System.IO.File.ReadAllBytes(filePath)));
                        converted = true;
                    }
                }
                catch {
                    OnError("Error on import");
                    Debug.LogWarning("Error on import: " + filePath);

                    yield break;
                }

                while (!converted) {
                    //				Debug.Log ("Converting Song...");
                    yield return null;
                }

#pragma warning disable CS0618 // El tipo o el miembro están obsoletos
                WWW www = new WWW("file://" + songSaveLocation);
#pragma warning restore CS0618 // El tipo o el miembro están obsoletos
                AudioClip a = www.GetAudioClip(false, false, AudioType.WAV);

                a.name = filePath;
                a.LoadAudioData();

                while (a.loadState == AudioDataLoadState.Loading)
                {
                    yield return new WaitForEndOfFrame();
                }

#pragma warning disable 618
                while (!www.isDone)
#pragma warning restore 618
                {
                    //				Debug.Log ("Loading Song...");
                    yield return www;
                }

                if (!a.isReadyToPlay)
                {
                    
                }

                if (a == null) {
                    Debug.LogWarning("Error loading song: " + filePath);
                    yield break;
                }

                if (a.loadState == AudioDataLoadState.Unloaded || a.loadState == AudioDataLoadState.Failed) {
                    Debug.LogWarningFormat("Error loading song: Message {0} Location: {1} ", a.loadState.ToString(), filePath);
                    yield break;
                }

                lastCompletePath = filePath;
                lastAudioName = Path.GetFileName(lastCompletePath);

                OnSucess(a, parameters);
            }
            else {
                Debug.LogWarning("The file doesn't exists: " + filePath);
                OnError("The file doesn't exists");
            }

            yield break;
        }

        static string previousSaved = "";

        /// <summary>
        /// Converts a MP3 file to WAV
        /// </summary>
        /// <param name="mp3File">Mp3 file.</param>
        /// <param name="outputFile">Output file.</param>
        public static void Mp3ToWav(string mp3File, string outputFile)
        {

            if (previousSaved != "") {
                FileSafety.FileDelete(previousSaved);
            }

            //Mp3FileReader reader = new Mp3FileReader(mp3File);

            //WaveFileWriter.CreateWaveFile(outputFile, reader);

            previousSaved = outputFile;

            converted = true;

        }

        private bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        const int ERROR_SHARING_VIOLATION = 32;
        const int ERROR_LOCK_VIOLATION = 33;
        private bool IsFileLocked(string file)
        {
            //check that problem is not in destination file
            if (File.Exists(file) == true) {
                FileStream stream = null;
                try {
                    stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                catch (Exception ex2) {
                    //_log.WriteLog(ex2, "Error in checking whether file is locked " + file);
                    int errorCode = Marshal.GetHRForException(ex2) & ((1 << 16) - 1);
                    if ((ex2 is IOException) && (errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION)) {
                        return true;
                    }
                }
                finally {
                    if (stream != null)
                        stream.Close();
                }
            }
            return false;
        }

        private void OnApplicationQuit()
        {
            try {
                Directory.Delete(Path.GetTempPath() + @"\Rhythm Visualizator Pro\", true);
            }
            catch {

            }
        }
    }
}
