using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimationController : MonoBehaviour
{
    public List<CurveElement> curveElements = new List<CurveElement>();

    public static AnimationController instance;
    void Awake()
    {
        instance = this;
    }

    public IEnumerator AnimateMove3DGlobalCoroutine(
        Transform myTransform,
        Vector3 startPosition, Vector3 targetPosition,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        myTransform.position = startPosition;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            myTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        myTransform.position = targetPosition;
    }

    public IEnumerator AnimateMoveWorldPositionCoroutine(
        Transform myTransform,
        Vector3 startPosition, Vector3 targetPosition,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        myTransform.position = startPosition;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            myTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        myTransform.position = targetPosition;
    }

    public void AnimateMovePercentage(
        RectTransform rectTransform,
        float startPercentageHorizontal, float startPercentageVertical,
        float targetPercentageHorizontal, float targetPercentageVertical,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {
        StartCoroutine(AnimateMovePercentageCoroutine(
            rectTransform,
            startPercentageHorizontal, startPercentageVertical,
            targetPercentageHorizontal, targetPercentageVertical,
            animationDuration, animationCurveID
        ));
    }

    public IEnumerator AnimateMovePercentageCoroutine(
        RectTransform rectTransform,
        float startPercentageHorizontal, float startPercentageVertical,
        float targetPercentageHorizontal, float targetPercentageVertical,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        float height = rectTransform.rect.height;
        float width = rectTransform.rect.width;
        Vector2 startPosition = new Vector2(width * startPercentageHorizontal, height * startPercentageVertical);
        Vector2 targetPosition = new Vector2(width * targetPercentageHorizontal, height * targetPercentageVertical);
        rectTransform.anchoredPosition = startPosition;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
    }

    public void AnimateMove(
        RectTransform rectTransform,
        Vector2 startPosition, Vector2 targetPosition,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {
        StartCoroutine(AnimateMoveCoroutine(
            rectTransform,
            startPosition, targetPosition,
            animationDuration, animationCurveID
        ));
    }

    public IEnumerator AnimateMoveCoroutine(
        RectTransform rectTransform,
        Vector2 startPosition, Vector2 targetPosition,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        rectTransform.anchoredPosition = startPosition;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
    }

    public IEnumerator AnimateMove3DCoroutine(
        Transform myTransform,
        Vector3 startPosition, Vector3 targetPosition,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        myTransform.localPosition = startPosition;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            myTransform.localPosition = Vector3.LerpUnclamped(startPosition, targetPosition, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        myTransform.localPosition = targetPosition;
    }

    public void AnimateRotate(
        RectTransform rectTransform,
        Vector3 startRotation, Vector3 targetRotation,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {
        StartCoroutine(AnimateRotateCoroutine(
            rectTransform,
            startRotation, targetRotation,
            animationDuration, animationCurveID
        ));
    }

    public IEnumerator AnimateRotateCoroutine(
        RectTransform rectTransform,
        Vector3 startRotation, Vector3 targetRotation,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        rectTransform.localEulerAngles = startRotation;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            rectTransform.localEulerAngles = Vector3.LerpUnclamped(startRotation, targetRotation, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.localEulerAngles = targetRotation;
    }

    public IEnumerator AnimateRotateCoroutine(
        Transform myTransform,
        Vector3 startRotation, Vector3 targetRotation,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        myTransform.localEulerAngles = startRotation;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            myTransform.localEulerAngles = Vector3.LerpUnclamped(startRotation, targetRotation, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        myTransform.localEulerAngles = targetRotation;
    }

    public IEnumerator AnimateRotateXYZCoroutine(
        Transform myTransform,
        Vector3 startRotation, Vector3 targetRotation,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        myTransform.localEulerAngles = startRotation;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            float x = Mathf.LerpAngle(startRotation.x, targetRotation.x, lerpValue);
            float y = Mathf.LerpAngle(startRotation.y, targetRotation.y, lerpValue);
            float z = Mathf.LerpAngle(startRotation.z, targetRotation.z, lerpValue);

            myTransform.localEulerAngles = new Vector3(x, y, z);

            currentTime += Time.deltaTime;
            yield return null;
        }

        myTransform.localEulerAngles = targetRotation;
    }

    public IEnumerator AnimateLayoutElementMinHeightCoroutine(
        LayoutElement layoutElement,
        float startHeight, float targetHeight,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {
        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        layoutElement.minHeight = startHeight;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {
            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            float h = Mathf.LerpUnclamped(startHeight, targetHeight, lerpValue);

            layoutElement.minHeight = h;

            currentTime += Time.deltaTime;
            yield return null;
        }
        layoutElement.minHeight = targetHeight;
    }

    public void AnimateSize(
        RectTransform rectTransform,
        Vector2 startSize, Vector2 targetSize,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {
        StartCoroutine(AnimateSizeCoroutine(
            rectTransform,
            startSize, targetSize,
            animationDuration, animationCurveID
        ));
    }

    public IEnumerator AnimateSizeCoroutine(
        RectTransform rectTransform,
        Vector2 startSize, Vector2 targetSize,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        rectTransform.sizeDelta = startSize;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            rectTransform.sizeDelta = Vector2.LerpUnclamped(startSize, targetSize, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.sizeDelta = targetSize;
    }


    public void AnimateImageColor(
        Image image,
        Color startColor, Color endColor,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {
        StartCoroutine(AnimateImageColorCoroutine(
            image,
            startColor, endColor,
            animationDuration, animationCurveID
        ));
    }

    public IEnumerator AnimateImageColorCoroutine(
        Image image,
        Color startColor, Color endColor,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {
        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        image.color = startColor;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            image.color = Color.LerpUnclamped(startColor, endColor, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        image.color = endColor;
    }

    public IEnumerator AnimateRawImageColorCoroutine(
        RawImage image,
        Color startColor, Color endColor,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {
        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        image.color = startColor;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            image.color = Color.LerpUnclamped(startColor, endColor, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        image.color = endColor;
    }

    public void AnimateImageColorAlpha(
        Image image,
        float startColorAlpha, float endColorAlpha,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {
        StartCoroutine(AnimateImageColorAlphaCoroutine(
            image,
            startColorAlpha, endColorAlpha,
            animationDuration, animationCurveID
        ));
    }

    public IEnumerator AnimateImageColorAlphaCoroutine(
        Image image,
        float startColorAlpha, float endColorAlpha,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        image.color = new Color(image.color.r, image.color.g, image.color.b, startColorAlpha);

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            float alpha = Mathf.LerpUnclamped(startColorAlpha, endColorAlpha, lerpValue);
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);

            currentTime += Time.deltaTime;
            yield return null;
        }

        image.color = new Color(image.color.r, image.color.g, image.color.b, endColorAlpha); ;
    }

    public void AnimateCanvasGroupAlpha(
        CanvasGroup canvasGroup,
        float startAlpha, float targetAlpha,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {
        StartCoroutine(AnimateCanvasGroupAlphaCoroutine(
            canvasGroup,
            startAlpha, targetAlpha,
            animationDuration, animationCurveID
        ));
    }

    public IEnumerator AnimateCanvasGroupAlphaCoroutine(
        CanvasGroup canvasGroup,
        float startAlpha, float targetAlpha,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        canvasGroup.alpha = startAlpha;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }


    public IEnumerator AnimateMaterialPropertyCoroutine(
        RawImage rawImage, string materialProperty,
        float startValue, float targetValue,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {
        if (rawImage.material == null) yield break;
        if (!rawImage.material.HasProperty(materialProperty)) yield break;

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        rawImage.material.SetFloat(materialProperty, startValue);

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            float val = Mathf.LerpUnclamped(startValue, targetValue, lerpValue);
            rawImage.material.SetFloat(materialProperty, val);

            currentTime += Time.deltaTime;
            yield return null;
        }

        rawImage.material.SetFloat(materialProperty, targetValue);
    }


    public IEnumerator AnimateMaterialColorPropertyCoroutine(
    RawImage rawImage, string materialProperty,
    Color startColor, Color endColor,
    float animationDuration = 1, string animationCurveID = "linear")
    {
        if (rawImage.material == null) yield break;
        if (!rawImage.material.HasProperty(materialProperty)) yield break;

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        rawImage.material.SetColor(materialProperty, startColor);

        float currentTime = 0;
        while (currentTime < animationDuration)
        {
            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            rawImage.material.SetColor(materialProperty, Color.LerpUnclamped(startColor, endColor, lerpValue));

            currentTime += Time.deltaTime;
            yield return null;
        }

        rawImage.material.SetColor(materialProperty, endColor);
    }

    public void AnimateScale(
        Transform myTransform,
        Vector3 startScale, Vector3 targetScale,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {
        StartCoroutine(AnimateScaleCoroutine(
            myTransform,
            startScale, targetScale,
            animationDuration, animationCurveID
        ));
    }

    public IEnumerator AnimateScaleCoroutine(
        Transform myTransform,
        Vector3 startScale, Vector3 targetScale,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        myTransform.localScale = startScale;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            myTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        myTransform.localScale = targetScale;
    }

    public IEnumerator AnimateScalePingPongCoroutine(
        Transform myTransform,
        Vector3 startScale, Vector3 targetScale,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        myTransform.localScale = startScale;

        animationDuration = animationDuration * 0.5f;
        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            myTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        myTransform.localScale = targetScale;

        currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            myTransform.localScale = Vector3.LerpUnclamped(targetScale, startScale, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        myTransform.localScale = startScale;
    }

    public IEnumerator AnimateFillAmountCoroutine(
        Image image,
        float startValue, float targetValue,
        float animationDuration = 1, string animationCurveID = "linear"
    )
    {

        AnimationCurve animationCurve = GetAnimationCurveWithID(animationCurveID);
        if (animationCurve == null) yield break;

        image.fillAmount = startValue;

        float currentTime = 0;
        while (currentTime < animationDuration)
        {

            float lerpValue = animationCurve.Evaluate(currentTime / animationDuration);
            image.fillAmount = Mathf.LerpUnclamped(startValue, targetValue, lerpValue);

            currentTime += Time.deltaTime;
            yield return null;
        }

        image.fillAmount = targetValue;
    }

    public AnimationCurve GetAnimationCurveWithID(string animationCurveID)
    {

        for (int i = 0; i < curveElements.Count; i++)
        {

            if (curveElements[i].id == animationCurveID)
            {
                return curveElements[i].animationCurve;
            }
        }
        return null;
    }
}

[System.Serializable]
public class CurveElement
{

    public string id;
    public AnimationCurve animationCurve;
}
