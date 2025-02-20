using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace neoludic.QuickHyphenation.DataNodes
{
    [Serializable]
    public class SerializableHyphenationDataTree : IDisposable
    {
        [SerializeField] private List<SerializedHyphenationDataNode> patterns = new(), exceptions = new();
        [NonSerialized] public HyphenationDataNode root = new HyphenationDataNode();
        [NonSerialized] private bool _setup = false;
        
        public void TryInit()
        {
            if(_setup) return;
            root?.Clear();
            root ??= new HyphenationDataNode();
            for (int i = 0; i < patterns.Count; i++)
            {
                patterns[i].TryAddToNode(root);
            }

            for (int i = 0; i < exceptions.Count; i++)
            {
                exceptions[i].TryAddToNode(root);
            }
            _setup = true;
        }
        public void Dispose()
        {
            root.Clear();
            _setup = false;
        }

        #region Patterns
        public void AddPattern(string key, byte[] value)
        {
            SerializedHyphenationDataNode serializedNode = patterns.FirstOrDefault(x => x.Equals(key));
            if (serializedNode == null)
            {
                serializedNode = new SerializedHyphenationDataNode(key, value);
                patterns.Add(serializedNode);
            }
            else serializedNode.SetBytes(value);
            serializedNode.TryAddToNode(root);
        }

        public void RemovePattern(string key)
        {
            SerializedHyphenationDataNode serializedNode = patterns.FirstOrDefault(x => x.Equals(key));
            if (serializedNode != null)
            {
                patterns.Remove(serializedNode);
                this.root.TryRemove(key,0,this.root,this.root);
            }
        }

        public void ClearPatterns() => patterns.Clear();
        public int GetPatternsCount() => patterns.Count;
        #endregion
        #region Exceptions
        public void AddException(string key)
        {
            string lookUpKey = key.Replace("-", "");
            RemoveException(key);
            SerializedHyphenationDataNode node = SerializedHyphenationDataNode.FromException(key);
            exceptions.Add(node);
            node.TryAddToNode(this.root);
        }

        public string[] GetExceptions() => exceptions.Select(x => x.GetString()).ToArray();

        public void RemoveException(string key)
        {
            string lookUpKey = key.Replace("-", "");
            SerializedHyphenationDataNode node = exceptions.FirstOrDefault(x => x.Equals(lookUpKey));
            if (node != null)
            {
                exceptions.Remove(node);
                this.root.TryRemove(lookUpKey,0,this.root,this.root);
            }
        }

        public void ClearExceptions() => exceptions.Clear();
        public int GetExceptionsCount() => exceptions.Count;
        #endregion
    }
}