using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlideShowScrollViewPro
{

    public class FadeCanvasMenus : MonoBehaviour
    {

        public List<FadeCanvas> fadeCanvasList;

        public void HideAll_Click()
        {
            foreach (FadeCanvas fadeCanvas in fadeCanvasList) {
                fadeCanvas.FadeOut();
            }
        }

        public void HideAllExcept(FadeCanvas canvasToNotFade)
        {
            foreach (FadeCanvas fadeCanvas in fadeCanvasList) {
                if (fadeCanvas == canvasToNotFade) {
                    continue;
                }

                fadeCanvas.FadeOut();
            }
        }
    }
}
