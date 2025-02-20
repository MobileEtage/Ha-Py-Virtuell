using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace neoludic.QuickHyphenation.DataNodes
{
    [Serializable]
    public class SerializedHyphenationDataNode
    {
        [SerializeField] private string key;
        [SerializeField] private byte[] bytes;
        public bool Equals(string value) => key.Equals(value);
        public byte[] GetBytes() => bytes;
        public string GetString() => key;
        public void SetBytes(byte[] newValue) => bytes = newValue;
        
        public SerializedHyphenationDataNode() {}

        public static SerializedHyphenationDataNode FromException(string exception)
        {
            var result = FromPattern(exception.Replace("-", "9"));
            for (int i = 0; i < result.bytes.Length; i++)
            {
                if (result.bytes[i] != 9) result.bytes[i] = 8;
            }
            return result;
        }

        public static SerializedHyphenationDataNode FromPattern(string pattern)
        {
            StringBuilder stringBuilder = new StringBuilder();
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
                    if (!insertedDigit) result.Insert(result.Count, 0);
                    insertedDigit = false;
                    stringBuilder.Append(pattern[i]);
                }
            }
            return new SerializedHyphenationDataNode()
            {
                bytes = result.ToArray(),
                key = stringBuilder.ToString()
            };
        }

        public SerializedHyphenationDataNode(string key, byte[] value)
        {
            this.key = key;
            bytes = value;
        }

        public void TryAddToNode(HyphenationDataNode node)
        {
            node.TryAdd(key,0,bytes);
        }
    }
}