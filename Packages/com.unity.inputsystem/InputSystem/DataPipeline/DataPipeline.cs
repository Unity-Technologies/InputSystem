using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.InputSystem.DataPipeline.Merger;
using UnityEngine.InputSystem.DataPipeline.Processor;
using UnityEngine.InputSystem.DataPipeline.SlidingWindow;
using UnityEngine.InputSystem.DataPipeline.TypeConversion;

namespace UnityEngine.InputSystem.DataPipeline
{
    internal struct DataPipeline : IDisposable
    {
        [ReadOnly] public NativeArray<float> enumsToFloatsLut;
        [ReadOnly] public NativeArray<EnumToFloat> enumsToFloats;

        [ReadOnly] public NativeArray<Two1DsTo2D> two1DsTo2Ds; // TODO where this should be?

        [ReadOnly] public NativeArray<Vec2ToMagnitude> vec2sToMagnitudes;

        [ReadOnly] public NativeArray<Processor1D> process1Ds;
        [ReadOnly] public NativeArray<Processor2D> process2Ds;
        [ReadOnly] public NativeArray<Processor3D> process3Ds;

        [ReadOnly] public NativeArray<Accumulate1D> accumulate1Ds;

        [ReadOnly] public NativeArray<Latest1D> latest1Ds;
        [ReadOnly] public NativeArray<MaxValue1D> maxValue1Ds;
        [ReadOnly] public NativeArray<MaxValue2D> maxValue2Ds;

        private static readonly ProfilerMarker s_MarkerMap = new ProfilerMarker("DataPipelineMap");
        private static readonly ProfilerMarker s_MarkerExecute = new ProfilerMarker("DataPipelineExecute");
        private static readonly ProfilerMarker s_MarkerEnumToFloat = new ProfilerMarker("EnumToFloat");
        private static readonly ProfilerMarker s_MarkerTwo1DsTo2D = new ProfilerMarker("Two1DsTo2D");
        private static readonly ProfilerMarker s_MarkerVec2ToMagnitude = new ProfilerMarker("Vec2ToMagnitude");
        private static readonly ProfilerMarker s_MarkerProcessor1D = new ProfilerMarker("Processor1D");
        private static readonly ProfilerMarker s_MarkerProcessor2D = new ProfilerMarker("Processor2D");
        private static readonly ProfilerMarker s_MarkerProcessor3D = new ProfilerMarker("Processor3D");
        private static readonly ProfilerMarker s_MarkerAccumulate1D = new ProfilerMarker("Accumulate1D");
        private static readonly ProfilerMarker s_MarkerLatest1D = new ProfilerMarker("Latest1D");
        private static readonly ProfilerMarker s_MarkerMaxValue1D = new ProfilerMarker("MaxValue1D");
        private static readonly ProfilerMarker s_MarkerMaxValue2D = new ProfilerMarker("MaxValue2D");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map(DatasetProxy datasetProxy)
        {
            using (s_MarkerMap.Auto())
            {
                for (var i = 0; i < enumsToFloats.Length; ++i)
                    enumsToFloats[i].Map(datasetProxy);

                for (var i = 0; i < two1DsTo2Ds.Length; ++i)
                    two1DsTo2Ds[i].Map(datasetProxy);

                for (var i = 0; i < vec2sToMagnitudes.Length; ++i)
                    vec2sToMagnitudes[i].Map(datasetProxy);

                for (var i = 0; i < process1Ds.Length; ++i)
                    process1Ds[i].Map(datasetProxy);

                for (var i = 0; i < process2Ds.Length; ++i)
                    process2Ds[i].Map(datasetProxy);

                for (var i = 0; i < process3Ds.Length; ++i)
                    process3Ds[i].Map(datasetProxy);

                for (var i = 0; i < accumulate1Ds.Length; ++i)
                    accumulate1Ds[i].Map(datasetProxy);

                for (var i = 0; i < latest1Ds.Length; ++i)
                    latest1Ds[i].Map(datasetProxy);

                for (var i = 0; i < maxValue1Ds.Length; ++i)
                    maxValue1Ds[i].Map(datasetProxy);

                for (var i = 0; i < maxValue2Ds.Length; ++i)
                    maxValue2Ds[i].Map(datasetProxy);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(DatasetProxy datasetProxy)
        {
            using (s_MarkerExecute.Auto())
            {
                using (s_MarkerEnumToFloat.Auto())
                    for (var i = 0; i < enumsToFloats.Length; ++i)
                        enumsToFloats[i].Execute(datasetProxy);

                using (s_MarkerTwo1DsTo2D.Auto())
                    for (var i = 0; i < two1DsTo2Ds.Length; ++i)
                        two1DsTo2Ds[i].Execute(datasetProxy);

                using (s_MarkerVec2ToMagnitude.Auto())
                    for (var i = 0; i < vec2sToMagnitudes.Length; ++i)
                        vec2sToMagnitudes[i].Execute(datasetProxy);

                using (s_MarkerProcessor1D.Auto())
                    for (var i = 0; i < process1Ds.Length; ++i)
                        process1Ds[i].Execute(datasetProxy);

                using (s_MarkerProcessor2D.Auto())
                    for (var i = 0; i < process2Ds.Length; ++i)
                        process2Ds[i].Execute(datasetProxy);

                using (s_MarkerProcessor3D.Auto())
                    for (var i = 0; i < process3Ds.Length; ++i)
                        process3Ds[i].Execute(datasetProxy);

                using (s_MarkerAccumulate1D.Auto())
                    for (var i = 0; i < accumulate1Ds.Length; ++i)
                        accumulate1Ds[i].Execute(datasetProxy);

                using (s_MarkerLatest1D.Auto())
                    for (var i = 0; i < latest1Ds.Length; ++i)
                        latest1Ds[i].Execute(datasetProxy);

                using (s_MarkerMaxValue1D.Auto())
                    for (var i = 0; i < maxValue1Ds.Length; ++i)
                        maxValue1Ds[i].Execute(datasetProxy);

                using (s_MarkerMaxValue2D.Auto())
                    for (var i = 0; i < maxValue2Ds.Length; ++i)
                        maxValue2Ds[i].Execute(datasetProxy);
            }
        }

        public void Dispose()
        {
            enumsToFloatsLut.Dispose();
            enumsToFloats.Dispose();
            two1DsTo2Ds.Dispose();
            vec2sToMagnitudes.Dispose();

            process1Ds.Dispose();
            process2Ds.Dispose();
            process3Ds.Dispose();

            accumulate1Ds.Dispose();

            latest1Ds.Dispose();
            maxValue1Ds.Dispose();
            maxValue2Ds.Dispose();
        }
    }
}