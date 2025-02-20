using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CarouselImage : MonoBehaviour
{
    public CanvasGroup maskImage;
    public Image mainImage;
    public GameObject copyRightButton;
    public GameObject copyRightDescription;

    public int index = -1;
    public int side = 0;
    public bool isFocused = false;
    public float percentage = 0;
    public float centerPercentage = 0;
    private float fadeLerpSpeed = 10;
    private float moveLerpSpeed = 10;

    void LateUpdate()
    {
        this.transform.parent.eulerAngles = new Vector3(0, 0, 0);

        float maskImageAlpha = isFocused ? 0:1;
        maskImage.alpha = Mathf.Lerp(maskImage.alpha, maskImageAlpha, Time.deltaTime * fadeLerpSpeed);

        float targetPosition = 0;
        if(side == 1){ targetPosition = 300; }
        else if (side == -1){ targetPosition = -300; }
        this.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(this.GetComponent<RectTransform>().anchoredPosition, new Vector2(targetPosition, 0), Time.deltaTime*moveLerpSpeed);
    }

    public void SetParameterImmediate()
    {
        float targetPosition = 0;
        if (side == 1) { targetPosition = 300; }
        else if (side == -1) { targetPosition = -300; }
        this.GetComponent<RectTransform>().anchoredPosition = new Vector2(targetPosition, 0);

        float maskImageAlpha = isFocused ? 0 : 1;
        maskImage.alpha = maskImageAlpha;
    }

    public void SetCopyRight(string text)
    {
        copyRightButton.SetActive(false);
        copyRightDescription.SetActive(false);
        if(text != "")
        {
            copyRightButton.SetActive(true);
            copyRightDescription.GetComponentInChildren<TextMeshProUGUI>().text = text;
        }
    }

    public void OpenCloseCopyRight()
    {
        copyRightButton.SetActive(!copyRightButton.activeInHierarchy);
        copyRightDescription.SetActive(!copyRightDescription.activeInHierarchy);
    }
}
