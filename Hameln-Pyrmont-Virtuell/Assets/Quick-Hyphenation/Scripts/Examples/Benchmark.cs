using neoludic.QuickHyphenation;
using UnityEngine;

namespace Quick_Hyphenation.Scripts.Examples
{
    public class Benchmark : MonoBehaviour
    {
        [SerializeField] private bool hyphenate = false;
        [SerializeField] private HyphenationAsset asset;
        [SerializeField] private string textToHyphenate;
        [SerializeField] [Min(0)] private int itterationsPerFrame = 1;

        [SerializeField] [TextArea(3, 10)] private string result;
    
        // Update is called once per frame
        private string hyphen = "-";
        void Update()
        {
            for (int i = 0; i < itterationsPerFrame; i++)
            {
                if (hyphenate) result = asset.HyphenateText(textToHyphenate,hyphen); //our hyphenation function to test
                else result = textToHyphenate.Replace(" ",""); //Comparative string function
            }
        }
    }
}
