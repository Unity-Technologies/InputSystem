using System;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.InputSystem.DataPipeline.Collections;
using UnityEngine.InputSystem.DataPipeline.Demux;

namespace UnityEngine.InputSystem.DataPipeline
{
    internal struct Dataset : IDisposable
    {
        public ResizableNativeArray<ulong> timestamps;
        public NativeArray<int> timestampAxisIndexToLength;
        public NativeArray<int> timestampAxisIndexToMaxLength;
        public NativeArray<int> timestampAxisIndexToOffset;
        public NativeArray<ulong> timestampAxisIndexToPreviousRunValue;

        public ResizableNativeArray<float> values;
        public NativeArray<int> valueAxisIndexToOffset;
        public NativeArray<int> valueAxisIndexToTimestampIndex; // TODO make readonly
        public NativeArray<float> valueAxisIndexToPreviousRunValue;

        // Opaque offsets are in bytes.
        public ResizableNativeArray<byte> opaqueValues;
        public NativeArray<int> opaqueValueAxisIndexToOffset;

        public NativeArray<int> opaqueValueIndexToTimestampIndex; // TODO make readonly
        // TODO prev value

        private static readonly ProfilerMarker s_MarkerCalculateIngressLengths =
            new ProfilerMarker("CalculateIngressLengths");

        private static readonly ProfilerMarker s_MarkerAoSToSoa = new ProfilerMarker("AoSToSoA");

        public void CalculateIngressLengths(NativeSlice<DemuxedData> demuxedData)
        {
            using (s_MarkerCalculateIngressLengths.Auto())
            {
                // -----------------------------------
                // sanity check
                Debug.Assert(timestampAxisIndexToLength.Length == timestampAxisIndexToMaxLength.Length);
                Debug.Assert(timestampAxisIndexToLength.Length == timestampAxisIndexToOffset.Length);
                Debug.Assert(timestampAxisIndexToLength.Length == timestampAxisIndexToPreviousRunValue.Length);
                Debug.Assert(valueAxisIndexToOffset.Length == valueAxisIndexToTimestampIndex.Length);
                Debug.Assert(valueAxisIndexToOffset.Length == valueAxisIndexToPreviousRunValue.Length);
                Debug.Assert(opaqueValueAxisIndexToOffset.Length == opaqueValueIndexToTimestampIndex.Length);

                // -----------------------------------
                // store values from prev run
                var timestampsSlice = timestamps.ToNativeSlice();
                for (var i = 0; i < timestampAxisIndexToLength.Length; ++i)
                {
                    var offset = timestampAxisIndexToOffset[i];
                    var size = timestampAxisIndexToLength[i];
                    timestampAxisIndexToPreviousRunValue[i] = (size > 0) ? timestampsSlice[offset + size - 1] : timestampAxisIndexToPreviousRunValue[i];
                }

                var valuesSlice = values.ToNativeSlice();
                for (var i = 0; i < valueAxisIndexToOffset.Length; ++i)
                {
                    var timestampsAxisIndex = valueAxisIndexToTimestampIndex[i];
                    var offset = valueAxisIndexToOffset[i];
                    var size = timestampAxisIndexToLength[timestampsAxisIndex];
                    valueAxisIndexToPreviousRunValue[i] = (size > 0) ? valuesSlice[offset + size - 1] : valueAxisIndexToPreviousRunValue[i];
                }

                // -----------------------------------
                // reset lengths
                for (var i = 0; i < timestampAxisIndexToLength.Length; ++i)
                    timestampAxisIndexToLength[i] = 0;
                for (var i = 0; i < timestampAxisIndexToLength.Length; ++i)
                    timestampAxisIndexToMaxLength[i] = 0;

                // -----------------------------------
                // calculate the lenghts
                for(var i = 0; i < demuxedData.Length; ++i)
                {
                    // TODO opaque values
                    var timestampsAxisIndex = valueAxisIndexToTimestampIndex[demuxedData[i].valueStepfunctionIndex];
                    timestampAxisIndexToLength[timestampsAxisIndex]++;
                    timestampAxisIndexToMaxLength[timestampsAxisIndex]++;
                }
            }
        }

        public void AoSToSoa(NativeSlice<DemuxedData> demuxedData)
        {
            using (s_MarkerAoSToSoa.Auto())
            {
                timestamps.Clear();
                values.Clear();

                // -------------
                // calculate offsets

                var timestampsCount = 0;
                for (var i = 0; i < timestampAxisIndexToLength.Length; ++i)
                {
                    timestampAxisIndexToOffset[i] = timestampsCount;
                    var size = timestampAxisIndexToLength[i];
                    timestampsCount += size;
                }

                var valuesCount = 0;
                for (var i = 0; i < valueAxisIndexToOffset.Length; ++i)
                {
                    valueAxisIndexToOffset[i] = valuesCount;
                    var timestampsAxisIndex = valueAxisIndexToTimestampIndex[i];
                    var size = timestampAxisIndexToLength[timestampsAxisIndex];
                    valuesCount += size;
                }

                // -------------
                // allocate

                timestamps.ResizeToFit(timestampsCount);
                values.ResizeToFit(valuesCount);

                // -------------
                // fill-in the data

                var timestampsSlice = timestamps.ToNativeSlice();
                for(var i = 0; i < demuxedData.Length; ++i)
                {
                    // TODO maybe store resolved index in demuxed data so we avoid calculating it over again
                    // TODO is the data actually sorted?
                    var timestampsAxisIndex = valueAxisIndexToTimestampIndex[demuxedData[i].valueStepfunctionIndex];
                    var offset = timestampAxisIndexToOffset[timestampsAxisIndex]++;
                    timestampsSlice[offset] = demuxedData[i].timestamp;
                }

                var valuesSlice = values.ToNativeSlice();
                for(var i = 0; i < demuxedData.Length; ++i)
                {
                    var offset = valueAxisIndexToOffset[demuxedData[i].valueStepfunctionIndex]++;
                    valuesSlice[offset] = demuxedData[i].value;
                }

                // -------------
                // because we use offsets to as "iterators" in previous step, recalculate them again
                // TODO maybe extra space tradeoff would be better here

                timestampsCount = 0;
                for (var i = 0; i < timestampAxisIndexToLength.Length; ++i)
                {
                    timestampAxisIndexToOffset[i] = timestampsCount;
                    timestampsCount += timestampAxisIndexToLength[i];
                }

                valuesCount = 0;
                for (var i = 0; i < valueAxisIndexToOffset.Length; ++i)
                {
                    valueAxisIndexToOffset[i] = valuesCount;
                    valuesCount += timestampAxisIndexToLength[valueAxisIndexToTimestampIndex[i]];
                }

                // TODO opaque values
            }
        }

        public DatasetProxy ToDatasetProxy()
        {
            return new DatasetProxy
            {
                timestampsUnsafe = timestamps.ToUnsafeNativeSlice(),
                timestampAxisIndexToLengthUnsafe = timestampAxisIndexToLength.ToUnsafeNativeSlice(),
                timestampAxisIndexToMaxLengthUnsafe = timestampAxisIndexToMaxLength.ToUnsafeNativeSlice(),
                timestampAxisIndexToOffsetUnsafe = timestampAxisIndexToOffset.ToUnsafeNativeSlice(),
                timestampAxisIndexToPreviousRunValueUnsafe = timestampAxisIndexToPreviousRunValue.ToUnsafeNativeSlice(),

                valuesUnsafe = values.ToUnsafeNativeSlice(),
                valueAxisIndexToOffsetUnsafe = valueAxisIndexToOffset.ToUnsafeNativeSlice(),
                valueAxisIndexToTimestampIndexUnsafe = valueAxisIndexToTimestampIndex.ToUnsafeNativeSlice(),
                valueAxisIndexToPreviousRunValueUnsafe = valueAxisIndexToPreviousRunValue.ToUnsafeNativeSlice(),

                opaqueValuesUnsafe = opaqueValues.ToUnsafeNativeSlice(),
                opaqueValueAxisIndexToOffsetUnsafe = opaqueValueAxisIndexToOffset.ToUnsafeNativeSlice(),
                opaqueValueIndexToTimestampIndexUnsafe = opaqueValueIndexToTimestampIndex.ToUnsafeNativeSlice(),
            };
        }

        public void Dispose()
        {
            timestamps.Dispose();
            timestampAxisIndexToLength.Dispose();
            timestampAxisIndexToMaxLength.Dispose();
            timestampAxisIndexToOffset.Dispose();
            timestampAxisIndexToPreviousRunValue.Dispose();
            values.Dispose();
            valueAxisIndexToOffset.Dispose();
            valueAxisIndexToTimestampIndex.Dispose();
            valueAxisIndexToPreviousRunValue.Dispose();
            opaqueValues.Dispose();
            opaqueValueAxisIndexToOffset.Dispose();
            opaqueValueIndexToTimestampIndex.Dispose();
        }
    }
}