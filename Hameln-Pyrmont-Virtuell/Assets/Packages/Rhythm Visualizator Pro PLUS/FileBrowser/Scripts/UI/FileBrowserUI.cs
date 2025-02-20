using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

namespace RhythmVisualizatorPro.FileBrowser.Scripts.UI {

	public abstract class FileBrowserUI : MonoBehaviour {

		// Button Prefab used to create a button for each directory in the current path
		public GameObject DirectoryButtonPrefab;

		// Button Prefab used to create a button for each file in the current path
		public GameObject FileButtonPrefab;

		// The file browser using this user interface
		private FileBrowser _fileBrowser;

		// Button used to select a file to save/load
		private GameObject _selectFileButton;

		// Game object that represents the current path
		private GameObject _pathText;

		// Game object used as the parent for all the Directories of the current path
		protected GameObject DirectoriesParent;

		// Game object used as the parent for all the Files of the current path
		protected GameObject FilesParent;

		// Input field and variable to allow file search
		private InputField _searchInputField;

		// Setup the file browser user interface
		public void Setup(FileBrowser fileBrowser) {
			_fileBrowser = fileBrowser;
			name = "FileBrowserUI";
			SetupClickListeners();
			SetupTextLabels();
			SetupParents();
			SetupSearchInputField();
		}

		// Setup click listeners for buttons
		private void SetupClickListeners() {
			// Hook up Directory Navigation methods to Directory Navigation Buttons
			Utilities.FindButtonAndAddOnClickListener("DirectoryBackButton", _fileBrowser.DirectoryBackward);
			Utilities.FindButtonAndAddOnClickListener("DirectoryForwardButton", _fileBrowser.DirectoryForward);
			Utilities.FindButtonAndAddOnClickListener("DirectoryUpButton", _fileBrowser.DirectoryUp);

			// Hook up CloseFileBrowser method to CloseFileBrowserButton
			Utilities.FindButtonAndAddOnClickListener("CloseFileBrowserButton", _fileBrowser.CloseFileBrowser);
			// Hook up SelectFile method to SelectFileButton
			_selectFileButton = Utilities.FindButtonAndAddOnClickListener("SelectFileButton", _fileBrowser.SelectFile);
		}

		// Setup path, load and save file text
		private void SetupTextLabels() {

			// Find pathText game object to update path on clicks
			_pathText = Utilities.FindGameObjectOrError("PathText");
		}

		// Setup parents object to hold directories and files
		protected abstract void SetupParents();

		// Setup search filter
		private void SetupSearchInputField() {
			// Find search input field and get input field component
			// and hook up onValueChanged listener to update search results on value change
			_searchInputField = Utilities.FindGameObjectOrError("SearchInputField").GetComponent<InputField>();

			_searchInputField.onValueChanged.AddListener(_fileBrowser.UpdateSearchFilter);
		}

		// Toggles the SelectFileButton to ensure valid file names during save
		public void ToggleSelectFileButton(bool enable) {
			_selectFileButton.SetActive(enable);
		}

		// Update the path text
		public void UpdatePathText(string newPath) {
			_pathText.GetComponent<Text>().text = newPath;
		}

		// Resets the directories and files parent game objects
		public void ResetParents() {
			ResetParent(DirectoriesParent);
			ResetParent(FilesParent);
		}

		// Removes all current game objects under the parent game object
		private void ResetParent(GameObject parent) {
			if (parent.transform.childCount > 0) {
				foreach (Transform child in parent.transform) {
					Destroy(child.gameObject);
				}
			}
		}

		// Creates a directory button given a directory
		public void CreateDirectoryButton(string directory) {
			GameObject button = Instantiate(DirectoryButtonPrefab, Vector3.zero, Quaternion.identity);
			SetupButton(button, new DirectoryInfo(directory).Name, DirectoriesParent.transform);
			// Setup FileBrowser DirectoryClick method to onClick event
			button.GetComponent<Button>().onClick.AddListener(() => { _fileBrowser.DirectoryClick(directory); });
		}

		// Creates a file button given a file
		public void CreateFileButton(string file) {
			GameObject button = Instantiate(FileButtonPrefab, Vector3.zero, Quaternion.identity);
			// Disable the buttons with different extension than the given file extension
			    DisableWrongExtensionFiles(button, file);
			

			SetupButton(button, Path.GetFileName(file), FilesParent.transform);
		}

		// Generic method used to extract common code for creating a directory or file button
		private void SetupButton(GameObject button, string text, Transform parent) {
			button.GetComponentInChildren<Text>().text = text;
			button.transform.SetParent(parent, false);
			button.transform.localScale = Vector3.one;
		}

		// Disables file buttons with files that have a different file extension (than given to the OpenFilePanel)
		private void DisableWrongExtensionFiles(GameObject button, string file) {
			if (!_fileBrowser.CompatibleFileExtension(file)) {
				button.GetComponent<Button>().interactable = false;
			}
		}
	}
}