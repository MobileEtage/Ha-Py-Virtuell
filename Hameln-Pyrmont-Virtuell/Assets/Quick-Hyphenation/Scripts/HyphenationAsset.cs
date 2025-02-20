using System;
using System.Text;
using neoludic.QuickHyphenation.DataNodes;
using UnityEngine;
using UnityEngine.Scripting;

namespace neoludic.QuickHyphenation
{
    [Icon("Assets/Quick Hyphen/Icon/QuickHyphenAssetIcon.png")]
    [CreateAssetMenu(fileName = "Hyphenator", menuName = "Hyphenation Asset")]
    public class HyphenationAsset : ScriptableObject
    {
        [Header("Settings")] 
        [SerializeField] private string languageName;
        [SerializeField] [Min(0)] int minCharactersToStartOfWord = 1;
        [SerializeField] [Min(0)] int minCharactersToEndOfWord = 1;

        [Header("Patterns License Information")]
        [SerializeField] [Preserve] private string authors;
        [SerializeField] [Preserve] [TextArea(1,3)] private string copyright;
        [SerializeField] [Preserve] [TextArea(5,7)] private string copyrightNotice;
        [SerializeField] [Preserve] private string licenseType;
        [SerializeField] [Preserve] private string licenseURL;

        [SerializeField] private SerializableHyphenationDataTree rootTree = new SerializableHyphenationDataTree();

        private const char _RICH_TEXT_START_TAG_ = '<', _RICH_TEXT_END_TAG = '>', _END_OF_WORD_IDENTIFIER = '.';
        private const int _MAX_ARRAY_EXPANSION = 1024 * 1024 * 4; //At 4 Megabytes we will reset the size of our string builders

        private byte[] _patternResult = new byte[1024];
        private StringBuilder _result = new StringBuilder(1024);
        private StringBuilder _sourceBuilder = new StringBuilder(1024);
        
        
        /// <summary>
        /// Hyphenates the inserted text 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="hyphen"></param>
        /// <returns></returns>
        public string HyphenateText(string source, string hyphen = null)
        {
            rootTree.TryInit();
            hyphen ??= Hyphenation.GlobalHyphen;
            _result.Clear();
            _sourceBuilder.Clear();
            _sourceBuilder.Append(_END_OF_WORD_IDENTIFIER).Append(source).Append(_END_OF_WORD_IDENTIFIER);
            if (_patternResult.Length < _sourceBuilder.Length) _patternResult = new byte[_sourceBuilder.Length];
            else Array.Clear(_patternResult,0,_patternResult.Length);

            // In this pass, we assign the byte values to our mask, skipping any areas inside rich text identifiers '<' and '>'
            HyphenationDataNode node = rootTree.root;
            for (int i = 0; i < _sourceBuilder.Length; i++)
            {
                if (_sourceBuilder[i] == _RICH_TEXT_START_TAG_)
                {
                    ApplyWordBoundaryMask(ref _patternResult,i);
                    i = source.IndexOf(_RICH_TEXT_END_TAG, i);
                    if (i == -1) break;
                    else continue;
                }
                
                int letterOffset = 0;
                while (i + letterOffset < _sourceBuilder.Length && node.TryGetNode(_sourceBuilder[i+letterOffset], out node))
                {
                    node.ApplyTo(ref _patternResult, i);
                    letterOffset++;
                }
                if(!Char.IsLetter(_sourceBuilder[i])) ApplyWordBoundaryMask(ref _patternResult,i);
                node = rootTree.root;
            }

            //In this pass, we copy the source string into the target string between the hyphens we've identified.
            ushort lastHyphen = 1;
            for (ushort i = 1; i < _sourceBuilder.Length-1; i++)
            {
                if (_patternResult[i] % 2 > 0) //Hyphens are marked by uneven numbers
                {
                    _result.Append(_sourceBuilder, lastHyphen, i - lastHyphen); //Append text between hyphens
                    _result.Append(hyphen); //Append the Hyphen
                    lastHyphen = i;
                }
            }
            
            _result.Append(_sourceBuilder, lastHyphen, _sourceBuilder.Length - 1 - lastHyphen);
            string resultingString = _result.ToString();
            
            if (_patternResult.Length > _MAX_ARRAY_EXPANSION || _result.Length > _MAX_ARRAY_EXPANSION)
            {
                _result.Capacity = 1024;
                _patternResult = new byte[1024];
                _sourceBuilder.Capacity = 1024;
            }
            return resultingString;
        }
        
        /// <summary>
        /// Ensures that words Boundaries are maintained. By Applying a high even byte value to word starts and ends.
        /// </summary>
        /// <param name="patternsMask"></param>
        /// <param name="index"></param>
        private void ApplyWordBoundaryMask(ref byte[] patternsMask, int index)
        {
            int min = Mathf.Max(index - minCharactersToEndOfWord, 0);
            int max = index + minCharactersToStartOfWord +1;
            Array.Fill(patternsMask,(byte)12,min,max-min);
        }

        public void SetAsCurrent() => Hyphenation.Setup(this);
        
        public void Init() => rootTree.TryInit();
        public void Dispose() => rootTree.Dispose();
    }
}