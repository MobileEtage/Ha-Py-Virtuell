// Created by Carlos Arturo Rodriguez Silva (Legend)
// Contact: carlosarturors@gmail.com

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SlideShowScrollViewPro
{

    public class MySongElementUI : MonoBehaviour
    {
        [Header("Element Values")]

        public SlideShowScrollViewPro_Group actualGroup;

        public int elementPos;
        public int elementID;
        public Button button;
        public Image image;

        [Header("My Values")]
        // Data Variables
        public TMP_Text title;
        public TMP_Text artist;

        public MySongCustomData mySongCustomData;

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
        /// Sets the element ID in the scroll.
        /// </summary>
        /// <param name="ID"></param>
        public void SetID(int ID)
        {
            elementID = ID;
        }

        /// <summary>
        /// Updates the texts using the saved data.
        /// </summary>
        public void UpdateTextsData()
        {
            title.text = mySongCustomData.title;
            artist.text = mySongCustomData.artist;
        }



        /// <summary>
        /// Select this element, if already selected do another action.
        /// </summary>
        public void SelectThis_Click()
        {
            // If this button is already selected
            if (SlideShowScrollViewPro_Scroll.instance.selectedElementPos == elementPos && SlideShowScrollViewPro_Scroll.instance.selectedElementID == elementID) {
                // button.interactable = false;

                Debug.Log("Song button clicked again");
                // Load song
                mySongCustomData.LoadSong(image.sprite);
            }
            else { // If not selected yet
                SelectThisImageOnly_Click();
            }
        }

        

        /// <summary>
        /// Select this element only.
        /// </summary>
        public void SelectThisImageOnly_Click()
        {
            SlideShowScrollViewPro_Scroll.instance.SelectButtonByID_Click(elementID);
        }

        /// <summary>
        /// Set the texts color.
        /// </summary>
        /// <param name="color"></param>
        public void SetTextsColor(Color color)
        {
            title.color = color;
            artist.color = color;
        }

        /// <summary>
        /// Sets the image for this element.
        /// </summary>
        /// <param name="SongImage"></param>
        public void SetImage(Sprite SongImage)
        {
            image.color = Color.gray;
            image.sprite = SongImage;
        }

        /// <summary>
        /// Delete from list on destroy
        /// </summary>
        void OnDestroy()
        {
            if (SlideShowScrollViewPro_Scroll.instance.buttons.Exists(x => gameObject)) {
                SlideShowScrollViewPro_Scroll.instance.RecalculateActiveButtonsAndSize();
//                Debug.Log("Deleting from list");
            }
        }

        private void OnDisable()
        {
            GetComponent<ShowUIWhenIsVisible>().UpdateNow();
        }

        private void OnEnable()
        {
            GetComponent<ShowUIWhenIsVisible>().UpdateNow();
        }
    }
}
