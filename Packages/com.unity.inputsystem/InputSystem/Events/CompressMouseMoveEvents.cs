using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine.InputSystem.LowLevel;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if !UNITY_INPUT_SYSTEM_DO_NOT_USE_BURST
using Unity.Burst;
#endif

namespace UnityEngine.InputSystem
{
    // Utility to merges consecutive mouse move events into one.
    // It operates directly on event buffer and does all modification in-place. It can only shrink event buffer.
    // Use this define to completely disable any burst stuff, might be helpful if there are any issues with it.
#if !UNITY_INPUT_SYSTEM_DO_NOT_USE_BURST
    [BurstCompile(CompileSynchronously = true)]
#endif
    internal unsafe struct CompressMouseMoveEvents : IJob
    {
        private static ProfilerMarker s_ProfilerMarker = new ProfilerMarker("CompressMouseMoveEvents");

        public static void ProcessEvents(InputUpdateType updateType, double processUntilTimestamp,
            ref InputEventBuffer events)
        {
            using (s_ProfilerMarker.Auto())
            {
#if UNITY_EDITOR
                // There are currently a few problems with our native callback from editor update loop:
                // - Burst compilation is not very happy, it fails to reliably replace managed job with bursted job.
                // - Temp allocator guard scope is missing in native, so it might leak, this also could be potentially true if we run a job on main thread.
                if (!EditorApplication.isPlaying || updateType == InputUpdateType.Editor)
                    return;
#endif

                if (events.sizeInBytes == 0 || events.sizeInBytes == InputEventBuffer.BufferSizeUnknown)
                    return;

                var resultEventCount = 0;
                var resultEventSizeInBytes = 0L;

                new CompressMouseMoveEvents
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
        }

        public InputUpdateType updateType;
        public double processUntilTimestamp;

        [NativeDisableUnsafePtrRestriction] public InputEvent* eventPtr;

        public int eventCount;
        public long eventSizeInBytes;

        // Job fields are copied, so to read results back from a job we need to write to a pointer.
        [NativeDisableUnsafePtrRestriction] public int* resultEventCount;
        [NativeDisableUnsafePtrRestriction] public long* resultEventSizeInBytes;

        public void Execute()
        {
            // Write down no-op results now.
            *resultEventCount = eventCount;
            *resultEventSizeInBytes = eventSizeInBytes;

            // Step 1: early out if any mouse event is propagated via delta state event.
            // It's a bit involved to support compression of delta state events,
            // and given that no backend sends mouse delta events today it's probably safe to early out.
            // Also calculate correct event count for fixed update.
            var currentEvent = eventPtr;
            var eventPtrs = new NativeArray<IntPtr>(eventCount + 1, Allocator.Temp);
            var eventToProcessCount = eventCount;
            for (var i = 0; i < eventCount; ++i)
            {
                eventPtrs[i] = (IntPtr)currentEvent;

                if (currentEvent->type == DeltaStateEvent.Type)
                {
                    var stateEvent = DeltaStateEvent.FromUnchecked(currentEvent);
                    if (stateEvent->stateFormat == MouseState.Format)
                        return;
                }

                currentEvent = InputEvent.GetNextInMemory(currentEvent);

                if (updateType == InputUpdateType.Fixed && currentEvent->internalTime >= processUntilTimestamp)
                {
                    eventToProcessCount = i;
                    break;
                }
            }
            eventPtrs[eventToProcessCount] = (IntPtr)currentEvent;  // Remember the end pointer, will make life easier later.

            // Step 2: compress move events in-place, mark redundant events for skipping
            NativeMouseStateBurstFriendly* previousState = null;
            var previousStateIndex = 0;
            var skipEvent = new NativeArray<bool>(eventToProcessCount, Allocator.Temp);
            currentEvent = eventPtr;
            var skipFirstOne = true;
            for (var i = 0; i < eventToProcessCount; ++i)
            {
                NativeMouseStateBurstFriendly* currentState = null;

                // Find if current event is mouse state event.
                if (currentEvent->type == StateEvent.Type)
                {
                    var stateEvent = StateEvent.FromUnchecked(currentEvent);
                    if (stateEvent->stateFormat == MouseState.Format)
                    {
                        currentState = (NativeMouseStateBurstFriendly*)stateEvent->state;
                    }
                }

                if (currentState != null)
                {
                    // If buttons and other states stay the same,
                    // modify current event in-place and mark previous one for skipping.
                    if (previousState != null &&
                        currentState->buttons == previousState->buttons &&
                        currentState->displayIndex == previousState->displayIndex &&
                        currentState->clickCount == previousState->clickCount)
                    {
                        currentState->delta += previousState->delta;
                        currentState->scroll += previousState->scroll;
                        skipEvent[previousStateIndex] = true;
                    }

                    if (skipFirstOne)
                    {
                        // Skip first one so we don't need to track device state.
                        // For example imagine we have two frames:
                        // - First frame, one event, button not pressed, delta x is 5
                        //   t=1,left_btn=0,dx=5
                        // - Second frame, three events, in first one button gets pressed, second and third are just move events.
                        //   t=2,left_btn=1,dx=10   t=3,left_btn=1,dx=20   t=4,left_btn=1,dx=30
                        // If we would not skip first event in second frame it would get merged with second and third events to make:
                        //                                                 t=4,left_btn=1,dx=60
                        // So now if we have a button action on left button it would trigger on timestamp=4 instead of timestamp=2.
                        // Which is suboptimal because we pretend like mouse press was happening much later in time.
                        // A more robust way would be to read device state and compare to that, but that's involves too much poking and device tracking.
                        // A compromise is to always preserve first event and base from it, so we will end up with:
                        //   t=2,left_btn=1,dx=10                          t=4,left_btn=1,dx=50
                        // This way any potential mouse press/release/etc will be correctly reported, and the rest of events compressed.
                        // Paying price of one event for this simplicity is a reasonable tradeoff at this point.
                        skipFirstOne = false;
                    }
                    else
                    {
                        // Remember current mouse state event
                        previousState = currentState;
                        previousStateIndex = i;
                    }
                }
                else
                {
                    // Stop compression if we have anything else. This is important to catch hidden data dependencies.
                    // For example let's say we have one button composite of mouse position and ctrl key:
                    // t=1,dx=10 t=2,ctrl=1 t=3,dx=20 t=4,ctrl=0 t=5,dx=30
                    // If we would compress all mouse move events together we will get:
                    //           t=2,ctrl=1           t=4,ctrl=0 t=5,dx=60
                    // And look there are no movement of mouse during ctrl being pressed.
                    // To fix this we stop compression if any other event is present in the buffer, and we will get:
                    // t=1,dx=10 t=2,ctrl=1 t=3,dx=20 t=4,ctrl=0 t=5,dx=30
                    // E.g. no compression will take place here.
                    // This is over conservative because we're lacking data to understand if events have dependency or not.
                    previousState = null;
                    previousStateIndex = 0;
                }

                currentEvent = InputEvent.GetNextInMemory(currentEvent);
            }

            // Step 3: move data around, think about it as RLE encoding of sorts.
            for (var i = 0; i < eventToProcessCount;)
            {
                if (!skipEvent[i])
                {
                    i++;
                    continue;
                }

                // Let's say we have event buffer of 11 events, where p is for preserve, s is for skip:
                //   0123456789a
                //   pppssssppps
                // We end up in this line with i = 3.
                // Then toSkip will be 4 and toMove = 3.
                // So we gonna have dst, src and next as following:
                //   0123456789a
                //   pppssssppps
                //   ---^---^--^
                // Then we jump and i becomes 10.

                // find how many events to skip first
                var toSkip = 1;
                for (var j = i + 1; j < eventToProcessCount; ++j)
                {
                    if (!skipEvent[j])
                        break;
                    toSkip++;
                }

                // find how much data we need to move
                var toMove = 0;
                for (var j = i + toSkip; j < eventToProcessCount; ++j)
                {
                    if (skipEvent[j])
                        break;
                    toMove++;
                }

                // do the actual moving
                var dst = (byte*)eventPtrs[i];
                var src = (byte*)eventPtrs[i + toSkip];
                var next = (byte*)eventPtrs[i + toSkip + toMove];
                var skipLength = src - dst;
                var moveLength = next - src;
                if (moveLength > 0)
                    UnsafeUtility.MemMove(dst, src, moveLength);

                // decrease amount of events and bytes
                *resultEventCount -= toSkip;
                *resultEventSizeInBytes -= skipLength;

                // skip ahead of two sections
                i += toSkip + toMove;
            }

            skipEvent.Dispose();
            eventPtrs.Dispose();
        }

        // should be 30
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        private struct NativeMouseStateBurstFriendly
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
