#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using neoludic.QuickHyphenation;
using neoludic.QuickHyphenation.DataNodes;
using UnityEditor;
using UnityEngine;

namespace Quick_Hyphenation.Scripts.Editor
{
    [CustomEditor(typeof(HyphenationAsset))]
    public class HyphenationAssetEditor : UnityEditor.Editor
    {
        private string _previewTextInput = "Hyphenation";
        private string _rawTextInput;
        private string _urlInput;
        private string[] _exceptions = null;
        private string _newException = "";
        private SerializableHyphenationDataTree _rootTree = null;
        readonly string[] _splittingCharacters = { "\n", "\r", " ", "\n\r", "\r", "\t" };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); //This causes some performance problems, possibly because of rootTrees many fields
        
            var asset = (HyphenationAsset)target;
            _rootTree ??= TryGet<SerializableHyphenationDataTree>("rootTree");
            _exceptions ??= GetExceptions();
        
            DisplayClearButtons(asset);
            DisplaySetupButtons();
            DisplayStats();
            DisplayPreview(asset);
            DisplayExceptionsEditor();
        }

        private void DisplayExceptionsEditor()
        {
            GUILayout.Space(25f);
            EditorGUILayout.LabelField("Exceptions", EditorStyles.boldLabel);
            for (int i = 0; i < _exceptions.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(_exceptions[i]);
                if (GUILayout.Button("x"))
                {
                    _rootTree.RemoveException(_exceptions[i]);
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                    _exceptions = GetExceptions();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            _newException = EditorGUILayout.TextField(_newException);
            if (GUILayout.Button("Add Exception") && !string.IsNullOrEmpty(_newException))
            {
                _rootTree.AddException(_newException.ToLowerInvariant());
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                _exceptions = GetExceptions();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DisplayPreview(HyphenationAsset asset)
        {
            GUILayout.Space(12.5f);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _previewTextInput = EditorGUILayout.TextField(_previewTextInput);
            EditorGUILayout.LabelField(asset.HyphenateText(_previewTextInput, "-"));
            EditorGUILayout.EndHorizontal();
        }

        private void DisplayStats()
        {
            GUILayout.Space(25);
            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{_rootTree.GetPatternsCount().ToString()} Patterns");
            EditorGUILayout.LabelField($"{_rootTree.GetExceptionsCount().ToString()} Exceptions");
            EditorGUILayout.EndHorizontal();
        }

        private void DisplaySetupButtons()
        {
            GUILayout.Space(12.5f);
            EditorGUILayout.LabelField("Setup from String", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _rawTextInput = EditorGUILayout.TextField(_rawTextInput);

            if (GUILayout.Button("Generate Patterns from String"))
                if (!string.IsNullOrEmpty(_rawTextInput))
                    InitPatterns(RemoveTexComments(_rawTextInput).Split(_splittingCharacters, StringSplitOptions.RemoveEmptyEntries));
            if (GUILayout.Button("Generate Exceptions from String"))
                if (!string.IsNullOrEmpty(_rawTextInput))
                    InitExceptions(RemoveTexComments(_rawTextInput).Split(_splittingCharacters, StringSplitOptions.RemoveEmptyEntries));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(12.5f);

            EditorGUILayout.LabelField("Setup from URL", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _urlInput = EditorGUILayout.TextField(_urlInput);
            if (GUILayout.Button("Download and Assign Exceptions and Patterns"))
                if (!string.IsNullOrEmpty(_urlInput))
                {
                    var client = new WebClient();
                    try
                    {
                        var result = client.DownloadString(_urlInput);
                        var removeCommentsRegex = @"%[^\r\n]*";
                        result = Regex.Replace(result, removeCommentsRegex, "");
                        var retrievePatternsRegex = @"\\patterns\{(.*?)\}";
                        var retrieveExceptionsRegex = @"\\hyphenation\{(.*?)\}";
                        TryRetrieve(result, retrievePatternsRegex, out var patterns);
                        TryRetrieve(result, retrieveExceptionsRegex, out var exceptions);

                        foreach (var splittingCharacter in _splittingCharacters)
                        {
                            if (patterns.Contains(splittingCharacter)) patterns = patterns.Replace(splittingCharacter, ";");
                            if (exceptions.Contains(splittingCharacter)) exceptions = exceptions.Replace(splittingCharacter, ";");
                        }

                        while (patterns.Contains(";;")) patterns = patterns.Replace(";;", ";");
                        while (exceptions.Contains(";;")) exceptions = exceptions.Replace(";;", ";");

                        InitPatterns(patterns.Split(";", StringSplitOptions.RemoveEmptyEntries));
                        InitExceptions(exceptions.Split(";", StringSplitOptions.RemoveEmptyEntries));
                    }
                    catch (Exception exception)
                    {
                        Debug.Log(exception);
                        throw;
                    }
                }
            EditorGUILayout.EndHorizontal();
        }

        private string RemoveTexComments(string source)
        {
            string removeCommentsRegex = @"%[^\r\n]*";
            return Regex.Replace(source, removeCommentsRegex, "");
        }

        private void DisplayClearButtons(HyphenationAsset asset)
        {
            GUILayout.Space(25);
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Everything"))
            {
                asset.Dispose();
                _rootTree = new SerializableHyphenationDataTree();
                TrySet("rootTree", _rootTree);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                _exceptions = GetExceptions();
            }

            if (GUILayout.Button("Clear Patterns"))
            {
                _rootTree.ClearPatterns();
                asset.Dispose();
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                _exceptions = GetExceptions();
            }

            if (GUILayout.Button("Clear Exceptions"))
            {
                _rootTree.ClearExceptions();
                asset.Dispose();
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                _exceptions = GetExceptions();
            }

            EditorGUILayout.EndHorizontal();
        }

        private string[] GetExceptions()
        {
            var asset = ((HyphenationAsset)(target));
            return _rootTree.GetExceptions().Select(x => asset.HyphenateText(x, "-")).OrderBy(x => x).ToArray();
        }

        private bool TryRetrieve(string source, string regex, out string result)
        {
            var match = Regex.Match(source, regex, RegexOptions.Singleline);
            if (match.Success && match.Groups.Count > 1)
            {
                result = match.Groups[1].Value;
                return true;
            }

            result = string.Empty;
            return false;
        }

        public void InitPatterns(IEnumerable<string> patternsCollection)
        {
            _rootTree ??= new SerializableHyphenationDataTree();
            _rootTree.ClearPatterns();
        
            var textResult = new StringBuilder();
            foreach (var pattern in patternsCollection)
            {
                var result = new List<byte>();
                var insertedDigit = false;
                for (var i = 0; i < pattern.Length; i++)
                {
                    if (byte.TryParse(pattern[i].ToString(), out var value))
                    {
                        result.Insert(result.Count, value);
                        insertedDigit = true;
                    }
                    else
                    {
                        if (!insertedDigit) result.Insert(result.Count, value);
                        insertedDigit = false;
                        textResult.Append(pattern[i]);
                    }
                }

                _rootTree.AddPattern(textResult.ToString(),result.ToArray());
                textResult.Clear();
                TrySet("rootTree", _rootTree);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        private void InitExceptions(IEnumerable<string> exceptions)
        {
            _rootTree ??= new SerializableHyphenationDataTree();
            _rootTree.ClearExceptions();
            foreach (var exception in exceptions)
            {
                _rootTree.AddException(exception);
            }
            TrySet("rootTree", _rootTree);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    
        private T TryGet<T>(string fieldName)
        {
            return (T)typeof(HyphenationAsset).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        }
    
        private void TrySet<T>(string fieldName, T value)
        {
            typeof(HyphenationAsset).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target,value);
        }
    }
}
#endif