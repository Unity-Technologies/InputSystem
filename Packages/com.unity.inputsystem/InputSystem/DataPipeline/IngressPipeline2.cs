using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.DataPipeline.Collections;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.DataPipeline
{
    public class IngressPipeline2 : IDisposable
    {
        public IngressPipeline2()
        {
        }

        public unsafe void ProcessEvents(InputUpdateType updateType, double processUntilTimestamp,
            ref InputEventBuffer events)
        {
            if (events.sizeInBytes == InputEventBuffer.BufferSizeUnknown)
                return;

            if (events.sizeInBytes == 0)
                return;

            var shouldSkipArray = new ResizableNativeArray<bool>(events.eventCount);
            shouldSkipArray.ResizeToFit(events.eventCount);
            var shouldSkip = shouldSkipArray.ToManagedSpan();
            for (var i = 0; i < shouldSkip.Length; ++i)
                shouldSkip[i] = false;

            InputEvent* current = events.bufferPtr;

            for (var i = 0; i < events.eventCount; ++i)
            {
                var next = InputEvent.GetNextInMemory(current);

                if (current->type == StateEvent.Type)
                {
                    var stateEvent = StateEvent.FromUnchecked(current);
                    if (stateEvent->stateFormat == MouseState.Format)
                        shouldSkip[i] = true;
                }
                else if (current->type == DeltaStateEvent.Type)
                {
                    var stateEvent = DeltaStateEvent.FromUnchecked(current);
                    if (stateEvent->stateFormat == MouseState.Format)
                        shouldSkip[i] = true;
                }

                if (!events.Contains(next))
                    break;

                current = next;
            }
            
            current = events.bufferPtr;
            for (var i = 0; i < events.eventCount; ++i)
            {
                var next = InputEvent.GetNextInMemory(current);

                current = next;
            }

            events.Shrink(0, 0);
        }

        public void Dispose()
        {
        }
    }
}