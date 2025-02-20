using System;
using System.IO;
using UnityEngine;

namespace neoludic.QuickHyphenation
{
    [Icon("Assets/Quick Hyphen/Icon/QuickHyphenTextProcessor.png")]
    public static class Hyphenation
    {
        [NonSerialized] public static string GlobalHyphen = SOFT_HYPHEN;
        [NonSerialized] public static Action onHyphenationChanged;
        public const string SOFT_HYPHEN = "\u00AD";
        
        public static bool IsLoading { get; private set;} = false;
        public static bool IsSetup { get; private set; } = false;
        public static string LanguageID { get; private set; } = "DE";
        
        private static HyphenationAsset _instance;
        private const string _DIRECTORY_NAME = "Hyphenation Assets";
        
        /// <summary>
        /// Hyphenates this <paramref name="source"/> string, using the global hyphen.
        /// If the hyphenation system has not been set-up, this will also set up the hyphenation system, defaulting to "EN-UK"
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string Hyphenate(this string source) => Hyphenate(source,GlobalHyphen);
        
        /// <summary>
        /// Hyphenates this <paramref name="source"/> string, using the provided <paramref name="hyphen"/>.
        /// If the hyphenation system has not been set-up, this will also set up the hyphenation system, defaulting to "EN-UK"
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string Hyphenate(this string source, string hyphen)
        {
            if(_instance == null) Setup(LanguageID);
            if (_instance == null) Debug.LogError($"Could not load Hyphenation Asset {LanguageID}. Make sure you configured that language.");
            else return _instance.HyphenateText(source, hyphen);
            return source;
        }

        /// <summary>
        /// This sets the <value>GlobalHyphen</value> character, and fires the <value>onHyphenationChanged</value> event.
        /// </summary>
        /// <param name="hyphen"></param>
        public static void SetHyphen(string hyphen)
        {
            Hyphenation.GlobalHyphen = hyphen;
            onHyphenationChanged?.Invoke();
        }
        
        /// <summary>
        /// Use this function to set-up the global hyphenation language if you want to load the hyphenation asset from it's string language ID.
        /// The language ID must correspond to an existing Hyphenation Asset file name. 
        /// </summary>
        /// <param name="languageID"></param>
        public static void SetupAsync(string languageID = null)
        {
            if(IsLoading) return;
            languageID ??= LanguageID;
            LanguageID = languageID;
            if (_instance != null)
            {
                if(_instance.name == languageID) return;
                //_instance.Dispose();
                _instance = null;
            }
            IsSetup = false;
            IsLoading = true;
            var request = Resources.LoadAsync<HyphenationAsset>(Path.Combine(_DIRECTORY_NAME, languageID));
            request.completed += x =>
            {
                IsLoading = false;
                SetupInstanceInternal(request.asset as HyphenationAsset);
            };
        }

        /// <summary>
        /// Use this to immediately set-up the Hyphenation system. Calling Resources.Load frequently at runtime is bad practise, try using SetupAsync instead when possible.
        /// </summary>
        /// <param name="languageID"></param>
        public static void Setup(string languageID)
        {
            LanguageID = languageID;
            SetupInstanceInternal(Resources.Load<HyphenationAsset>(Path.Combine(_DIRECTORY_NAME,languageID)));
        }

        /// <summary>
        /// Use this to immediately set-up the Hyphenation system based on an asset reference.
        /// </summary>
        /// <param name="asset"></param>
        public static void Setup(HyphenationAsset asset)
        {
            Debug.Assert(asset != null);
            if(LanguageID == asset.name) return;
            LanguageID = asset.name;
            IsLoading = false;
            SetupInstanceInternal(asset);
        }

        public static void DisposeCurrent()
        {
            if(_instance != null) _instance.Dispose();
            _instance = null;
        }

        private static void SetupInstanceInternal(HyphenationAsset asset)
        {
            if(asset != null)
            {
                if(asset.name != LanguageID) return;
                IsSetup = true;
                _instance = asset;
                asset.Init();
                onHyphenationChanged?.Invoke();
            }
        }
    }
}
