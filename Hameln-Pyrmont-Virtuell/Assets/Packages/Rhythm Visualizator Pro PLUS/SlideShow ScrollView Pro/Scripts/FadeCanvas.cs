// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlideShowScrollViewPro
{

    /// <summary>
    /// Use this script to fade out/in your canvas group
    /// </summary>
    [DisallowMultipleComponent]
    public class FadeCanvas : MonoBehaviour
    {

        public CanvasGroup canvasGroup;

        public bool DeactivateGameObject = true;

        public float FadeTime = 0.2f;

        public bool FadeWithButton = false;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home))
            {
                if (canvasGroup.alpha > 0)
                {
                    canvasGroup.blocksRaycasts = false;
                    FadeOut();
                } else
                {
                    canvasGroup.blocksRaycasts = true;
                    FadeIn();
                }
            }
        }

        /// <summary>
        /// Fade Out
        /// </summary>
        public void FadeOut()
        {
            StopAllCoroutines();
            if (gameObject.activeInHierarchy) {
                StartCoroutine(FadeOutNow());
            } else {
                canvasGroup.alpha = 0;
            }
        }

        /// <summary>
        /// Fade In
        /// </summary>
        public void FadeIn()
        {
            StopAllCoroutines();

            if (gameObject.activeInHierarchy) {
                StartCoroutine(FadeInNow());
            } else {
                canvasGroup.alpha = 1;
            }
        }

        IEnumerator FadeOutNow()
        {
            canvasGroup.alpha = 1;

            float t = 0;

            while (t <= FadeTime) {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / FadeTime);
                t += Time.unscaledDeltaTime;
                yield return new WaitForEndOfFrame();
            }

            canvasGroup.alpha = 0;

            if (DeactivateGameObject) {
                canvasGroup.gameObject.SetActive(false);
            }

        }

        IEnumerator FadeInNow()
        {

            SlideShowScrollViewPro_Scroll.instance.ShowVisibleActiveButtons();

            canvasGroup.alpha = 0;
            canvasGroup.gameObject.SetActive(true);

            float t = 0;

            while (t <= FadeTime) {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / FadeTime);
                t += Time.unscaledDeltaTime;
                yield return new WaitForEndOfFrame();
            }

            canvasGroup.alpha = 1;

            SlideShowScrollViewPro_Scroll.instance.ShowVisibleActiveButtons();
        }
    }
}
