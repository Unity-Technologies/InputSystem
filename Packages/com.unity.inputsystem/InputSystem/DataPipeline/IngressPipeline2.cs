using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.DataPipeline
{
    public class IngressPipeline2 : IDisposable
    {
        public IngressPipeline2()
        {
        }

        public unsafe void ProcessEvents(InputUpdateType updateType, double processUntilTimestamp, ref InputEventBuffer events)
        {
            if (events.sizeInBytes == InputEventBuffer.BufferSizeUnknown)
                return;

            if (events.sizeInBytes == 0)
                return;

            InputEvent* current = events.bufferPtr; 
            for(var i = 0; i < events.eventCount; ++i)
            {
                var next = InputEvent.GetNextInMemory(current);

                if (current->type == StateEvent.Type)
                {
                    var stateEvent = StateEvent.FromUnchecked(current);

                    if (stateEvent->stateFormat == MouseState.Format)
                    {
                    }
                }
                else if (current->type == DeltaStateEvent.Type)
                {
                    
                }
                
                if (!events.Contains(next))
                    break;

                current = next;
            }
        }
        
        public void Dispose()
        {
        }
    }
}