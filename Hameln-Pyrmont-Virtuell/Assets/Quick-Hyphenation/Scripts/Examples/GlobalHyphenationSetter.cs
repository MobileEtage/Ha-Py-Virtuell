using neoludic.QuickHyphenation;
using UnityEngine;

namespace Quick_Hyphenation.Scripts.Examples
{
    [ExecuteAlways]
    public class GlobalHyphenationSetter : MonoBehaviour
    {
        [SerializeField] private HyphenationAsset hyphenationAsset;

        private void OnValidate()
        {
            if(hyphenationAsset == null) return;
            if(Hyphenation.LanguageID != hyphenationAsset.name) Hyphenation.Setup(hyphenationAsset.name);
        }

        /// <summary>
        /// This is simply an example showing that you can set the global Hyphenation Language based on the name of the corresponding asset.
        /// </summary>
        private void OnEnable()
        {
            if(hyphenationAsset == null) return;
            if(Hyphenation.LanguageID != hyphenationAsset.name) Hyphenation.SetupAsync(hyphenationAsset.name);
        }
    }
}
