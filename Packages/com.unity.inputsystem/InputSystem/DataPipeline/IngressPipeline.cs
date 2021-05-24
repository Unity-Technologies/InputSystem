using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.InputSystem.DataPipeline.Demux;
using UnityEngineInternal.Input;

namespace UnityEngine.InputSystem.DataPipeline
{
    // TODO split into two/three so we can run user code between demuxer and main pipeline
    [BurstCompile(CompileSynchronously = true)] // , DisableSafetyChecks = true 
    internal unsafe struct IngressPipeline : IJob, IDisposable
    {
        public Demuxer demuxer;
        public Dataset dataset;
        public DataPipeline dataPipeline;

        [NativeDisableUnsafePtrRestriction] [NoAlias] [ReadOnly] public void* eventBuffer;

        public void Execute()
        {
            demuxer.Execute(eventBuffer);
            var demuxedData = demuxer.results.ToNativeSlice();
            dataset.CalculateIngressLengths(demuxedData);
            
            // <-- run user pipelines here, for initial mapping
            // TODO maybe mapping needs to be done via some configuration rather relying on pipeline stages?
            // that way we wouldn't need to break out from bursted job here
            
            dataPipeline.Map(dataset.ToDatasetProxy());
            dataset.AoSToSoa(demuxedData);

            // <-- run user pipelines here, before our pipelines
            
            dataPipeline.Execute(dataset.ToDatasetProxy());
            
            // <-- run user pipelines here, post our pipelines
        }

        public void Dispose()
        {
            demuxer.Dispose();
            dataset.Dispose();
            dataPipeline.Dispose();
        }
    }
}