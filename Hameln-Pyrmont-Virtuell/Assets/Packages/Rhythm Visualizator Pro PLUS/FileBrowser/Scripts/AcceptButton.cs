using RhythmVisualizatorPro.FileBrowser.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AcceptButton : MonoBehaviour
{
    public Button acceptButton;

    // Start is called before the first frame update
    void Start()
    {
        acceptButton.onClick.AddListener(FindObjectOfType<FileBrowser>().CloseAndSaveFileBrowser);
    }
}
