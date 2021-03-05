using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.DmytroRnD
{
    internal struct NativeDeviceState
    {
        private int _deviceId;
        private bool _ready;

        private bool _gotFirstEvent;

        private NativeArray<ulong> _lastState; // last device binary state struct, padded to 8 bytes
        private NativeArray<ulong> _currentState; // place where current state is copied to pad to 8 bytes
        private NativeArray<ulong> _enabledBits; // enabled controls bits
        private NativeArray<ulong> _changedBits; // changed bits

        public void Setup(int deviceId, int stateSize)
        {
            var count = ((stateSize + 7) / 8);
            _deviceId = deviceId;
            _ready = true;
            _gotFirstEvent = false;
            _lastState = new NativeArray<ulong>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _currentState = new NativeArray<ulong>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _enabledBits = new NativeArray<ulong>(count, Allocator.Persistent);
            _changedBits = new NativeArray<ulong>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // enable all bits for now
            for (var i = 0; i < _enabledBits.Length; ++i)
                _enabledBits[i] = ulong.MaxValue;
        }

        public void Clear()
        {
            _deviceId = 0;
            _ready = false;
            if (_lastState.IsCreated)
                _lastState.Dispose();
            if (_currentState.IsCreated)
                _currentState.Dispose();
            if (_enabledBits.IsCreated)
                _enabledBits.Dispose();
            if (_changedBits.IsCreated)
                _changedBits.Dispose();
            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ulong* PreDemux(int deviceId, void* state, int size)
        {
            Debug.Assert(_deviceId == deviceId);
            Debug.Assert(_ready == true);

            // just copy the whole struct so we get padded data
            UnsafeUtility.MemCpy(_currentState.GetUnsafePtr(), state, Math.Min(size, _currentState.Length * 8));

            if (!_gotFirstEvent)
            {
                // invert all bits so all of them are in changed mask first time
                for (var i = 0; i < _currentState.Length; ++i)
                    _lastState[i] = ~_currentState[i];
                _gotFirstEvent = true;
            }

            // calculate change mask
            for (var i = 0; i < _currentState.Length; ++i)
                _changedBits[i] = (_lastState[i] ^ _currentState[i]) & _enabledBits[i];

            // copy to last state
            // TODO change to front-back buffers
            UnsafeUtility.MemCpy(_lastState.GetUnsafePtr(), _currentState.GetUnsafeReadOnlyPtr(),
                _lastState.Length * 8);

            return (ulong*) _changedBits.GetUnsafeReadOnlyPtr();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInitialized(int deviceId) // TODO rename to something else
        {
            return (_deviceId == deviceId) && _ready;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInitialized()
        {
            return _ready;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string LatestStateForDebug()
        {
            return string.Join("", _currentState.Select(x => Convert.ToString((long) x, 16).PadLeft(16, '0')));
        }
    };
}