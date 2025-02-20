// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

using UnityEngine;
using System;
using System.IO;
using System.Linq;

using SlideShowScrollViewPro;
using RhythmVisualizatorPro.FileBrowser.Scripts.UI;

namespace RhythmVisualizatorPro.FileBrowser.Scripts {

	public class FileBrowser : MonoBehaviour {

		public GameObject FileBrowserUI;

		// Whether files with incompatible extensions should be hidden
		public bool HideIncompatibleFiles;

		// The user interface script for the file browser
		private FileBrowserUI userInterface;

		// Boolean to keep track whether the file browser is open
		private bool isOpen;

		// String used to filter files on name basis 
		private string searchFilter = "";

		// The current path of the file browser
		// Instantiated using the current directory of the Unity Project
		private string currentPath;

		// The currently selected file
		private string currentFile;

		// The name for file to be saved
		private string saveFileName;

		// Location of Android root directory, can be different for different device manufacturers
		private string rootAndroidPath;

		// Stacks to keep track for backward and forward navigation feature
		private readonly FiniteStack<string> _backwardStack = new FiniteStack<string>();

		private readonly FiniteStack<string> _forwardStack = new FiniteStack<string>();

		// String array file extensions to filter results and save new files
		private string[] _fileExtensions;

		// Unity Action Event for closing the file browser
		public event Action OnFileBrowserClose = delegate { };

		// Unity Action Event for selecting a file
		public event Action<string> OnFileSelect = delegate { };

		// Method used to setup the file browser
		public void SetupFileBrowser(string startPath = "") {
            // Find the canvas so UI elements can be added to it
            GameObject canvas = FindObjectOfType<FileBrowserCaller>().canvas;
			// Instantiate the file browser UI using the transform of the canvas
			// Then call the Setup method of the SetupUserInterface class to setup the User Interface using the set values
			if (canvas != null) {
				GameObject fileBrowserUi = Instantiate(FileBrowserUI, canvas.transform, false);
				userInterface = fileBrowserUi.GetComponent<FileBrowserUI>();
				userInterface.Setup(this);
			} else {
				Debug.LogError("Make sure there is a Canvas GameObject present in the Hierarcy (Create UI/Canvas)");
			}

            // If we don't have a current path, set the setup the start path
            if (string.IsNullOrEmpty(SlideShowScrollViewPro_Scroll.currentPath)) {
                SetupPath(startPath);
            }
            else { // Else, set the current path
                SetupPath(SlideShowScrollViewPro_Scroll.currentPath);
            }
        }

		// Sets the current path (Android or other devices)
		// If the given start path is valid, set the current path to start path
		private void SetupPath(string startPath) {
			if (!String.IsNullOrEmpty(startPath) && Directory.Exists(startPath)) {
				currentPath = startPath;
			} else if (IsAndroidPlatform()) {
				SetupAndroidVariables();
				currentPath = rootAndroidPath;
			} else {
				currentPath = Directory.GetCurrentDirectory();
			}
		}

		// Set up Android external storage root directory, else default to Directory.GetCurrentDirectory()
		private void SetupAndroidVariables() {
			rootAndroidPath = GetAndroidExternalFilesDir();
		}

		// Returns the external files directory for Android OS, else default to Directory.GetCurrentDirectory()
		private String GetAndroidExternalFilesDir() {
			string path = "";
			if (IsAndroidPlatform()) {
				try {
					using (AndroidJavaClass androidJavaClass = new AndroidJavaClass("android.os.Environment")) {
						path = androidJavaClass.CallStatic<AndroidJavaObject>("getExternalStorageDirectory")
							.Call<string>("getAbsolutePath");
					}
				}
				catch (Exception e) {
					Debug.LogWarning("Error fetching native Android external storage dir: " + e.Message);
					path = Directory.GetCurrentDirectory();
				}
			}

			return path;
		}

		// Returns whether the file browser is open
		public bool IsOpen() {
			return isOpen;
		}

		// Returns to the previously selected directory (inverse of DirectoryForward)
		public void DirectoryBackward() {
			// See if there is anything on the backward stack
			if (_backwardStack.Count > 0) {
				// If so, push it to the forward stack
				_forwardStack.Push(currentPath);
			}

			// Get the last path entry
			string backPath = _backwardStack.Pop();
			if (backPath != null) {
				// Set path and update the file browser
				currentPath = backPath;
				UpdateFileBrowser();
			}
		}

		// Goes forward to the previously selected directory (inverse of DirectoryBackward)
		public void DirectoryForward() {
			// See if there is anything on the redo stack
			if (_forwardStack.Count > 0) {
				// If so, push it to the backward stack
				_backwardStack.Push(currentPath);
			}

			// Get the last level entry
			string forwardPath = _forwardStack.Pop();
			if (forwardPath != null) {
				// Set path and update the file browser
				currentPath = forwardPath;
				UpdateFileBrowser();
			}
		}

		// Moves one directory up and update file browser
		// When there is no parent, show the drives of the computer
		public void DirectoryUp() {
			_backwardStack.Push(currentPath);
			if (!IsTopLevelReached()) {
				currentPath = Directory.GetParent(currentPath).FullName;
				UpdateFileBrowser();
			} else {
				UpdateFileBrowser(true);
			}
		}

		// Parent directory check as Android throws a permission error if it tries to go above the root external storage directory
		private bool IsTopLevelReached() {
			if (IsAndroidPlatform()) {
				return Directory.GetParent(currentPath).FullName == Directory.GetParent(rootAndroidPath).FullName;
			}

			return Directory.GetParent(currentPath) == null;
		}

		// Sends event on file browser close
		// Then destroys the file browser
		public void CloseFileBrowser() {
			OnFileBrowserClose();
			Destroy();
		}

        public void CloseAndSaveFileBrowser()
        {
            OnFileBrowserClose();
            SlideShowScrollViewPro_Scroll.currentPath = currentPath;
            SlideShowScrollViewPro_Scroll.instance.Restart();
            Destroy();
        }

		public void LoadSong()
		{
			OnFileBrowserClose();
			SlideShowScrollViewPro_Scroll.currentPath = currentPath;
			SlideShowScrollViewPro_Scroll.instance.Restart();
			Destroy();
		}

        // When a file is selected (save/load button clicked), 
        // send an event
        public void SelectFile() {
			SendFileSelectEvent(currentFile);		
		}

		// Sends event on file select using path
		// Then destroys the file browser
		private void SendFileSelectEvent(string path) {
			OnFileSelect(path);
			Destroy();
		}

		// Checks the current value of the InputField. If it is an empty string, disable the save button
		public void CheckValidFileName(string inputFieldValue) {
			userInterface.ToggleSelectFileButton(inputFieldValue != "");
		}

		// Updates the search filter and filters the UI
		public void UpdateSearchFilter(string searchFilter) {
			this.searchFilter = searchFilter;
			UpdateFileBrowser();
		}

		// Updates the file browser by updating the path, file name, directories and files
		private void UpdateFileBrowser(bool topLevel = false) {
			UpdatePathText();
			userInterface.ResetParents();
			BuildDirectories(topLevel);
			BuildFiles();
		}

		// Updates the path text
		private void UpdatePathText() {
			userInterface.UpdatePathText(currentPath);
		}

        // Creates a DirectoryButton for each directory in the current path
        private void BuildDirectories(bool topLevel) {

            // Get the directories that are not hidden
            DirectoryInfo directory = new DirectoryInfo(currentPath);
            string[] directories = directory.GetDirectories().Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)).Select(f => f.FullName).ToArray();

            // If the top level is reached return the drives
            if (topLevel) {
				if (IsWindowsPlatform()) {
					directories = Directory.GetLogicalDrives();
				} else if (IsMacOsPlatform()) {
					directories = Directory.GetDirectories("/Volumes");
				} else if (IsAndroidPlatform()) {
					currentPath = rootAndroidPath;
					directories = Directory.GetDirectories(currentPath);
				}
			}


            // Apply Alphanumeric sort to directories
            Array.Sort(directories, new AlphanumComparatorFast());

            // For each directory in the current directory, create a DirectoryButton and hook up the DirectoryClick method
            foreach (string dir in directories) {
				if (Directory.Exists(dir)) {
					userInterface.CreateDirectoryButton(dir);
				}
			}
		}

		// Returns whether the application is run on a Windows Operating System
		private bool IsWindowsPlatform() {
			return (Application.platform == RuntimePlatform.WindowsEditor ||
			        Application.platform == RuntimePlatform.WindowsPlayer);
		}

		private bool IsAndroidPlatform() {
			return Application.platform == RuntimePlatform.Android;
		}

		// Returns whether the application is run on a Mac Operating System
		private bool IsMacOsPlatform() {
			return (Application.platform == RuntimePlatform.OSXEditor ||
			        Application.platform == RuntimePlatform.OSXPlayer);
		}

		// Creates a FileButton for each file in the current path
		private void BuildFiles() {
            // Get the files that are not hidden
            DirectoryInfo directory = new DirectoryInfo(currentPath);
            string[] files = directory.GetFiles().Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)).Select(f => f.FullName).ToArray();

            // Apply search filter when not empty
            if (!String.IsNullOrEmpty(searchFilter)) {
				files = ApplyFileSearchFilter(files);
			}

            // Apply Alphanumeric sort to files
            Array.Sort(files, new AlphanumComparatorFast());

            // For each file in the current directory, create a FileButton and hook up the FileClick method
            foreach (string file in files) {
				if (!File.Exists(file)) return;
				// Hide files (no button) with incompatible file extensions when enabled
				if (!HideIncompatibleFiles)
					userInterface.CreateFileButton(file);
				else {
					if (CompatibleFileExtension(file)) {
						userInterface.CreateFileButton(file);
					}
				}
			}
		}

		// Apply search filter to string array of files and return filtered string array
		private string[] ApplyFileSearchFilter(string[] files) {
			// Keep files that whose name contains the search filter text
			return files.Where(file =>
				(!String.IsNullOrEmpty(file) &&
				 Path.GetFileName(file).IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
		}

		// Returns whether the file given is compatible (correct file extension)
		public bool CompatibleFileExtension(string file) {
			// Empty array, no filter
			if (_fileExtensions.Length == 0) {
				return true;
			}

			// Else check each file extension in file extensions array
			foreach (string fileExtension in _fileExtensions) {
				if (file.EndsWith("." + fileExtension)) {
					return true;
				}

			}

			// Not found, return not compatible
			return false;
		}

		// When a directory is clicked, update the path and the file browser
		public void DirectoryClick(string path) {
			_backwardStack.Push(currentPath.Clone() as string);
			currentPath = path;
			UpdateFileBrowser();
		}

		// When a file is click, validate and update the save file text or current file and update the file browser
		public void FileClick(string clickedFile) {
			// When in save mode, update the save name to the clicked file name

			currentFile = clickedFile;
			

			UpdateFileBrowser();
		}

		// Opens a file browser in load mode
		// Requires a file extension used to filter the loadable files
		public void OpenFilePanel(string[] fileExtensions) {
			// Make sure the file extensions are not invalid, else set it to an empty array (no filter for load)
			if (fileExtensions == null || fileExtensions.Length == 0) {
				fileExtensions = new string[0];
			}

			FilePanel(fileExtensions);
		}

		// Generic file browser panel to remove duplicate code
		private void FilePanel(string[] fileExtensions) {
			// Set _isOpen
			isOpen = true;
			// Set values
			_fileExtensions = fileExtensions;
			// Call update once to set all files for initial directory
			UpdateFileBrowser();
		}

		// Destroy this file browser (the UI and the GameObject)
		private void Destroy() {
			isOpen = false;
			Destroy(GameObject.Find("FileBrowserUI"));
			Destroy(GameObject.Find("FileBrowser"));
		}
	}
}