// Copyright (c) SharpYaml - Alexandre Mutel
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SharpYaml.Events;

namespace SharpYaml.Model {
    public class YamlMapping : YamlContainer, IDictionary<YamlElement, YamlElement>, IList<KeyValuePair<YamlElement, YamlElement>> {
        private MappingStart _mappingStart;
        private readonly MappingEnd _mappingEnd;

        private readonly List<YamlElement> _keys;
        private readonly Dictionary<YamlElement, YamlElement> _contents;

        private Dictionary<string, YamlValue> stringKeys;

        public YamlMapping() {
            _mappingStart = new MappingStart();
            _mappingEnd = new MappingEnd();
            _keys = new List<YamlElement>();
            _contents = new Dictionary<YamlElement, YamlElement>();
        }

        YamlMapping(MappingStart mappingStart, MappingEnd mappingEnd, List<YamlElement> keys, Dictionary<YamlElement, YamlElement> contents, YamlNodeTracker tracker) {
            if (tracker == null) {
                _keys = keys;
                _contents = contents;
            } else {
                _keys = new List<YamlElement>();
                _contents = new Dictionary<YamlElement, YamlElement>();

                Tracker = tracker;

                foreach (var key in keys)
                    Add(key, contents[key]);
            }

            MappingStart = mappingStart;
            this._mappingEnd = mappingEnd;
        }

        public MappingStart MappingStart {
            get => _mappingStart;
            set {
                var oldValue = _mappingStart;

                _mappingStart = value;

                if (Tracker != null)
                    Tracker.OnMappingStartChanged(this, oldValue, value);
            }
        }
        
        internal MappingEnd MappingEnd { get { return _mappingEnd;  } }

        public override string Anchor {
            get { return _mappingStart.Anchor; }
            set {
                MappingStart = new MappingStart(value,
                    _mappingStart.Tag,
                    _mappingStart.IsImplicit,
                    _mappingStart.Style,
                    _mappingStart.Start,
                    _mappingStart.End);
            }
        }

        public override string Tag {
            get { return _mappingStart.Tag; }
            set {
                MappingStart = new MappingStart(_mappingStart.Anchor,
                    value,
                    string.IsNullOrEmpty(value),
                    _mappingStart.Style,
                    _mappingStart.Start,
                    _mappingStart.End);
            }
        }

        public override YamlStyle Style {
            get { return _mappingStart.Style; }
            set {
                MappingStart = new MappingStart(_mappingStart.Anchor,
                    _mappingStart.Tag,
                    _mappingStart.IsImplicit,
                    value,
                    _mappingStart.Start,
                    _mappingStart.End);
            }
        }

        public override bool IsCanonical { get { return _mappingStart.IsCanonical; } }

        public override bool IsImplicit {
            get { return _mappingStart.IsImplicit; }
            set {
                MappingStart = new MappingStart(_mappingStart.Anchor,
                    _mappingStart.Tag,
                    value,
                    _mappingStart.Style,
                    _mappingStart.Start,
                    _mappingStart.End);
            }
        }

        public static YamlMapping Load(EventReader eventReader, YamlNodeTracker tracker) {
            var mappingStart = eventReader.Allow<MappingStart>();

            List<YamlElement> keys = new List<YamlElement>();
            Dictionary<YamlElement, YamlElement> contents = new Dictionary<YamlElement, YamlElement>();
            while (!eventReader.Accept<MappingEnd>()) {
                var key = ReadElement(eventReader, tracker);
                var value = ReadElement(eventReader, tracker);

                if (value == null)
                    throw new Exception();

                keys.Add(key);
                contents[key] = value;
            }

            var mappingEnd = eventReader.Allow<MappingEnd>();

            return new YamlMapping(mappingStart, mappingEnd, keys, contents, tracker);
        }
        
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<YamlElement, YamlElement>> GetEnumerator() {
            return _keys.Select(k => new KeyValuePair<YamlElement, YamlElement>(k, _contents[k])).GetEnumerator();
        }

        void ICollection<KeyValuePair<YamlElement, YamlElement>>.Add(KeyValuePair<YamlElement, YamlElement> item) {
            Add(item.Key, item.Value);
        }

        public void Clear() {
            var values = Tracker == null ? null : this.ToList();

            _contents.Clear();
            _keys.Clear();

            stringKeys = null;

            if (Tracker != null) {
                for (int i = values.Count - 1; i >= 0; i--)
                    Tracker.OnMappingRemovePair(this, values[i], i, null);
            }
        }

        bool ICollection<KeyValuePair<YamlElement, YamlElement>>.Contains(KeyValuePair<YamlElement, YamlElement> item) {
            return _contents.ContainsKey(item.Key);
        }

        void ICollection<KeyValuePair<YamlElement, YamlElement>>.CopyTo(KeyValuePair<YamlElement, YamlElement>[] array, int arrayIndex) {
            ((ICollection<KeyValuePair<YamlElement, YamlElement>>)_contents).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<YamlElement, YamlElement>>.Remove(KeyValuePair<YamlElement, YamlElement> item) {
            return Remove(item.Key);
        }

        public int Count { get { return _contents.Count; } }
        public bool IsReadOnly { get { return false; } }

        public void Add(YamlElement key, YamlElement value) {
            _contents.Add(key, value);
            _keys.Add(key);

            if (stringKeys != null && key is YamlValue) {
                stringKeys[((YamlValue) key).Value] = (YamlValue) key;
            }

            if (Tracker != null) {
                key.Tracker = Tracker;
                value.Tracker = Tracker;

                Tracker.OnMappingAddPair(this, new KeyValuePair<YamlElement, YamlElement>(key, value), _keys.Count - 1, null);
            }
        }

        public override YamlNodeTracker Tracker {
            get { return base.Tracker; }
            internal set {
                if (Tracker == value)
                    return;

                base.Tracker = value;

                for (var i = 0; i < _keys.Count; i++) {
                    var val = _contents[_keys[i]];

                    _keys[i].Tracker = value;
                    val.Tracker = value;

                    Tracker.OnMappingAddPair(this, new KeyValuePair<YamlElement, YamlElement>(_keys[i], val), i, null);
                }
            }
        }

        public bool ContainsKey(YamlElement key) {
            return _contents.ContainsKey(key);
        }

        public bool ContainsKey(string key) {
            if (stringKeys == null)
                stringKeys = Keys.OfType<YamlValue>().ToDictionary(k => k.Value, k => k);

            return stringKeys.ContainsKey(key);
        }

        public bool Remove(YamlElement key) {
            var index = _keys.IndexOf(key);
            if (index >= 0) {
                RemoveAt(index);
                return true;
            }

            return false;
        }


        public bool Remove(string key) {
            if (stringKeys == null)
                stringKeys = Keys.OfType<YamlValue>().ToDictionary(k => k.Value, k => k);

            YamlValue yaml;
            if (!stringKeys.TryGetValue(key, out yaml))
                return false;

            if (Remove(yaml)) {
                stringKeys.Remove(key);
                return true;
            }

            return false;
        }

        public bool TryGetValue(YamlElement key, out YamlElement value) {
            return _contents.TryGetValue(key, out value);
        }

        public bool TryGetValue(string key, out YamlElement value) {
            if (stringKeys == null)
                stringKeys = Keys.OfType<YamlValue>().ToDictionary(k => k.Value, k => k);

            YamlValue yamlKey;
            if (!stringKeys.TryGetValue(key, out yamlKey)) {
                value = null;
                return false;
            }

            return TryGetValue(yamlKey, out value);
        }

        public YamlElement this[YamlElement key] {
            get {
                if (!_contents.ContainsKey(key))
                    return null;
                return _contents[key];
            }
            set {
                var keyAdded = false;
                if (!_contents.ContainsKey(key)) {
                    _keys.Add(key);
                    keyAdded = true;

                    if (stringKeys != null && key is YamlValue) {
                        stringKeys[((YamlValue)key).Value] = (YamlValue)key;
                    }
                }

                YamlElement oldContents = null;
                if (!keyAdded && Tracker != null) {
                    oldContents = _contents[key];
                    
                    if (stringKeys != null && key is YamlValue) {
                        stringKeys[((YamlValue)key).Value] = (YamlValue)key;
                    }
                }

                _contents[key] = value;

                if (Tracker != null) {
                    if (keyAdded) {
                        key.Tracker = Tracker;
                        value.Tracker = Tracker;
                        Tracker.OnMappingAddPair(this, new KeyValuePair<YamlElement, YamlElement>(key, value),
                            _keys.Count - 1, null);
                    }
                    else {
                        value.Tracker = Tracker;
                        Tracker.OnMappingPairChanged(this, _keys.IndexOf(key),
                            new KeyValuePair<YamlElement, YamlElement>(key, oldContents),
                            new KeyValuePair<YamlElement, YamlElement>(key, value));
                    }
                }
            }
        }

        public YamlElement this[string key] {
            get {
                if (stringKeys == null)
                    stringKeys = Keys.OfType<YamlValue>().ToDictionary(k => k.Value, k => k);

                if (!stringKeys.ContainsKey(key))
                    return null;

                return this[stringKeys[key]];
            }
            set {
                if (stringKeys == null)
                    stringKeys = Keys.OfType<YamlValue>().ToDictionary(k => k.Value, k => k);

                if (!stringKeys.ContainsKey(key)) 
                    stringKeys[key] = new YamlValue(key);
                
                this[stringKeys[key]] = value;
            }
        }

        public ICollection<YamlElement> Keys { get { return _keys; } }
        public ICollection<YamlElement> Values { get { return _contents.Values; } }

        public int IndexOf(KeyValuePair<YamlElement, YamlElement> item) {
            return _keys.IndexOf(item.Key);
        }

        public void Insert(int index, KeyValuePair<YamlElement, YamlElement> item) {
            if (_contents.ContainsKey(item.Key))
                throw new Exception("Key already present.");

            _keys.Insert(index, item.Key);
            _contents[item.Key] = item.Value;

            if (stringKeys != null && item.Key is YamlValue) {
                stringKeys[((YamlValue)item.Key).Value] = (YamlValue) item.Key;
            }

            if (Tracker != null) {
                item.Key.Tracker = Tracker;
                item.Value.Tracker = Tracker;

                ICollection<KeyValuePair<YamlElement, YamlElement>> nextChildren = null;
                if (index < _contents.Count - 1)
                    nextChildren = this.Skip(index + 1).ToArray();
                
                Tracker.OnMappingAddPair(this, item, index, nextChildren);
            }
        }

        public void RemoveAt(int index) {
            var key = _keys[index];
            var value = _contents[key];

            _keys.RemoveAt(index);
            _contents.Remove(key);

            if (stringKeys != null && key is YamlValue) {
                stringKeys.Remove(((YamlValue) key).Value);
            }

            if (Tracker != null) {
                IEnumerable<KeyValuePair<YamlElement, YamlElement>> nextChildren = null;
                if (index < _contents.Count)
                    nextChildren = this.Skip(index);

                Tracker.OnMappingRemovePair(this, new KeyValuePair<YamlElement, YamlElement>(key, value), index, nextChildren);
            }
        }

        public KeyValuePair<YamlElement, YamlElement> this[int index] {
            get { return new KeyValuePair<YamlElement, YamlElement>(_keys[index], _contents[_keys[index]]); }
            set {
                if (_keys[index] != value.Key && _contents.ContainsKey(value.Key))
                    throw new Exception("Key already present at a different index.");

                var oldKey = _keys[index];
                var oldValue = _contents[oldKey];

                if (_keys[index] != value.Key) {
                    _contents.Remove(_keys[index]);
                }

                if (stringKeys != null && oldKey is YamlValue) {
                    stringKeys[((YamlValue)oldKey).Value] = (YamlValue)oldKey;
                }

                if (stringKeys != null && value.Key is YamlValue) {
                    stringKeys[((YamlValue)value.Key).Value] = (YamlValue)value.Key;
                }

                _keys[index] = value.Key;
                _contents[value.Key] = value.Value;


                if (Tracker != null) {
                    value.Key.Tracker = Tracker;
                    value.Value.Tracker = Tracker;
                    Tracker.OnMappingPairChanged(this, index,
                        new KeyValuePair<YamlElement, YamlElement>(oldKey, oldValue),
                        value);
                }
            }
        }

        public override YamlNode DeepClone(YamlNodeTracker tracker = null) {
            var keysClone = new List<YamlElement>(_keys.Count);
            for (var i = 0; i < _keys.Count; i++)
                keysClone.Add((YamlElement)_keys[i].DeepClone());

            var cloneContents = new Dictionary<YamlElement, YamlElement>();

            for (var i = 0; i < _keys.Count; i++)
                cloneContents[keysClone[i]] = (YamlElement) _contents[_keys[i]].DeepClone();

            return new YamlMapping(_mappingStart,
                _mappingEnd,
                keysClone,
                cloneContents,
                tracker);
        }
    }
}