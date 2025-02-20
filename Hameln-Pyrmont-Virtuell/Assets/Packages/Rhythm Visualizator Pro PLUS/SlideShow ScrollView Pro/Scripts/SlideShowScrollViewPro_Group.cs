// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlideShowScrollViewPro
{

    /// <summary>
    /// This script manages the groups
    /// </summary>
    public class SlideShowScrollViewPro_Group : MonoBehaviour
    {

        public bool returnButton;
        string groupName;

        public string GroupName {
            get { return groupName; }
            set {
                gameObject.name = value;
                groupTitle.text = value;
                groupName = value;
            }
        }

        public int indexNumber = 0;

        public Button button;
        public TMP_Text groupTitle;
        public TMP_Text groupElementsCount;

        int elementsCount = 0;

        public int ElementsCount {
            get { return elementsCount; }
            set {
                elementsCount = value;
                UpdateElementsCount();
            }

        }

        // Fix rotation
        void Start()
        {
            if (SlideShowScrollViewPro_Scroll.instance.vertical) {
                transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -90));
            }
            else {
                transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
            }
        }
        /// <summary>
        /// Show elements from this group
        /// </summary>
        public void OpenGroup_Click()
        {
            SlideShowScrollViewPro_Scroll.instance.ShowElementsFromGroup(this);
        }

        /// <summary>
        /// Resets the group
        /// </summary>
        public void ResetGroup()
        {
            elementsCount = 0;
            UpdateElementsCount();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Increases the elements count
        /// </summary>
        public void IncreaseElementsCount(bool activate = true)
        {
            elementsCount++;
            UpdateElementsCount();

            if (activate) {
                gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Updates elements count text
        /// </summary>
        void UpdateElementsCount()
        {
            if (elementsCount == 1) {
                groupElementsCount.text = string.Format("{0} element", elementsCount);
            }
            else {
                groupElementsCount.text = string.Format("{0} elements", elementsCount);
            }
        }
    }
}
