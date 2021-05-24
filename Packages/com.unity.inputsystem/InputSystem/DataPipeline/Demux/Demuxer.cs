using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEngine.InputSystem.DataPipeline.Collections;
using UnityEngine.InputSystem.DataPipeline.Demux.Dynamic;
using UnityEngine.InputSystem.Utilities;
using UnityEngineInternal.Input;

namespace UnityEngine.InputSystem.DataPipeline.Demux
{
    internal unsafe struct Demuxer : IDisposable
    {
        // it would be cool for this to be a hashmap
        [ReadOnly] public NativeArray<DeviceDescription> deviceDescriptions;

        // indexed via dataFieldsOffset/dataFieldsCount
        [ReadOnly] public NativeArray<DemuxerDataField> dataFields;

        // states of all devices, every device must be at least two ulongs long
        // indexed via stateOffsetInULongs / stateSizeInULongs
        public NativeArray<ulong> deviceStates;

        // indexed via validStateIndex
        public NativeArray<bool> deviceHasValidState;

        // with size of max state struct, must be at least two ulongs long
        public NativeArray<ulong> changedBits;

        public ResizableNativeArray<DemuxedData> results;

        private static readonly ProfilerMarker s_MarkerExecute = new ProfilerMarker("DynamicDemuxer");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(void* rawInputEventBuffer)
        {
            s_MarkerExecute.Begin();
            results.Clear();

            var inputEventBuffer = (NativeInputEventBuffer2*) rawInputEventBuffer;
            for (long offset = 0; offset < inputEventBuffer->sizeInBytes;)
            {
                var originalOffset = offset;

                var afterOffset = (byte*) inputEventBuffer->eventBuffer + offset;
                var inputEvent = (NativeInputEvent2*) afterOffset;
                var afterInputEvent = afterOffset + NativeInputEvent2.size;
                var afterInputSize = inputEvent->sizeInBytes - NativeInputEvent2.size;

                var timestamp = ConvertTimeToLong(inputEvent->time);

                offset += inputEvent->sizeInBytes;

                switch (inputEvent->type)
                {
                    case NativeInputEventType.State:
                    {
                        var stateEvent = (NativeStateEvent2*) afterInputEvent;
                        var afterStateEvent = afterInputEvent + NativeStateEvent2.size;
                        var afterStateEventSize = afterInputSize - NativeStateEvent2.size;
                        //Debug.Log($"got {inputEvent->type}/{stateEvent->fourCC} at {originalOffset} with size {inputEvent->sizeInBytes}");

                        var (found, deviceDescriptionsIndex) =
                            FindDeviceDescriptor(inputEvent->deviceId, stateEvent->fourCC);

                        if (!found)
                            break;

                        var descr = deviceDescriptions[deviceDescriptionsIndex];
                        SaveStateAndCalculateChangedBits(descr, afterStateEvent, afterStateEventSize);
                        DemuxChangedFields(descr, timestamp);
                        break;
                    }
                    case NativeInputEventType.Delta:
                    {
                        //Debug.Log($"got {inputEvent->type}/??? at {originalOffset} with size {inputEvent->sizeInBytes}");
                        // TODO fix it
                        break;
                    }
                    default:
                        // ignoring other events
                        break;
                }
            }

            results.ShrinkToFit();

            s_MarkerExecute.End();
        }

        public static ulong ConvertTimeToLong(double timeSinceStartupInSeconds)
        {
            // given long max value 9223372036854775807
            // meaning it will span ~292 years
            return (ulong) (timeSinceStartupInSeconds * 1000000000.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (bool found, int deviceDescriptionIndex) FindDeviceDescriptor(int deviceId, int stateEventFourCC)
        {
            for (var i = 0; i < deviceDescriptions.Length; ++i)
            {
                if (deviceDescriptions[i].deviceId != deviceId ||
                    (int) deviceDescriptions[i].stateEventFourCC != stateEventFourCC)
                    continue;

                return (true, i);
            }

            return (false, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SaveStateAndCalculateChangedBits(DeviceDescription descr, void* rawStateData,
            int rawStateDataLength)
        {
            if (rawStateDataLength > descr.stateSizeInULongs * sizeof(ulong))
            {
                Debug.LogError(
                    $"device id '{descr.deviceId}' state event with fourcc '{descr.stateEventFourCC}' size is larger than expected {rawStateDataLength} > {descr.stateSizeInULongs * sizeof(ulong)}, skipping");
                return;
            }

            Debug.Assert(descr.stateSizeInULongs <= changedBits.Length);

            if (deviceHasValidState[descr.validStateIndex])
            {
                // using changedBits to store nextState padded to ulong boundaries
                UnsafeUtility.MemCpy(
                    (ulong*) changedBits.GetUnsafePtr(),
                    rawStateData,
                    rawStateDataLength
                );

                for (var i = 0; i < descr.stateSizeInULongs; ++i)
                    changedBits[i] = deviceStates[i + descr.stateOffsetInULongs] ^ changedBits[i];
            }
            else
            {
                // just marking all bits as changed first time
                for (var i = 0; i < descr.stateSizeInULongs; ++i)
                    changedBits[i] = ulong.MaxValue;

                deviceHasValidState[descr.validStateIndex] = true;
            }

            // storing the state for next time
            UnsafeUtility.MemCpy(
                (ulong*) deviceStates.GetUnsafePtr() + descr.stateOffsetInULongs,
                rawStateData,
                rawStateDataLength
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DemuxChangedFields(DeviceDescription descr, ulong timestamp)
        {
            switch (descr.demuxerType)
            {
                case DemuxerType.StaticMouse:
                    DemuxMouseChangedFields(descr, timestamp);
                    break;
                case DemuxerType.Dynamic:
                    DemuxDynamicChangedFields(descr, timestamp);
                    break;
                default:
                    break;
            }
        }
        
        
        // TODO how this can be moved out to a separate class
        public void DemuxMouseChangedFields(DeviceDescription descr, ulong timestamp)
        {
            var changed = (NativeMouseStateChanged*) changedBits.GetUnsafeReadOnlyPtr();
            var state = (NativeMouseState2*) ((ulong*) deviceStates.GetUnsafeReadOnlyPtr() + descr.stateOffsetInULongs);

            if (changed->position.x != 0)
                PushValue(timestamp, descr.valueStepFunctionOffset + 0, state->position.x);
            if (changed->position.y != 0)
                PushValue(timestamp, descr.valueStepFunctionOffset + 1, state->position.y);
            if (changed->delta.x != 0)
                PushValue(timestamp, descr.valueStepFunctionOffset + 2, state->delta.x);
            if (changed->delta.y != 0)
                PushValue(timestamp, descr.valueStepFunctionOffset + 3, state->delta.y);
            if (changed->scroll.x != 0)
                PushValue(timestamp, descr.valueStepFunctionOffset + 4, state->scroll.x);
            if (changed->scroll.y != 0)
                PushValue(timestamp, descr.valueStepFunctionOffset + 5, state->scroll.y);
            if ((changed->buttons & 0b00001) != 0)
                PushValue(timestamp, descr.valueStepFunctionOffset + 6, ((state->buttons & 0b00001) != 0) ? 1.0f : 0.0f);
            if ((changed->buttons & 0b00010) != 0)
                PushValue(timestamp, descr.valueStepFunctionOffset + 7, ((state->buttons & 0b00010) != 0) ? 1.0f : 0.0f);
            if ((changed->buttons & 0b00100) != 0)
                PushValue(timestamp, descr.valueStepFunctionOffset + 8, ((state->buttons & 0b00100) != 0) ? 1.0f : 0.0f);
            if ((changed->buttons & 0b01000) != 0)
                PushValue(timestamp, descr.valueStepFunctionOffset + 9, ((state->buttons & 0b01000) != 0) ? 1.0f : 0.0f);
            if ((changed->buttons & 0b10000) != 0)
                PushValue(timestamp, descr.valueStepFunctionOffset + 10, ((state->buttons & 0b10000) != 0) ? 1.0f : 0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DemuxDynamicChangedFields(DeviceDescription descr, ulong timestamp)
        {
            for (var fieldIndex = descr.dataFieldsOffset;
                fieldIndex < descr.dataFieldsOffset + descr.dataFieldsCount;
                ++fieldIndex)
            {
                var f = dataFields[fieldIndex];

                // TODO opaque values

                var isChanged = ((changedBits[f.pairIndex] & f.maskA) |
                                 (changedBits[f.pairIndex + 1] & f.maskB)) != 0;

                if (!isChanged)
                    continue;

                var rawData = ((deviceStates[descr.stateOffsetInULongs + f.pairIndex] & f.maskA) >> f.shiftA) +
                              ((deviceStates[descr.stateOffsetInULongs + f.pairIndex + 1] & f.maskB) << f.shiftB);

                var value = float.NaN;

                switch (f.srcType)
                {
                    case SourceDataType.TwosComplementSignedBits:
                    {
                        // TODO ReadTwosComplementMultipleBitsAsInt is broken :(
                        var data = (f.bitSize == 16)
                            ? *(short*) (&rawData)
                            : MemoryHelpers.ReadTwosComplementMultipleBitsAsInt(&rawData, 0, (uint) f.bitSize);
                        switch (f.dstType)
                        {
                            case DestinationDataType.Float32:
                                value = (float) data;
                                break;
                            default:
                                break;
                        }

                        break;
                    }
                    case SourceDataType.ExcessKSignedBits:
                    {
                        var data = MemoryHelpers.ReadExcessKMultipleBitsAsInt(&rawData, 0, (uint) f.bitSize);
                        switch (f.dstType)
                        {
                            case DestinationDataType.Float32:
                                value = (float) data;
                                break;
                            default:
                                break;
                        }

                        break;
                    }
                    case SourceDataType.UnsignedBits:
                    {
                        var data = (uint) rawData;
                        switch (f.dstType)
                        {
                            case DestinationDataType.Float32:
                                value = (float) data;
                                break;
                            default:
                                break;
                        }

                        break;
                    }
                    case SourceDataType.Float32:
                    {
                        var data = *(float*) &rawData;
                        switch (f.dstType)
                        {
                            case DestinationDataType.Float32:
                                value = (float) data;
                                break;
                            default:
                                break;
                        }

                        break;
                    }
                }

                PushValue(timestamp, descr.valueStepFunctionOffset + f.dstValueStepFunctionIndex, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PushValue(ulong timestamp, int valueStepfunctionIndex, float value)
        {
            results.Push(new DemuxedData
            {
                timestamp = timestamp,
                valueStepfunctionIndex = valueStepfunctionIndex,
                value = value
            });
        }

        public void Dispose()
        {
            deviceDescriptions.Dispose();
            dataFields.Dispose();
            deviceStates.Dispose();
            deviceHasValidState.Dispose();
            changedBits.Dispose();
            results.Dispose();
        }

        // wild stuff, the struct size in-memory is 20, but burst is not supporting packed structs, so place 24 here and hope for the best
        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct NativeInputEventBuffer2
        {
            public const int size = 20;

            [FieldOffset(0)] public void* eventBuffer;
            [FieldOffset(8)] public int eventCount;
            [FieldOffset(12)] public int sizeInBytes;
            [FieldOffset(16)] public int capacityInBytes;
        }

        // wild stuff, the struct size in-memory is 20, but burst is not supporting packed structs, so place 24 here and hope for the best
        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct NativeInputEvent2
        {
            public const int size = 20;

            [FieldOffset(0)] public NativeInputEventType type;
            [FieldOffset(4)] public ushort sizeInBytes;
            [FieldOffset(6)] public ushort deviceId;
            [FieldOffset(8)] public double time;
            [FieldOffset(16)] public int eventId;
        }

        [StructLayout(LayoutKind.Explicit, Size = 4)]
        private struct NativeStateEvent2
        {
            public const int size = 4;

            [FieldOffset(0)] public int fourCC;
        }

        // should be 30
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        internal struct NativeMouseStateChanged
        {
            [FieldOffset(0)] public Vector2Int position;
            [FieldOffset(8)] public Vector2Int delta;
            [FieldOffset(16)] public Vector2Int scroll;
            [FieldOffset(24)] public ushort buttons;
            [FieldOffset(26)] private ushort _displayIndex; // unused
            [FieldOffset(28)] public ushort clickCount; // unused
        }

        // should be 30
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        internal struct NativeMouseState2
        {
            [FieldOffset(0)] public Vector2 position;
            [FieldOffset(8)] public Vector2 delta;
            [FieldOffset(16)] public Vector2 scroll;
            [FieldOffset(24)] public ushort buttons;
            [FieldOffset(26)] private ushort _displayIndex; // unused
            [FieldOffset(28)] public ushort clickCount; // unused
        }
    }
}