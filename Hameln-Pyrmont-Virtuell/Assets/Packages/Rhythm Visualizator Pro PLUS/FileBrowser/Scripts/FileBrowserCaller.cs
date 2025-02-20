// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

using UnityEngine;

namespace RhythmVisualizatorPro.FileBrowser.Scripts {

	public class FileBrowserCaller : MonoBehaviour {

		// Use the file browser prefab
		public GameObject FileBrowserPrefab;

		// Define a file extension
		public string[] FileExtensions;

        public GameObject canvas;

        void Start() {

			if (canvas == null) {
				Debug.LogError("There is no Canvas assigned");
			}
		}

        // Open a file browser load files
        public void OpenFileBrowser()
        {
            // Create the file browser and name it
            GameObject fileBrowserObject = Instantiate(FileBrowserPrefab, transform);
            fileBrowserObject.name = "FileBrowser";

            FileBrowser fileBrowserScript = fileBrowserObject.GetComponent<FileBrowser>();
            fileBrowserScript.SetupFileBrowser();

            fileBrowserScript.OpenFilePanel(FileExtensions);

        }
	}
}