using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SlideShowScrollViewPro
{

    public class SlideShowScrollViewPro_PointerHelper : MonoBehaviour, IPointerClickHandler
    {

        public MySongElementUI songData;

        #region IPointerClickHandler implementation

        public void OnPointerClick(PointerEventData pointerData)
        {
            if (pointerData.button == PointerEventData.InputButton.Right) {
                songData.SelectThisImageOnly_Click();
                //				FindObjectOfType<SongSelection_SongOptions> ().Open ();
            }

            #endregion

        }
    }
}

