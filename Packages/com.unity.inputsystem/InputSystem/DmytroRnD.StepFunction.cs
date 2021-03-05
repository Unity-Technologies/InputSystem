using System;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;

namespace UnityEngine.InputSystem.DmytroRnD
{
    // right continuous step function
    // https://en.wikipedia.org/wiki/Step_function#/media/File:StepFunctionExample.png
    // with value before first sample set at -inf
    internal struct StepFunction
    {
        private ResizeableRingBuffer<long> _timestamps;
        private ResizeableRingBuffer<float> _values;

        private string _debugName;

        private float _valueEarliest;
        private float _valueLatest;
        // TODO timestampEarliest?
        private long _timestampLatest;

        private bool _dirty;
        private uint _firstDirty;

        public void Setup(float initialValue = 0.0f)
        {
            _timestamps.Setup();
            _values.Setup();
            _valueEarliest = initialValue;
            _valueLatest = initialValue;
            _timestampLatest = 0;
            _dirty = false;
            _firstDirty = 0;
        }

        public void SetDebugName(string debugName)
        {
            _debugName = debugName;
        }

        public void Clear()
        {
            _timestamps.Clear();
            _values.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Record(long timestamp, float value)
        {
            if (!_dirty)
                _firstDirty = _timestamps.Head;
            _timestamps.Push(timestamp);
            _values.Push(value);
            _timestampLatest = timestamp;
            _valueLatest = value;
            _dirty = true;

            //Debug.Log($"rec({timestamp},{value}),{_timestamps.ToString()}{_values.ToString()}");
            //Debug.Log($"{ToString()}");
        }

        public void ResolveRange(long fromTimestampInclusive, long toTimestampInclusive,
            ref float valueBeforeFirstSample, ref int fromIndex, ref int toIndex)
        {
        }

        public void ResolveFrom(long fromTimestampInclusive, ref float valueBeforeFirstSample, ref int fromIndex,
            ref int toIndex)
        {
        }

        public void ResolveAll(ref float valueBeforeFirstSample, ref uint index, ref uint count)
        {
            valueBeforeFirstSample = _valueEarliest;
            index = _timestamps.Tail;
            count = _timestamps.Head - _timestamps.Tail;
        }

        public bool ResolveDirty(ref float valueBeforeFirstSample, ref uint index, ref uint count)
        {
            // TODO this function is broken
            if (!_dirty)
            {
                valueBeforeFirstSample = _valueLatest;
                index = 0;
                count = 0;
                return false;
            }

            valueBeforeFirstSample = _firstDirty == _timestamps.Tail ? _valueEarliest : _values.Get(_firstDirty - 1);
            index = _firstDirty;
            count = _timestamps.Head - _firstDirty;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkAsClear()
        {
            _dirty = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDirty()
        {
            return _dirty;
        }

        // drop all < timestamp
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DropAllOlderThan(long timestamp)
        {
            //var any = false;
            // TODO optimize maybe?
            while ((!_timestamps.Empty()) && (_timestamps.Get(_timestamps.Tail) < timestamp) && (_dirty && _firstDirty > _timestamps.Tail || !_dirty))
            {
                _valueEarliest = _values.Get(_timestamps.Tail);
                _timestamps.PopN(1);
                _values.PopN(1);
                //any = true;
            }

            /*
            if (any)
            {
                Debug.Log($"drp<{timestamp},{_timestamps.ToString()}{_values.ToString()}");
                Debug.Log($"{ToString()}");
            }
            */

            // HM?
            //if (_timestamps.Empty())
            //    _timestampLatest = 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Count()
        {
            return _timestamps.Head - _timestamps.Tail;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (long timestamp, float value) Get(uint index)
        {
            return (_timestamps.Get(index), _values.Get(index));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ValueBeforeFirstSample()
        {
            return _valueEarliest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long LatestTimestamp()
        {
            return _timestampLatest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float LatestValue()
        {
            return _valueLatest;
        }
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{_debugName}({_valueEarliest})");
            for (uint i = 0; i < _timestamps.Count(); ++i)
                sb.Append($"->({_timestamps.Get(_timestamps.Tail+i)},{_values.Get(_values.Tail+i)})");
            float valueBefore = 0;
            uint firstIndex = 0;
            uint count = 0;
            if(ResolveDirty(ref valueBefore, ref firstIndex, ref count))
            {
                sb.Append($",dirty:({valueBefore})");
                for(uint i = 0; i < count; ++i)
                    sb.Append($"->({_timestamps.Get(i + firstIndex)},{_values.Get(i+firstIndex)})");
            }
            return sb.ToString();
        }
    }
}