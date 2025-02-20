using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlideShowScrollViewPro
{

    public class ShowUIWhenIsVisible : MonoBehaviour
    {

        public Camera scrollListCamera;
        public GameObject content;
        public RectTransform rectTransform;

        void OnEnable()
        {
            try {
                scrollListCamera = GameObject.FindWithTag("ScrollListCamera").GetComponent<Camera>();
            }
            catch {
                Debug.LogError("ScrollListCamera not found, add the ScrollListCamera prefab to the scene");
            }
        }

        public void UpdateNow()
        {
            bool isFullyVisible = rectTransform.IsVisibleFrom(scrollListCamera);
            if (isFullyVisible) {
                content.SetActive(true);
            }
            else {
                content.SetActive(false);
            }
        }

        //void Update ()
        //{
        //    bool isFullyVisible = rectTransform.IsVisibleFrom(Camera.main);
        //    if (isFullyVisible) {
        //        content.SetActive(true);
        //    }
        //    else {
        //        content.SetActive(false);
        //    }
        //}
    }
}
