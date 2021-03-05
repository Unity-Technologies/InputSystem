using System;
using System.Runtime.CompilerServices;
using UnityEditor.Graphs;

namespace UnityEngine.InputSystem.DmytroRnD
{
    internal enum ComputationalOperation
    {
        None,
        RaisingEdgeTrigger,
        FallingEdgeTrigger
    }

    internal struct ComputationalTask
    {
        public ComputationalOperation Operation;
        public int[] Inputs; // array of step function indices
        public int Output;
        public int Configuration;

        public void Configure(ComputationalOperation setOperation, int[] setInputs, int setOutput, int setConfiguration)
        {
            Operation = setOperation;
            Inputs = setInputs;
            Output = setOutput;
            Configuration = setConfiguration;
        }
    }

    internal struct TriggerConfiguration
    {
        public float Level;
    }

    internal struct ComputationalGraph
    {
        // node is a step function
        // all edges to the node form a task
        public StepFunction[] StepFunctions; // all step functions
        public ComputationalTask[] Tasks; // all tasks topologically sorted
        public int[] DeviceChannelOffsets; // offset in step functions for every device id, -1 if none
        //public long TimestampAtLastUpdate;
        public long MinTimestampAtCurrentUpdate; // TODO this is a bit overly complicated
        public long MaxTimestampAtCurrentUpdate;
        public bool NoUpdatesLastFrame;
        public TriggerConfiguration[] TriggerConfigurations; // all trigger configurations

        public void Setup()
        {
            // TODO currently hardcoding a graph, but this should be generated in runtime from a model
            // and regenerated every time model changes, like a new device discovered, or input actions added/removed
            StepFunctions = new StepFunction[(int) MouseDemux.Channels.Count + 5 * 2];
            for (var i = 0; i < StepFunctions.Length; ++i)
                StepFunctions[i].Setup();
            StepFunctions[(int) MouseDemux.Channels.ButtonLeft].SetDebugName("btnleft");

            var triggersOffset = (int) MouseDemux.Channels.Count;

            Tasks = new ComputationalTask[10];
            for (int i = 0; i < 5; ++i)
            {
                Tasks[i * 2 + 0].Configure(ComputationalOperation.RaisingEdgeTrigger,
                    new[] {(int) MouseDemux.Channels.ButtonLeft + i}, triggersOffset + i * 2 + 0, 0);
                Tasks[i * 2 + 1].Configure(ComputationalOperation.FallingEdgeTrigger,
                    new[] {(int) MouseDemux.Channels.ButtonLeft + i}, triggersOffset + i * 2 + 1, 0);
                StepFunctions[triggersOffset + i * 2 + 0].SetDebugName("trrise");
                StepFunctions[triggersOffset + i * 2 + 1].SetDebugName("trfall");
            }

            TriggerConfigurations = new TriggerConfiguration[1];
            TriggerConfigurations[0].Level = 0.5f;


            DeviceChannelOffsets = new int[1024];
            MinTimestampAtCurrentUpdate = 0;
            MaxTimestampAtCurrentUpdate = 0;
            NoUpdatesLastFrame = false;
        }

        public void Clear()
        {
            if (StepFunctions != null)
            {
                for (var i = 0; i < StepFunctions.Length; ++i)
                    StepFunctions[i].Clear();
            }

            StepFunctions = new StepFunction[0];
            Tasks = new ComputationalTask[0];
            DeviceChannelOffsets = new int[] { };
            MinTimestampAtCurrentUpdate = 0;
            MaxTimestampAtCurrentUpdate = 0;
            NoUpdatesLastFrame = false;
            TriggerConfigurations = new TriggerConfiguration[0];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DropOldStates(long timestamp)
        {
            // drop all events that were received before last update
            // TODO instead this should be about some duration,
            // like for gesture recognition a duration of 1-2 seconds 
            
            for (var i = 0; i < StepFunctions.Length; ++i)
                StepFunctions[i].DropAllOlderThan(timestamp);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordDeviceEvent(int deviceId, int deviceChannel, long timestamp, float value)
        {
            var offset = DeviceChannelOffsets[deviceId];
            if (offset < 0)
                return;
            StepFunctions[offset + deviceChannel].Record(timestamp, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Compute()
        {
            for (var taskIndex = 0; taskIndex < Tasks.Length; ++taskIndex)
            {
                var dirty = false;
                for (var j = 0; j < Tasks[taskIndex].Inputs.Length; ++j)
                {
                    if (StepFunctions[Tasks[taskIndex].Inputs[j]].IsDirty())
                    {
                        dirty = true;
                        break;
                    }
                }

                if (!dirty)
                    continue;

                var task = Tasks[taskIndex];
                switch (task.Operation)
                {
                    case ComputationalOperation.RaisingEdgeTrigger:
                    {
                        var valueBeforeFirstSample = 0.0f;
                        uint fromIndex = 0;
                        uint count = 0;
                        if (StepFunctions[task.Inputs[0]]
                            .ResolveDirty(ref valueBeforeFirstSample, ref fromIndex, ref count))
                        {
                            var prevValue = valueBeforeFirstSample;
                            var level = TriggerConfigurations[task.Configuration].Level;
                            for (uint j = 0; j < count; ++j)
                            {
                                var (timestamp, value) = StepFunctions[task.Inputs[0]].Get(j + fromIndex);
                                //Debug.Log($"{value} => {level} && {prevValue} < {level}");
                                if (value >= level && prevValue < level)
                                {
                                    StepFunctions[task.Output].Record(timestamp, 1.0f);
                                }
                                

                                prevValue = value;
                            }
                        }

                        break;
                    }
                    case ComputationalOperation.FallingEdgeTrigger:
                    {
                        var valueBeforeFirstSample = 0.0f;
                        uint fromIndex = 0;
                        uint count = 0;
                        if (StepFunctions[task.Inputs[0]]
                            .ResolveDirty(ref valueBeforeFirstSample, ref fromIndex, ref count))
                        {
                            var prevValue = valueBeforeFirstSample;
                            var level = TriggerConfigurations[task.Configuration].Level;
                            //Debug.Log($"trying for falling edge");

                            for (var j = 0; j < count; ++j)
                            {
                                var (timestamp, value) = StepFunctions[task.Inputs[0]].Get((uint) (j + fromIndex));

                                //Debug.Log($"{prevValue} -> {value}");

                                if (value < level && prevValue >= level)
                                {
                                    StepFunctions[task.Output].Record(timestamp, 1.0f);
                                    //Debug.Log($"released at {timestamp}");
                                    
                                    //Debug.Log($"was released and {StepFunctions[(int) MouseDemux.Channels.Count + 1].LatestTimestamp()} >= {MinTimestampAtCurrentUpdate}");
                                }

                                prevValue = value;
                            }
                        }

                        break;
                    }
                }
            }

            for (var i = 0; i < StepFunctions.Length; ++i)
                StepFunctions[i].MarkAsClear();
        }


        public bool DebugMouseLeftWasPressedThisFrame()
        {
            return Core.Graph.StepFunctions[(int) MouseDemux.Channels.Count + 0].LatestTimestamp() >=
                   Core.Graph.MinTimestampAtCurrentUpdate;
        }

        public bool DebugMouseLeftWasReleasedThisFrame()
        {
            return Core.Graph.StepFunctions[(int) MouseDemux.Channels.Count + 1].LatestTimestamp() >=
                   Core.Graph.MinTimestampAtCurrentUpdate;
        }
    }
}