using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Input.Utilities
{
    // A collection of utility functions to work with arrays.
    internal static class ArrayHelpers
    {
        public static bool Contains<TValue>(TValue[] array, TValue value)
        {
            if (array == null)
                return false;

            var comparer = EqualityComparer<TValue>.Default;
            for (var i = 0; i < array.Length; ++i)
                if (comparer.Equals(array[i], value))
                    return true;

            return false;
        }

        public static bool HaveEqualElements<TValue>(TValue[] first, TValue[] second)
        {
            if (first == null || second == null)
                return second == first;

            var lengthFirst = first.Length;
            var lengthSecond = second.Length;

            if (lengthFirst != lengthSecond)
                return false;

            var comparer = EqualityComparer<TValue>.Default;
            for (var i = 0; i < lengthFirst; ++i)
                if (!comparer.Equals(first[i], second[i]))
                    return false;

            return true;
        }

        public static int IndexOf<TValue>(TValue[] array, TValue value)
        {
            if (array == null)
                return -1;

            var length = array.Length;
            var comparer = EqualityComparer<TValue>.Default;
            for (var i = 0; i < length; ++i)
                if (comparer.Equals(array[i], value))
                    return i;

            return -1;
        }

        public static int Append<TValue>(ref TValue[] array, TValue value)
        {
            if (array == null)
            {
                array = new TValue[1];
                array[0] = value;
                return 0;
            }

            var length = array.Length;
            Array.Resize(ref array, length + 1);
            array[length] = value;
            return length;
        }

        public static int Append<TValue>(ref TValue[] array, IEnumerable<TValue> values)
        {
            if (array == null)
            {
                array = values.ToArray();
                return 0;
            }

            var oldLength = array.Length;
            var valueCount = values.Count();

            Array.Resize(ref array, oldLength + valueCount);

            var index = oldLength;
            foreach (var value in values)
                array[index++] = value;

            return oldLength;
        }

        // Append to an array that is considered immutable. This allows using 'values' as is
        // if 'array' is null.
        // Returns the index of the first newly added element in the resulting array.
        public static int AppendToImmutable<TValue>(ref TValue[] array, TValue[] values)
        {
            if (array == null)
            {
                array = values;
                return 0;
            }

            if (values != null && values.Length > 0)
            {
                var oldCount = array.Length;
                var valueCount = values.Length;
                Array.Resize(ref array, oldCount + valueCount);
                Array.Copy(values, 0, array, oldCount, valueCount);
                return oldCount;
            }

            return array.Length;
        }

        public static int AppendWithCapacity<TValue>(ref TValue[] array, ref int count, TValue value, int capacityIncrement = 10)
        {
            if (array == null)
            {
                array = new TValue[capacityIncrement];
                array[0] = value;
                ++count;
                return 0;
            }

            var capacity = array.Length;
            if (capacity == count)
            {
                capacity += capacityIncrement;
                Array.Resize(ref array, capacity);
            }

            var index = count;
            array[index] = value;
            ++count;

            return index;
        }

        public static int AppendWithCapacity<TValue>(ref NativeArray<TValue> array, ref int count, TValue value,
            int capacityIncrement = 10, Allocator allocator = Allocator.Persistent)
            where TValue : struct
        {
            var capacity = array.Length;
            if (capacity == count)
                GrowBy(ref array, capacityIncrement > 1 ? capacityIncrement : 1, allocator);

            var index = count;
            array[index] = value;
            ++count;

            return index;
        }

        public static void InsertAt<TValue>(ref TValue[] array, int index, TValue value)
        {
            if (array == null)
            {
                ////REVIEW: allow growing array to specific size by inserting at arbitrary index?
                if (index != 0)
                    throw new IndexOutOfRangeException();

                array = new TValue[1];
                array[0] = value;
                return;
            }

            // Reallocate.
            var oldLength = array.Length;
            Array.Resize(ref array, oldLength + 1);

            // Make room for element.
            if (index != oldLength)
                Array.Copy(array, index, array, index + 1, oldLength - index);

            array[index] = value;
        }

        // Adds 'count' entries to the array. Returns first index of newly added entries.
        public static int GrowBy<TValue>(ref TValue[] array, int count)
        {
            if (array == null)
            {
                array = new TValue[count];
                return 0;
            }

            var oldLength = array.Length;
            Array.Resize(ref array, oldLength + count);
            return oldLength;
        }

        public static unsafe int GrowBy<TValue>(ref NativeArray<TValue> array, int count, Allocator allocator = Allocator.Persistent)
            where TValue : struct
        {
            var length = array.Length;
            if (length == 0)
            {
                array = new NativeArray<TValue>(count, allocator);
                return 0;
            }

            var newArray = new NativeArray<TValue>(length + count, allocator);
            // CopyFrom() expects length to match. Copy manually.
            UnsafeUtility.MemCpy(newArray.GetUnsafePtr(), array.GetUnsafeReadOnlyPtr(), (long)length * UnsafeUtility.SizeOf<TValue>());
            array.Dispose();
            array = newArray;

            return length;
        }

        public static int GrowWithCapacity<TValue>(ref NativeArray<TValue> array, ref int count, int growBy,
            int capacityIncrement = 10, Allocator allocator = Allocator.Persistent)
            where TValue : struct
        {
            var length = array.Length;
            if (length < count + growBy)
            {
                if (capacityIncrement < growBy)
                    capacityIncrement = growBy;
                GrowBy(ref array, capacityIncrement, allocator);
            }

            var offset = count;
            count += growBy;
            return offset;
        }

        public static TValue[] Join<TValue>(TValue value, params TValue[] values)
        {
            // Determine length.
            var length = 0;
            if (value != null)
                ++length;
            if (values != null)
                length += values.Length;

            if (length == 0)
                return null;

            var array = new TValue[length];

            // Populate.
            var index = 0;
            if (value != null)
                array[index++] = value;

            if (values != null)
                Array.Copy(values, 0, array, index, length);

            return array;
        }

        public static TValue[] Merge<TValue>(TValue[] first, TValue[] second)
            where TValue : IEquatable<TValue>
        {
            if (first == null)
                return second;
            if (second == null)
                return first;

            var merged = new List<TValue>();
            merged.AddRange(first);

            for (var i = 0; i < second.Length; ++i)
            {
                var secondValue = second[i];
                if (!merged.Exists(x => x.Equals(secondValue)))
                {
                    merged.Add(secondValue);
                }
            }

            return merged.ToArray();
        }

        public static TValue[] Merge<TValue>(TValue[] first, TValue[] second, IEqualityComparer<TValue> comparer)
        {
            if (first == null)
                return second;
            if (second == null)
                return null;

            var merged = new List<TValue>();
            merged.AddRange(first);

            for (var i = 0; i < second.Length; ++i)
            {
                var secondValue = second[i];
                if (!merged.Exists(x => comparer.Equals(secondValue)))
                {
                    merged.Add(secondValue);
                }
            }

            return merged.ToArray();
        }

        public static void EraseAt<TValue>(ref TValue[] array, int index)
        {
            Debug.Assert(array != null);
            Debug.Assert(index >= 0 && index < array.Length);

            var length = array.Length;
            if (index == 0 && length == 1)
            {
                array = null;
                return;
            }

            if (index < length - 1)
                Array.Copy(array, index + 1, array, index, length - index - 1);

            Array.Resize(ref array, length - 1);
        }

        public static bool Erase<TValue>(ref TValue[] array, TValue value)
        {
            var index = IndexOf(array, value);
            if (index != -1)
            {
                EraseAt(ref array, index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Erase an element from the array by moving the tail element into its place.
        /// </summary>
        /// <param name="array">Array to modify. May be not <c>null</c>.</param>
        /// <param name="count">Current number of elements inside of array. May be less than <c>array.Length</c>.</param>
        /// <param name="index">Index of element to remove. Tail element will get moved into its place.</param>
        /// <typeparam name="TValue"></typeparam>
        /// <remarks>
        /// This method does not re-allocate the array. Instead <paramref name="count"/> is used
        /// to keep track of how many elements there actually are in the array.
        /// </remarks>
        public static void EraseAtByMovingTail<TValue>(TValue[] array, ref int count, int index)
        {
            Debug.Assert(array != null);
            Debug.Assert(index >= 0 && index < array.Length);
            Debug.Assert(count >= 0 && count <= array.Length);
            Debug.Assert(index < count);

            // Move tail, if necessary.
            if (index != count - 1)
                array[index] = array[count - 1];

            // Destroy current tail.
            if (count >= 1)
                array[count - 1] = default(TValue);
            --count;
        }

        public static TValue[] Copy<TValue>(TValue[] array)
        {
            if (array == null)
                return null;

            var length = array.Length;
            var result = new TValue[length];
            Array.Copy(array, result, length);
            return result;
        }

        public static TValue[] Clone<TValue>(TValue[] array)
            where TValue : ICloneable
        {
            if (array == null)
                return null;

            var count = array.Length;
            var result = new TValue[count];

            for (var i = 0; i < count; ++i)
                result[i] = (TValue)array[i].Clone();

            return result;
        }

        public static TNew[] Select<TOld, TNew>(TOld[] array, Func<TOld, TNew> converter)
        {
            if (array == null)
                return null;

            var length = array.Length;
            var result = new TNew[length];

            for (var i = 0; i < length; ++i)
                result[i] = converter(array[i]);

            return result;
        }

        private static void Swap<TValue>(ref TValue first, ref TValue second)
        {
            var temp = first;
            first = second;
            second = temp;
        }

        /// <summary>
        /// Swap the contents of two potentially overlapping slices within the array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="destinationIndex"></param>
        /// <param name="count"></param>
        /// <typeparam name="TValue"></typeparam>
        public static void SwapSlice<TValue>(TValue[] array, int sourceIndex, int destinationIndex, int count)
        {
            if (sourceIndex < destinationIndex)
            {
                for (var i = 0; i < count; ++i)
                    Swap(ref array[sourceIndex + count - i - 1], ref array[destinationIndex + count - i - 1]);
            }
            else
            {
                for (var i = 0; i < count; ++i)
                    Swap(ref array[sourceIndex + i], ref array[destinationIndex + i]);
            }
        }

        /// <summary>
        /// Move a slice in the array to a different place without allocating a temporary array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="destinationIndex"></param>
        /// <param name="count"></param>
        /// <typeparam name="TValue"></typeparam>
        /// <remarks>
        /// The slice is moved by repeatedly swapping slices until all the slices are where they
        /// are supposed to go. This is not super efficient but avoids having to allocate a temporary
        /// array on the heap.
        /// </remarks>
        public static void MoveSlice<TValue>(TValue[] array, int sourceIndex, int destinationIndex, int count)
        {
            if (count <= 0 || sourceIndex == destinationIndex)
                return;

            // Make sure we're moving from lower part of array to higher part so we only
            // have to deal with that scenario.
            if (sourceIndex > destinationIndex)
                Swap(ref sourceIndex, ref destinationIndex);

            var length = array.Length;

            while (destinationIndex != sourceIndex)
            {
                // Swap source and destination slice. Afterwards, the source slice is the right, final
                // place but the destination slice may not be.
                SwapSlice(array, sourceIndex, destinationIndex, count);

                // Slide destination window down.
                if (destinationIndex - sourceIndex >= count * 2)
                {
                    // Slide down one whole window of count elements.
                    destinationIndex -= count;
                }
                else
                {
                    ////TODO: this can be improved by using halving instead and only doing the final step as a single element slide
                    // Slide down by one element.
                    --destinationIndex;
                }
            }
        }

        public static void EraseSliceWithCapacity<TValue>(ref TValue[] array, ref int length, int index, int count)
        {
            if (count < length)
            {
                Array.Copy(array, index + count, array, index, length - index - count);
                for (var i = 0; i < count; ++i)
                    array[length - i - 1] = default(TValue);
            }

            length -= count;
        }
    }
}
