using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class GalleryImage : MonoBehaviour
{
    public string copyrightText = "";
    public string descriptionText = "";
    public GameObject copyrightButton;
    public GameObject copyrightDescriptionContent;
    public TextMeshProUGUI copyrightLabel;
    public TextMeshProUGUI copyrightHelperLabel;
    public LayoutElement copyrightLayoutElement;

    public UIImage uiImage;
	public UIRawImage uiRawImage;
	public int index = 0;
    public float minSize = 115f;    // Old 200

    public bool copyRightIsOpen = false;
    private bool isLoading = false;

	void Update()
	{
        if (!isLoading) { UpdateCopyrightSize(); }
    }

    public void SetCopyRight(string text)
    {
        SetIsOpen(false);

        copyrightText = text;
        copyrightDescriptionContent.SetActive(copyrightText != "");
    }

    public void UpdateCopyrightSize()
	{
        float maxWidth = copyrightDescriptionContent.GetComponent<RectTransform>().rect.width - 80;

        //copyrightLayoutElement.preferredWidth = Mathf.Clamp(copyrightHelperLabel.GetComponent<RectTransform>().rect.width + 110, minSize, maxWidth);
        copyrightLayoutElement.preferredWidth = Mathf.Clamp(copyrightHelperLabel.GetComponent<RectTransform>().rect.width, minSize, maxWidth);

    }

    public void ShowHideCopyright()
	{
		bool enable = copyrightButton.activeInHierarchy;
        copyrightButton.SetActive(!enable);
        copyrightDescriptionContent.SetActive(enable);
    }

    public void SetIsOpen(bool isOpen)
    {
        copyrightButton.SetActive(false);
        if (copyrightText != "") { copyrightDescriptionContent.SetActive(true); }
        else { copyrightDescriptionContent.SetActive(false); }

        if (isOpen)
        {
            copyrightHelperLabel.text = "<sprite=5> " + copyrightText;
            copyrightLabel.text = "<sprite=5> " + copyrightText;
            copyRightIsOpen = true;
        }
        else
        {
            copyrightHelperLabel.text = "<sprite=5> ";
            copyrightLabel.text = "<sprite=5> ";
            copyRightIsOpen = false;
        }
    }

    public void AnimateShowHideCopyright()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(AnimateShowHideCopyrightCoroutine());
    }

    public IEnumerator AnimateShowHideCopyrightCoroutine()
    {
        //long t = ToolsController.instance.GetTimestampMilliSeconds();

        //string text = "<voffset=-0.3em><size=150%>© </voffset></size>";
        string copyrightSign = "<sprite=5> ";
        string copyrightSuffix = copyrightText;
        string fullCopyrightText = copyrightSign;

        float timer = 0.125f;
        int lettersToAppend = 1;
        if (copyrightSuffix.Length < 10) { lettersToAppend = 1; timer = 0.05f; }
        else if (copyrightSuffix.Length < 20) { lettersToAppend = 1; timer = 0.125f; }
        else { lettersToAppend = 3; timer = 0.1f; }

        float stepTime = timer / (copyrightSuffix.Length/(float)lettersToAppend);

        if (!copyRightIsOpen)
        {
            for (int i = 0; i < copyrightSuffix.Length; i+=lettersToAppend)
            {
                for(int j = i; j<(i+lettersToAppend) && j < copyrightSuffix.Length; j++) { fullCopyrightText += copyrightSuffix[j]; }
                copyrightHelperLabel.text = fullCopyrightText;

                yield return new WaitForSeconds(Mathf.Min(1f / 15f, stepTime));

                UpdateCopyrightSize();

                copyrightLabel.text = fullCopyrightText;
            }
        }
        else
        {
            for (int i = 0; i <= copyrightSuffix.Length; i+=lettersToAppend)
            {
                string newText = copyrightSuffix.Substring(0, (copyrightSuffix.Length-i));
                for (int j = i; j <= (i+lettersToAppend) && j <= copyrightSuffix.Length; j++) { newText = copyrightSuffix.Substring(0, (copyrightSuffix.Length-j)); }       
                
                copyrightHelperLabel.text = copyrightSign + newText;

                yield return new WaitForSeconds(Mathf.Min(1f/15f, stepTime));

                UpdateCopyrightSize();
                copyrightLabel.text = copyrightSign+newText;
            }
        }

        copyRightIsOpen = !copyRightIsOpen;
        isLoading = false;

        //print(ToolsController.instance.GetTimestampMilliSeconds()-t);
    }

}
