using UnityEngine;

namespace neoludic.QuickHyphenation
{
    public class CustomHyphenationProcessor : BaseHyphenationProcessor
    {
        [SerializeField] private HyphenationAsset hyphenationAsset;
        [SerializeField] private string customHyphenationCharacter = "-";
        [SerializeField] private bool useSoftHyphen = true;
        protected override string Hyphenate(string source)
        {
            if (hyphenationAsset == null)
            {
                Debug.LogWarning($"Please assign a hyphenation asset to {name}", this);
                return source;
            }
            return hyphenationAsset.HyphenateText(source, useSoftHyphen ? null : customHyphenationCharacter);
        }
    }
}