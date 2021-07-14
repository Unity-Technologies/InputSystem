using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEditor;
using UnityEngine.InputSystem.DataPipeline.Collections;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.DataPipeline
{
    [BurstCompile(CompileSynchronously = true)]
    internal unsafe struct CompressMouseEvents : IJob
    {
        public static void ProcessEvents(InputUpdateType updateType, double processUntilTimestamp, ref InputEventBuffer events)
        {
            // There are currently two problems in editor update loop:
            // - burst compilation is not very happy, it fails to reliably replace managed job with bursted job
            // - temp allocator is missing native guarding scope, so it might leak, this is also potentially true if we run a job on main thread
            if (!EditorApplication.isPlaying || updateType == InputUpdateType.Editor)
                return;

            if (events.sizeInBytes == 0 || events.sizeInBytes == InputEventBuffer.BufferSizeUnknown)
                return;

            var resultEventCount = 0;
            var resultEventSizeInBytes = 0L;

            new CompressMouseEvents
            {
                updateType = updateType,
                processUntilTimestamp = processUntilTimestamp,
                eventPtr = events.bufferPtr.ToPointer(),
                eventCount = events.eventCount,
                eventSizeInBytes = events.sizeInBytes,
                resultEventCount = &resultEventCount,
                resultEventSizeInBytes = &resultEventSizeInBytes
            }.Run();

            events.Shrink(resultEventCount, resultEventSizeInBytes);
        }

        public InputUpdateType updateType;
        public double processUntilTimestamp;

        [NativeDisableUnsafePtrRestriction]
        public InputEvent* eventPtr;

        public int eventCount;
        public long eventSizeInBytes;
        
        // job fields are copied, so to read results back from a job we need to write to a pointer
        [NativeDisableUnsafePtrRestriction]
        public int* resultEventCount;
        [NativeDisableUnsafePtrRestriction]
        public long* resultEventSizeInBytes;

        public void Execute()
        {
            // Step 1: early out if any mouse event is propagated via delta state event.
            // It's a bit involved to support compression of delta state events,
            // and given that no backend sends mouse delta events today it's probably safe to early out.
            var currentEvent = eventPtr;
            var eventPtrs = new NativeArray<IntPtr>(eventCount + 1, Allocator.Temp);
            for (var i = 0; i < eventCount; ++i)
            {
                eventPtrs[i] = (IntPtr) currentEvent;

                if (currentEvent->type == DeltaStateEvent.Type)
                {
                    var stateEvent = DeltaStateEvent.FromUnchecked(currentEvent);
                    if (stateEvent->stateFormat == MouseState.Format)
                        return;
                }

                currentEvent = InputEvent.GetNextInMemory(currentEvent);
            }

            // Step 2: compress move events in-place, mark redundant events for skipping 
            NativeMouseState2* previousState = null;
            var previousStateIndex = 0;
            var skipEvent = new NativeArray<bool>(eventCount, Allocator.Temp);
            currentEvent = eventPtr;
            for (var i = 0; i < eventCount; ++i)
            {
                var nextEvent = InputEvent.GetNextInMemory(currentEvent);

                if (currentEvent->type == StateEvent.Type)
                {
                    var stateEvent = StateEvent.FromUnchecked(currentEvent);
                    if (stateEvent->stateFormat == MouseState.Format)
                    {
                        var currentState = (NativeMouseState2*) stateEvent->state;

                        // If buttons and other states stay the same,
                        // modify current event in-place and mark previous one for skipping
                        if (previousState != null &&
                            currentState->buttons == previousState->buttons &&
                            currentState->displayIndex == previousState->displayIndex &&
                            currentState->clickCount == previousState->clickCount)
                        {
                            currentState->delta += previousState->delta;
                            currentState->scroll += previousState->scroll;
                            skipEvent[previousStateIndex] = true;
                        }

                        // Remember current mouse state event
                        previousState = currentState;
                        previousStateIndex = i;
                    }
                }

                currentEvent = nextEvent;
            }
            eventPtrs[eventCount] = (IntPtr) currentEvent; // remember the end pointer

            // Step 3: move data around, think about it as RLE encoding of sorts
            for (var i = 0; i < eventCount;)
            {
                if (!skipEvent[i])
                {
                    i++;
                    continue;
                }

                // find how many events to skip first
                var toSkip = 1;
                for (var j = i + 1; j < eventCount; ++j)
                {
                    if (!skipEvent[i])
                        break;
                    toSkip++;
                }

                // find how much data we need to move
                var toMove = 0;
                for (var j = i + toSkip; j < eventCount; ++j)
                {
                    if (skipEvent[i])
                        break;
                    toMove++;
                }

                // do the actual moving
                var dst = (byte*) eventPtrs[i];
                var src = (byte*) eventPtrs[i + toSkip];
                var next = (byte*) eventPtrs[i + toSkip + toMove];
                var skipLength = src - dst;
                var moveLength = next - src;
                if (moveLength > 0)
                    UnsafeUtility.MemMove(dst, src, moveLength);

                // decrease amount of events and bytes
                eventCount -= toSkip;
                eventSizeInBytes -= skipLength;

                // skip ahead of two sections
                i += toSkip + toMove;
            }

            skipEvent.Dispose();
            eventPtrs.Dispose();

            *resultEventCount = eventCount;
            *resultEventSizeInBytes = eventSizeInBytes;
        }

        // should be 30
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        private struct NativeMouseState2
        {
            [FieldOffset(0)] public Vector2 position;
            [FieldOffset(8)] public Vector2 delta;
            [FieldOffset(16)] public Vector2 scroll;
            [FieldOffset(24)] public ushort buttons;
            [FieldOffset(26)] public ushort displayIndex; // unused
            [FieldOffset(28)] public ushort clickCount; // unused
        }
    }
}