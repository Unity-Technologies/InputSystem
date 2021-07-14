using System;
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
    internal unsafe struct TestBurstJob : IJob, IDisposable
    {
        internal struct Data
        {
            public InputUpdateType updateType;
            public double processUntilTimestamp;
            public InputEvent* eventPtr;
            public int eventCount;
            public long eventSizeInBytes;
        }

        public NativeArray<Data> dataContainer;
        public ResizableNativeArray<bool> shouldSkipArray;

        public TestBurstJob(int minEventsCount)
        {
            dataContainer = new NativeArray<Data>(1, Allocator.Persistent);
            shouldSkipArray = new ResizableNativeArray<bool>(minEventsCount);
        }

        public void Execute()
        {
            var data = dataContainer[0];

            shouldSkipArray.ResizeToFit(data.eventCount, growOnly: true);
            var shouldSkip = shouldSkipArray.ToNativeSlice();
            for (var i = 0; i < shouldSkip.Length; ++i)
                shouldSkip[i] = false;
            
            var current = (InputEvent*)data.eventPtr;
            
            for (var i = 0; i < data.eventCount; ++i)
            {
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
            
                current = InputEvent.GetNextInMemory(current);
            }

            current = (InputEvent*)data.eventPtr;
            for (var i = 0; i < data.eventCount; ++i)
            {
                var next = InputEvent.GetNextInMemory(current);
            
                current = next;
            }

            data.eventCount = 0;
            data.eventSizeInBytes = 0;
            dataContainer[0] = data;
        }

        public void Dispose()
        {
            dataContainer.Dispose();
            shouldSkipArray.Dispose();
        }
    }
    
    internal class IngressPipeline2 : IDisposable
    {
        //internal TestBurstJob job;
        public IngressPipeline2()
        {
            //job = new TestBurstJob(1000);
        }

        public unsafe void ProcessEvents(InputUpdateType updateType, double processUntilTimestamp, ref InputEventBuffer events)
        {
            if (events.sizeInBytes == InputEventBuffer.BufferSizeUnknown)
                return;

            if (events.sizeInBytes == 0)
                return;

            // job.dataContainer[0] = new TestBurstJob.Data
            // {
            //     updateType = updateType,
            //     processUntilTimestamp = processUntilTimestamp,
            //     eventPtr = events.bufferPtr.ToPointer(),
            //     eventCount = events.eventCount,
            //     eventSizeInBytes = events.sizeInBytes
            // };
            //
            // if (EditorApplication.isPlaying)
            //     job.Run();
            // else
            //     job.Execute();
            //
            // var result = job.dataContainer[0];
            //
            // events.Shrink(result.eventCount, result.eventSizeInBytes);
            events.Shrink(0, 0);
        }

        public void Dispose()
        {
            //job.Dispose();
        }
    }
}