using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.InputSystem.DataPipeline.Collections;
using UnityEngine.InputSystem.DataPipeline.Demux;

namespace UnityEngine.InputSystem.DataPipeline
{
    public struct Dataset : IDisposable
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
        
                // Sets destination length to be equal to source length, and returns the length.
        // Destination should be pointing to the same timestamp index as the source.
        // Because when you map one Y axis to another Y axis, the values in X axis should stay the same.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int MapNToN<T1, T2>(T1 src, T2 dst) where T1 : IStepFunction where T2 : IStepFunction
        {
            SanityCheck(src);
            SanityCheck(dst);

            var srcTimestampAxisIndex = GetTimestampIndex(src);
            var dstTimestampAxisIndex = GetTimestampIndex(dst);

            var length = timestampAxisIndexToLength[srcTimestampAxisIndex];
            var maxLenght = timestampAxisIndexToMaxLength[srcTimestampAxisIndex];

            Debug.Assert(length <= maxLenght);
            Debug.Assert(srcTimestampAxisIndex == dstTimestampAxisIndex);

            return length;
        }
        
        // Sets destination length to be equal to max(source length, minLength), and returns the length.
        // Destination should be pointing to the different timestamp index from the source.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int MapNToMaxNAndX<T1, T2>(T1 src, T2 dst, int minLength) where T1 : IStepFunction where T2 : IStepFunction
        {
            SanityCheck(src);
            SanityCheck(dst);

            var srcTimestampAxisIndex = GetTimestampIndex(src);
            var dstTimestampAxisIndex = GetTimestampIndex(dst);

            var length = timestampAxisIndexToLength[srcTimestampAxisIndex];
            length = length < minLength ? minLength : length;

            var maxLength = timestampAxisIndexToMaxLength[srcTimestampAxisIndex];

            timestampAxisIndexToLength[dstTimestampAxisIndex] = length;
            timestampAxisIndexToMaxLength[dstTimestampAxisIndex] = length; // TODO

            //Debug.Assert(length <= maxLength); // TODO
            Debug.Assert(srcTimestampAxisIndex != dstTimestampAxisIndex);
            
            return length;
        }

        // Sets destination length to be equal to sum of source lengths, and returns the length.
        // Destination should be pointing to different timestamp index.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int lenth1, int length2) MapNAndMToNPlusM<T1, T2, T3>(T1 src1, T2 src2, T3 dst) where T1 : IStepFunction
            where T2 : IStepFunction
            where T3 : IStepFunction
        {
            SanityCheck(src1);
            SanityCheck(src2);
            SanityCheck(dst);

            var src1TimestampAxisIndex = GetTimestampIndex(src1);
            var src2TimestampAxisIndex = GetTimestampIndex(src2);
            var dstTimestampAxisIndex = GetTimestampIndex(dst);

            var length1 = timestampAxisIndexToLength[src1TimestampAxisIndex];
            var length2 = timestampAxisIndexToLength[src2TimestampAxisIndex];
            var maxLenght1 = timestampAxisIndexToMaxLength[src1TimestampAxisIndex];
            var maxLenght2 = timestampAxisIndexToMaxLength[src2TimestampAxisIndex];
            var maxLenght3 = timestampAxisIndexToMaxLength[dstTimestampAxisIndex];
            timestampAxisIndexToLength[dstTimestampAxisIndex] = length1 + length2;
            timestampAxisIndexToMaxLength[dstTimestampAxisIndex] = maxLenght1 + maxLenght2;

            // Debug.Assert((length1 + length2) <= maxLenght3);
            // Debug.Assert((maxLenght1 + maxLenght2) <= maxLenght3);
            Debug.Assert(src1TimestampAxisIndex != dstTimestampAxisIndex);
            Debug.Assert(src2TimestampAxisIndex != dstTimestampAxisIndex);

            return (length1, length2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ShrinkSizeTo<T>(T stepfunction, int newLength) where T : IStepFunction
        {
            SanityCheck(stepfunction);

            var timestampAxisIndex = GetTimestampIndex(stepfunction);
            
            Debug.Assert(newLength <= timestampAxisIndexToLength[timestampAxisIndex]);
            Debug.Assert(newLength <= timestampAxisIndexToMaxLength[timestampAxisIndex]);

            timestampAxisIndexToLength[timestampAxisIndex] = newLength;
            timestampAxisIndexToMaxLength[timestampAxisIndex] = newLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<ulong> GetTimestamps<T>(T stepfunction)
            where T : IStepFunction
        {
            SanityCheck(stepfunction);

            var timestampAxisIndex = GetTimestampIndex(stepfunction);

            var offset = timestampAxisIndexToOffset[timestampAxisIndex];
            var length = timestampAxisIndexToLength[timestampAxisIndex];

            return timestamps.ToNativeSlice(offset, length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetPreviousTimestamp<T>(T stepfunction)
            where T : IStepFunction
        {
            SanityCheck(stepfunction);

            var timestampAxisIndex = GetTimestampIndex(stepfunction);
            return timestampAxisIndexToPreviousRunValue[timestampAxisIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<float> GetValuesX<T>(T stepfunction)
            where T : IStepFunction
        {
            Debug.Assert(stepfunction.dimensionsCount >= 1);

            var timestampAxisIndex = valueAxisIndexToTimestampIndex[stepfunction.valuesXProperty];

            var offset = valueAxisIndexToOffset[stepfunction.valuesXProperty];
            var length = timestampAxisIndexToLength[timestampAxisIndex];

            return values.ToNativeSlice(offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<float> GetValuesY<T>(T stepfunction)
            where T : IStepFunction
        {
            Debug.Assert(stepfunction.dimensionsCount >= 2);

            var timestampAxisIndex = valueAxisIndexToTimestampIndex[stepfunction.valuesYProperty];

            var offset = valueAxisIndexToOffset[stepfunction.valuesYProperty];
            var length = timestampAxisIndexToLength[timestampAxisIndex];

            return values.ToNativeSlice(offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<float> GetValuesZ<T>(T stepfunction)
            where T : IStepFunction
        {
            Debug.Assert(stepfunction.dimensionsCount >= 3);

            var timestampAxisIndex = valueAxisIndexToTimestampIndex[stepfunction.valuesZProperty];

            var offset = valueAxisIndexToOffset[stepfunction.valuesZProperty];
            var length = timestampAxisIndexToLength[timestampAxisIndex];

            return values.ToNativeSlice(offset, length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetPreviousValueX<T>(T stepfunction)
            where T : IStepFunction
        {
            Debug.Assert(stepfunction.dimensionsCount >= 1);
            return valueAxisIndexToPreviousRunValue[stepfunction.valuesXProperty];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetPreviousValueY<T>(T stepfunction)
            where T : IStepFunction
        {
            Debug.Assert(stepfunction.dimensionsCount >= 2);
            return valueAxisIndexToPreviousRunValue[stepfunction.valuesYProperty];
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetPreviousValueZ<T>(T stepfunction)
            where T : IStepFunction
        {
            Debug.Assert(stepfunction.dimensionsCount >= 3);
            return valueAxisIndexToPreviousRunValue[stepfunction.valuesZProperty];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<TResult> GetValuesOpaque<TResult, TStepFunction>(TStepFunction stepfunction)
            where TStepFunction : IStepFunction where TResult : struct
        {
            Debug.Assert(stepfunction.dimensionsCount == 0);

            var timestampAxisIndex = opaqueValueIndexToTimestampIndex[stepfunction.opaqueValuesProperty];
            Debug.Assert(timestampAxisIndexToLength[timestampAxisIndex] >= 1);

            var offset = opaqueValueAxisIndexToOffset[stepfunction.opaqueValuesProperty];
            var stride = stepfunction.opaqueValuesStrideProperty;
            var length = timestampAxisIndexToLength[timestampAxisIndex];

            // TODO check me!
            return values.ToNativeSlice(offset, length).SliceConvert<TResult>();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult GetPreviousValueOpaque<TResult, TStepFunction>(TStepFunction stepfunction)
            where TStepFunction : IStepFunction where TResult : struct
        {
            // TODO implement me!
            return default(TResult);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<int> GetValuesOpaque(StepFunctionInt stepfunction)
        {
            return GetValuesOpaque<int, StepFunctionInt>(stepfunction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<Quaternion> GetValuesOpaque(StepFunctionQuaternion stepfunction)
        {
            return GetValuesOpaque<Quaternion, StepFunctionQuaternion>(stepfunction);
        }

        private int GetTimestampIndex<T>(T stepfunction) where T : IStepFunction
        {
            return stepfunction.dimensionsCount > 0
                ? valueAxisIndexToTimestampIndex[stepfunction.valuesXProperty]
                : opaqueValueAxisIndexToOffset[stepfunction.opaqueValuesProperty];
        }

        private void SanityCheck<T>(T stepfunction) where T : IStepFunction
        {
            // in multidimensional step functions all value axes should point to the same timestamp axis
            if (stepfunction.dimensionsCount <= 0)
                return;

            var timestampsAxisX = stepfunction.dimensionsCount >= 1
                ? valueAxisIndexToTimestampIndex[stepfunction.valuesXProperty]
                : -1;
            var timestampsAxisY = stepfunction.dimensionsCount >= 2
                ? valueAxisIndexToTimestampIndex[stepfunction.valuesYProperty]
                : -1;
            var timestampsAxisZ = stepfunction.dimensionsCount >= 3
                ? valueAxisIndexToTimestampIndex[stepfunction.valuesZProperty]
                : -1;

            Debug.Assert(stepfunction.dimensionsCount < 2 ||
                         timestampsAxisX == timestampsAxisY);
            Debug.Assert(stepfunction.dimensionsCount < 3 ||
                         timestampsAxisX == timestampsAxisZ);
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