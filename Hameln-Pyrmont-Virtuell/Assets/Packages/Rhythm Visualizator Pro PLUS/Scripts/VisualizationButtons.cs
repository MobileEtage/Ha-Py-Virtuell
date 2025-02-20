using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmVisualizatorPro
{

    public class VisualizationButtons : MonoBehaviour
    {
        public Button prevButton;
        public Button nextButton;

        void Start()
        {
            try {
                prevButton.onClick.AddListener(() => FindObjectOfType<RhythmVisualizatorPro>().NextForm(false));
                nextButton.onClick.AddListener(() => FindObjectOfType<RhythmVisualizatorPro>().NextForm(true));
            }
            catch {
                Debug.LogError("Rhythm Visualizator Pro script is not in the scene, the prev/next visualization buttons doesn't work");
            }

        }
    }
}
