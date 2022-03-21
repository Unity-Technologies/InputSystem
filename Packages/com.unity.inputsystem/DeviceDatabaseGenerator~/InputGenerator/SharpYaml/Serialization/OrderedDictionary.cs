// Copyright (c) 2014 SharpYaml - Silicon Studio Corp.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SharpYaml.Serialization
{
    class OrderedDictionary<TKey, TValue> : IOrderedDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>
    {
        private readonly KeyedCollection items = new KeyedCollection();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            items.Add(item);
        }

        public void Clear()
        {
            items.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return items.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return items.Remove(item);
        }

        public int Count { get { return items.Count; } }

        public bool IsReadOnly { get { return false; } }

        public void Add(TKey key, TValue value)
        {
            items.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public bool ContainsKey(TKey key)
        {
            return items.Contains(key);
        }

        public bool Remove(TKey key)
        {
            return items.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!items.Contains(key))
            {
                value = default(TValue);
                return false;
            }

            value = items[key].Value;
            return true;
        }

        public void Insert(int index, TKey key, TValue value)
        {
            items.Insert(index, new KeyValuePair<TKey, TValue>(key, value));
        }

        public void RemoveAt(int index)
        {
            items.RemoveAt(index);
        }

        public int IndexOf(KeyValuePair<TKey, TValue> item)
        {
            return items.IndexOf(item);
        }

        public void Insert(int index, KeyValuePair<TKey, TValue> item)
        {
            items.Insert(index, item);
        }

        public int IndexOf(TKey key)
        {
            if (!items.Contains(key))
                return -1;

            return items.IndexOf(items[key]);
        }

        public KeyValuePair<TKey, TValue> this[int index] { get { return items[index]; } set { items[index] = value; } }

        public TValue this[TKey key]
        {
            get { return items[key].Value; }
            set
            {
                var item = new KeyValuePair<TKey, TValue>(key, value);
                var index = IndexOf(key);
                if (index != -1)
                    items[index] = item;
                else
                    items.Add(item);
            }
        }

        public ICollection<TKey> Keys { get { return items.Select(x => x.Key).ToList(); } }

        public ICollection<TValue> Values { get { return items.Select(x => x.Value).ToList(); } }

        class KeyedCollection : KeyedCollection<TKey, KeyValuePair<TKey, TValue>>
        {
            protected override TKey GetKeyForItem(KeyValuePair<TKey, TValue> item)
            {
                return item.Key;
            }
        }
    }
}