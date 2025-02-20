using System;
using System.Collections.Generic;

namespace neoludic.QuickHyphenation.DataNodes
{
    public class HyphenationDataNode
    {
        [NonSerialized] private readonly Dictionary<int,HyphenationDataNode> _dict = new ();
        [NonSerialized] private byte[] _array = Array.Empty<byte>();

        private const int DOT_LETTER = '.';

        public bool TryGetNode(Char character, out HyphenationDataNode node)
        {
            return _dict.TryGetValue(CharToInt(character), out node);
        }

        public void ApplyTo(ref byte[] bytes, int startIndex)
        {
            for (int i = 0; i < _array.Length; i++)
            {
                if(_array[i] > bytes[startIndex+i]) bytes[startIndex+i] = _array[i];
            }
        }

        private int CharToInt(Char character)
        {
            if (Char.IsLetter(character))
            {
                if (Char.IsUpper(character)) return Char.ToLowerInvariant(character);
                else return character;
            }
            else return DOT_LETTER;
        }
        
        //Setup and Assembly
        public void TryAdd(string myEntireKey, int indexInKey, byte[] values)
        {
            if (indexInKey == myEntireKey.Length)
            {
                _array = values;
                return;
            }
            
            int charValue = CharToInt(myEntireKey[indexInKey]);
            HyphenationDataNode node;
            if (_dict.TryGetValue(charValue, out node)){}
            else
            {
                node = new HyphenationDataNode();
                _dict.TryAdd(charValue, node);
            }
            indexInKey++;
            node.TryAdd(myEntireKey,indexInKey,values);
        }

        public void TryRemove(string myEntireKey, int indexInKey,HyphenationDataNode parent, HyphenationDataNode root)
        {
            if (indexInKey == myEntireKey.Length)
            {
                if(myEntireKey.Length == 0) return;
                else parent._dict.Remove(CharToInt(myEntireKey[indexInKey - 1]));
                if(this == root) return;
                if(parent._dict.Count == 0 && (parent._array == null || parent._array.Length == 0)) root.TryRemove(myEntireKey.Remove(indexInKey-1,1),0,root,root); 
                return;
            }
            
            int charValue = CharToInt(myEntireKey[indexInKey]);
            HyphenationDataNode node;
            if (_dict.TryGetValue(charValue, out node))
            {
                indexInKey++;
                node.TryRemove(myEntireKey, indexInKey, this, root);
            }
        }

        public void Clear()
        {
            foreach (var kvp in _dict)
            {
                kvp.Value.Clear();
            }
            _dict.Clear();
            _array = Array.Empty<byte>();
        }
    }
}