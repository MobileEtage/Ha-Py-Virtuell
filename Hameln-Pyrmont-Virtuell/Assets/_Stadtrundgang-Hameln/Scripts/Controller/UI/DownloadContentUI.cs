using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DownloadContentUI : MonoBehaviour
{
    public List<GameObject> skipButtons = new List<GameObject>();

    public GameObject newDataAvailableContent;
    public GameObject downloadingContent;
    public Image progressImage;
    public TextMeshProUGUI downloadInfoLabel;

    public static DownloadContentUI instance;
    void Awake()
    {
        instance = this;
    }

    public void Download()
    {
        DownloadContentController.instance.Download();
    }

    public void SkipDownload()
    {
        DownloadContentController.instance.SkipDownload();
    }

    public void AbortDownload()
    {
        InfoController.instance.ShowCommitAbortDialog("Willst Du das Herunterladen wirklich abbrechen?", CommitAbort);
    }

    public void CommitAbort()
    {
        DownloadContentController.instance.CommitAbort();
    }
}
