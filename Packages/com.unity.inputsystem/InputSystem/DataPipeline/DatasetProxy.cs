using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine.InputSystem.DataPipeline.Collections;

namespace UnityEngine.InputSystem.DataPipeline
{
    // Can't use Dataset because we pass it around as an argument, so it has to be a non-managed type.
    public struct DatasetProxy
    {
        // TODO values before current ones?

        public UnsafeNativeSlice<ulong> timestampsUnsafe;
        public UnsafeNativeSlice<int> timestampAxisIndexToLengthUnsafe;
        public UnsafeNativeSlice<int> timestampAxisIndexToMaxLengthUnsafe;
        public UnsafeNativeSlice<int> timestampAxisIndexToOffsetUnsafe;
        public UnsafeNativeSlice<ulong> timestampAxisIndexToPreviousRunValueUnsafe;

        public UnsafeNativeSlice<float> valuesUnsafe;
        public UnsafeNativeSlice<int> valueAxisIndexToOffsetUnsafe;
        public UnsafeNativeSlice<int> valueAxisIndexToTimestampIndexUnsafe;
        public UnsafeNativeSlice<float> valueAxisIndexToPreviousRunValueUnsafe;

        // Opaque offsets are in bytes.
        public UnsafeNativeSlice<byte> opaqueValuesUnsafe;
        public UnsafeNativeSlice<int> opaqueValueAxisIndexToOffsetUnsafe;
        public UnsafeNativeSlice<int> opaqueValueIndexToTimestampIndexUnsafe;

        // Sets destination length to be equal to source length, and returns the length.
        // Destination should be pointing to the same timestamp index as the source.
        // Because when you map one Y axis to another Y axis, the values in X axis should stay the same.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int MapNToN<T1, T2>(T1 src, T2 dst) where T1 : IStepFunction where T2 : IStepFunction
        {
            SanityCheck(src);
            SanityCheck(dst);

            var timestampAxisIndexToLength = timestampAxisIndexToLengthUnsafe.ToNativeSlice();
            var timestampAxisIndexToMaxLength = timestampAxisIndexToMaxLengthUnsafe.ToNativeSlice();

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

            var timestampAxisIndexToLength = timestampAxisIndexToLengthUnsafe.ToNativeSlice();
            var timestampAxisIndexToMaxLength = timestampAxisIndexToMaxLengthUnsafe.ToNativeSlice();

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

            var timestampAxisIndexToLength = timestampAxisIndexToLengthUnsafe.ToNativeSlice();
            var timestampAxisIndexToMaxLength = timestampAxisIndexToMaxLengthUnsafe.ToNativeSlice();

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

            var timestampAxisIndexToLength = timestampAxisIndexToLengthUnsafe.ToNativeSlice();
            var timestampAxisIndexToMaxLength = timestampAxisIndexToMaxLengthUnsafe.ToNativeSlice();

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

            var timestampAxisIndexToLength = timestampAxisIndexToLengthUnsafe.ToNativeSlice();
            var timestampAxisIndexToOffset = timestampAxisIndexToOffsetUnsafe.ToNativeSlice();

            var offset = timestampAxisIndexToOffset[timestampAxisIndex];
            var length = timestampAxisIndexToLength[timestampAxisIndex];

            return timestampsUnsafe.ToNativeSlice(offset, length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetPreviousTimestamp<T>(T stepfunction)
            where T : IStepFunction
        {
            SanityCheck(stepfunction);

            var timestampAxisIndex = GetTimestampIndex(stepfunction);
            return timestampAxisIndexToPreviousRunValueUnsafe.ToNativeSlice()[timestampAxisIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<float> GetValuesX<T>(T stepfunction)
            where T : IStepFunction
        {
            Debug.Assert(stepfunction.dimensionsCount >= 1);

            var valueAxisIndexToTimestampIndex = valueAxisIndexToTimestampIndexUnsafe.ToNativeSlice();
            var timestampAxisIndexToLength = timestampAxisIndexToLengthUnsafe.ToNativeSlice();
            var valueAxisIndexToOffset = valueAxisIndexToOffsetUnsafe.ToNativeSlice();

            var timestampAxisIndex = valueAxisIndexToTimestampIndex[stepfunction.valuesXProperty];

            var offset = valueAxisIndexToOffset[stepfunction.valuesXProperty];
            var length = timestampAxisIndexToLength[timestampAxisIndex];

            return valuesUnsafe.ToNativeSlice(offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<float> GetValuesY<T>(T stepfunction)
            where T : IStepFunction
        {
            Debug.Assert(stepfunction.dimensionsCount >= 2);

            var valueAxisIndexToTimestampIndex = valueAxisIndexToTimestampIndexUnsafe.ToNativeSlice();
            var timestampAxisIndexToLength = timestampAxisIndexToLengthUnsafe.ToNativeSlice();
            var valueAxisIndexToOffset = valueAxisIndexToOffsetUnsafe.ToNativeSlice();

            var timestampAxisIndex = valueAxisIndexToTimestampIndex[stepfunction.valuesYProperty];

            var offset = valueAxisIndexToOffset[stepfunction.valuesYProperty];
            var length = timestampAxisIndexToLength[timestampAxisIndex];

            return valuesUnsafe.ToNativeSlice(offset, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<float> GetValuesZ<T>(T stepfunction)
            where T : IStepFunction
        {
            Debug.Assert(stepfunction.dimensionsCount >= 3);

            var valueAxisIndexToTimestampIndex = valueAxisIndexToTimestampIndexUnsafe.ToNativeSlice();
            var timestampAxisIndexToLength = timestampAxisIndexToLengthUnsafe.ToNativeSlice();
            var valueAxisIndexToOffset = valueAxisIndexToOffsetUnsafe.ToNativeSlice();

            var timestampAxisIndex = valueAxisIndexToTimestampIndex[stepfunction.valuesZProperty];

            var offset = valueAxisIndexToOffset[stepfunction.valuesZProperty];
            var length = timestampAxisIndexToLength[timestampAxisIndex];

            return valuesUnsafe.ToNativeSlice(offset, length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetPreviousValueX<T>(T stepfunction)
            where T : IStepFunction
        {
            Debug.Assert(stepfunction.dimensionsCount >= 1);
            return valueAxisIndexToPreviousRunValueUnsafe.ToNativeSlice()[stepfunction.valuesXProperty];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetPreviousValueY<T>(T stepfunction)
            where T : IStepFunction
        {
            Debug.Assert(stepfunction.dimensionsCount >= 2);
            return valueAxisIndexToPreviousRunValueUnsafe.ToNativeSlice()[stepfunction.valuesYProperty];
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetPreviousValueZ<T>(T stepfunction)
            where T : IStepFunction
        {
            Debug.Assert(stepfunction.dimensionsCount >= 3);
            return valueAxisIndexToPreviousRunValueUnsafe.ToNativeSlice()[stepfunction.valuesZProperty];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSlice<TResult> GetValuesOpaque<TResult, TStepFunction>(TStepFunction stepfunction)
            where TStepFunction : IStepFunction where TResult : struct
        {
            Debug.Assert(stepfunction.dimensionsCount == 0);

            var opaqueValueAxisIndexToOffset = opaqueValueAxisIndexToOffsetUnsafe.ToNativeSlice();
            var opaqueValueIndexToTimestampIndex = opaqueValueIndexToTimestampIndexUnsafe.ToNativeSlice();
            var timestampAxisIndexToLength = timestampAxisIndexToLengthUnsafe.ToNativeSlice();

            var timestampAxisIndex = opaqueValueIndexToTimestampIndex[stepfunction.opaqueValuesProperty];
            Debug.Assert(timestampAxisIndexToLength[timestampAxisIndex] >= 1);

            var offset = opaqueValueAxisIndexToOffset[stepfunction.opaqueValuesProperty];
            var stride = stepfunction.opaqueValuesStrideProperty;
            var length = timestampAxisIndexToLength[timestampAxisIndex];

            return valuesUnsafe.ToNativeSlice<TResult>(offset, stride, length);
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
                ? valueAxisIndexToTimestampIndexUnsafe.ToNativeSlice()[stepfunction.valuesXProperty]
                : opaqueValueAxisIndexToOffsetUnsafe.ToNativeSlice()[stepfunction.opaqueValuesProperty];
        }

        private void SanityCheck<T>(T stepfunction) where T : IStepFunction
        {
            // in multidimensional step functions all value axes should point to the same timestamp axis
            if (stepfunction.dimensionsCount <= 0)
                return;

            var valueAxisIndexToTimestampIndex = valueAxisIndexToTimestampIndexUnsafe.ToNativeSlice();
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
    };
}