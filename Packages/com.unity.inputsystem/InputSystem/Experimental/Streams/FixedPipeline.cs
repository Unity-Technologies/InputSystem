using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.InputSystem.Experimental
{
    // Find a way to generate fixed operations as a dependency chain
    internal static class FixedPipeline
    {
        public enum Operation : ushort
        {
            None,
            
            Equal,
            NotEqual,
            GreaterThan,
            GreaterOrEqualThan,
            LessThan,
            LessOrEqualThan,
                
            FixedStep,
            DynamicStep
        }
        
        [StructLayout(LayoutKind.Explicit)]
        internal struct FixedBlock
        {
            [FieldOffset(0)] public Operation operation;
            [FieldOffset(2)] public ushort sizeBytes;
        }

        internal struct FixedStep
        {
            public void Apply(bool initialValue, ref NativeSlice<bool> input, ref NativeArray<bool> output)
            {
                for (var i = 0; i < input.Length; ++i)
                {
                    output[i] = input[i] != input[i - 1];
                }
            }
        }
        
        // Setup job dependencies based on dependency graph https://docs.unity3d.com/Manual/JobSystemJobDependencies.html
        
        internal struct FixedStepJob : IJob
        {
            public NativeSlice<bool> input;
            public NativeArray<bool> output;
            
            public void Execute()
            {
                new FixedStep().Apply(false, ref input, ref output);
            }
        }
    }
}