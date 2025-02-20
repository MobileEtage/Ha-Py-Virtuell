using TMPro;
using UnityEngine;

namespace neoludic.QuickHyphenation
{
    [Icon("Assets/Quick Hyphen/Icon/QuickHyphenTextProcessor.png")]
    [RequireComponent(typeof(TMP_Text))] [ExecuteAlways]
    public abstract class BaseHyphenationProcessor : MonoBehaviour, ITextPreprocessor
    {
        [HideInInspector] [SerializeField] protected TMP_Text textReference;
        private ITextPreprocessor _followingProcessor = null;
        
        [ExecuteAlways]
        protected virtual void OnEnable()
        {
            OnValidate();
        }

        [ExecuteAlways]
        protected virtual void OnDisable()
        {
            if (textReference != null)
            {
                if (_followingProcessor != null) textReference.textPreprocessor = _followingProcessor;
                else textReference.textPreprocessor = null;
                if(textReference.isActiveAndEnabled) textReference.SetText(textReference.text);
            }
        }

        public string PreprocessText(string text)
        {
            if (_followingProcessor != null) text = _followingProcessor.PreprocessText(text);
            return enabled ? Hyphenate(text) : text;
        }

        protected abstract string Hyphenate(string source);

        private void OnValidate()
        {
            textReference ??= GetComponent<TMP_Text>();
            if (textReference.textPreprocessor != null && (BaseHyphenationProcessor)textReference.textPreprocessor != this)
                _followingProcessor = textReference.textPreprocessor;
            if ((BaseHyphenationProcessor)textReference.textPreprocessor != this)
            {
                textReference.textPreprocessor = this;
            }
            textReference.SetAllDirty();
        }
    }
}