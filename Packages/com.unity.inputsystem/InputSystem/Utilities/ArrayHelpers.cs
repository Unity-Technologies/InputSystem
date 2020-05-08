using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// A collection of utility functions for working with arrays.
    /// </summary>
    /// <remarks>
    /// The goal of this collection is to make it easy to use arrays directly rather than resorting to
    /// <see cref="List{T}"/>.
    /// </remarks>
    internal static class ArrayHelpers
    {
        public static int LengthSafe<TValue>(this TValue[] array)
        {
            if (array == null)
                return 0;
            return array.Length;
        }

        public static void Clear<TValue>(this TValue[] array)
        {
            if (array == null)
                return;

            Array.Clear(array, 0, array.Length);
        }

        public static void Clear<TValue>(this TValue[] array, int count)
        {
            if (array == null)
                return;
            Array.Clear(array, 0, count);
        }

        public static void Clear<TValue>(this TValue[] array, ref int count)
        {
            if (array == null)
                return;

            Array.Clear(array, 0, count);
            count = 0;
        }

        public static void EnsureCapacity<TValue>(ref TValue[] array, int count, int capacity, int capacityIncrement = 10)
        {
            if (capacity == 0)
                return;

            if (array == null)
            {
                array = new TValue[Math.Max(capacity, capacityIncrement)];
                return;
            }

            var currentCapacity = array.Length - count;
            if (currentCapacity >= capacity)
                return;

            DuplicateWithCapacity(ref array, count, capacity, capacityIncrement);
        }

        public static void DuplicateWithCapacity<TValue>(ref TValue[] array, int count, int capacity, int capacityIncrement = 10)
        {
            if (array == null)
            {
                array = new TValue[Math.Max(capacity, capacityIncrement)];
                return;
            }

            var newSize = count + Math.Max(capacity, capacityIncrement);
            var newArray = new TValue[newSize];
            Array.Copy(array, newArray, count);
            array = newArray;
        }

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

        public static bool ContainsReference<TValue>(TValue[] array, TValue value)
            where TValue : class
        {
            if (array == null)
                return false;

            return ContainsReference(array, array.Length, value);
        }

        public static bool ContainsReference<TValue>(TValue[] array, int count, TValue value)
            where TValue : class
        {
            return IndexOfReference(array, value, count) != -1;
        }

        public static bool HaveEqualElements<TValue>(TValue[] first, TValue[] second, int count = int.MaxValue)
        {
            if (first == null || second == null)
                return second == first;

            var lengthFirst = Math.Min(count, first.Length);
            var lengthSecond = Math.Min(count, second.Length);

            if (lengthFirst != lengthSecond)
                return false;

            var comparer = EqualityComparer<TValue>.Default;
            for (var i = 0; i < lengthFirst; ++i)
                if (!comparer.Equals(first[i], second[i]))
                    return false;

            return true;
        }

        ////REVIEW: remove this to get rid of default equality comparer?
        public static int IndexOf<TValue>(TValue[] array, TValue value, int startIndex = 0, int count = -1)
        {
            if (array == null)
                return -1;

            if (count < 0)
                count = array.Length - startIndex;
            var comparer = EqualityComparer<TValue>.Default;
            for (var i = startIndex; i < startIndex + count; ++i)
                if (comparer.Equals(array[i], value))
                    return i;

            return -1;
        }

        public static int IndexOf<TValue>(this TValue[] array, Predicate<TValue> predicate)
        {
            if (array == null)
                return -1;

            var length = array.Length;
            for (var i = 0; i < length; ++i)
                if (predicate(array[i]))
                    return i;

            return -1;
        }

        public static int IndexOfReference<TValue>(this TValue[] array, TValue value, int count = -1)
            where TValue : class
        {
            return IndexOfReference(array, value, 0, count);
        }

        public static int IndexOfReference<TValue>(this TValue[] array, TValue value, int startIndex, int count)
            where TValue : class
        {
            if (array == null)
                return -1;

            if (count < 0)
                count = array.Length - startIndex;
            for (var i = startIndex; i < startIndex + count; ++i)
                if (ReferenceEquals(array[i], value))
                    return i;

            return -1;
        }

        public static int IndexOfValue<TValue>(this TValue[] array, TValue value, int startIndex = 0, int count = -1)
            where TValue : struct, IEquatable<TValue>
        {
            if (array == null)
                return -1;

            if (count < 0)
                count = array.Length - startIndex;
            for (var i = startIndex; i < startIndex + count; ++i)
                if (value.Equals(array[i]))
                    return i;

            return -1;
        }

        public static unsafe void Resize<TValue>(ref NativeArray<TValue> array, int newSize, Allocator allocator)
            where TValue : struct
        {
            var oldSize = array.Length;
            if (oldSize == newSize)
                return;

            if (newSize == 0)
            {
                if (array.IsCreated)
                    array.Dispose();
                array = new NativeArray<TValue>();
                return;
            }

            var newArray = new NativeArray<TValue>(newSize, allocator);
            if (oldSize != 0)
            {
                // Copy contents from old array.
                UnsafeUtility.MemCpy(newArray.GetUnsafePtr(), array.GetUnsafeReadOnlyPtr(),
                    UnsafeUtility.SizeOf<TValue>() * (newSize < oldSize ? newSize : oldSize));
                array.Dispose();
            }
            array = newArray;
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

        public static int AppendListWithCapacity<TValue, TValues>(ref TValue[] array, ref int length, TValues values, int capacityIncrement = 10)
            where TValues : IReadOnlyList<TValue>
        {
            var numToAdd = values.Count;
            if (array == null)
            {
                var size = Math.Max(numToAdd, capacityIncrement);
                array = new TValue[size];
                for (var i = 0; i < numToAdd; ++i)
                    array[i] = values[i];
                length += numToAdd;
                return 0;
            }

            var capacity = array.Length;
            if (capacity < length + numToAdd)
            {
                capacity += Math.Max(length + numToAdd, capacityIncrement);
                Array.Resize(ref array, capacity);
            }

            var index = length;
            for (var i = 0; i < numToAdd; ++i)
                array[index + i] = values[i];
            length += numToAdd;

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
                    throw new ArgumentOutOfRangeException(nameof(index));

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

        public static void InsertAtWithCapacity<TValue>(ref TValue[] array, ref int count, int index, TValue value, int capacityIncrement = 10)
        {
            EnsureCapacity(ref array, count, count + 1, capacityIncrement);

            if (index != count)
                Array.Copy(array, index, array, index + 1, count - index);

            array[index] = value;
            ++count;
        }

        public static void PutAtIfNotSet<TValue>(ref TValue[] array, int index, Func<TValue> valueFn)
        {
            if (array.LengthSafe() < index + 1)
                Array.Resize(ref array, index + 1);

            if (EqualityComparer<TValue>.Default.Equals(array[index], default(TValue)))
                array[index] = valueFn();
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

        public static int GrowWithCapacity<TValue>(ref TValue[] array, ref int count, int growBy, int capacityIncrement = 10)
        {
            var length = array != null ? array.Length : 0;
            if (length < count + growBy)
            {
                if (capacityIncrement < growBy)
                    capacityIncrement = growBy;
                GrowBy(ref array, capacityIncrement);
            }

            var offset = count;
            count += growBy;
            return offset;
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
                Array.Copy(values, 0, array, index, values.Length);

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

        public static void EraseAtWithCapacity<TValue>(TValue[] array, ref int count, int index)
        {
            Debug.Assert(array != null);
            Debug.Assert(count <= array.Length);
            Debug.Assert(index >= 0 && index < count);

            // If we're erasing from the beginning or somewhere in the middle, move
            // the array contents down from after the index.
            if (index < count - 1)
            {
                Array.Copy(array, index + 1, array, index, count - index - 1);
            }

            array[count - 1] = default; // Tail has been moved down by one.
            --count;
        }

        public static unsafe void EraseAtWithCapacity<TValue>(NativeArray<TValue> array, ref int count, int index)
            where TValue : struct
        {
            Debug.Assert(array.IsCreated);
            Debug.Assert(count <= array.Length);
            Debug.Assert(index >= 0 && index < count);

            // If we're erasing from the beginning or somewhere in the middle, move
            // the array contents down from after the index.
            if (index < count - 1)
            {
                var elementSize = UnsafeUtility.SizeOf<TValue>();
                var arrayPtr = (byte*)array.GetUnsafePtr();

                UnsafeUtility.MemCpy(arrayPtr + elementSize * index, arrayPtr + elementSize * (index + 1),
                    (count - index - 1) * elementSize);
            }

            --count;
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
                array[count - 1] = default;
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
            // Move elements down.
            if (count < length)
                Array.Copy(array, index + count, array, index, length - index - count);

            // Erase now vacant slots.
            for (var i = 0; i < count; ++i)
                array[length - i - 1] = default;

            length -= count;
        }

        public static void SwapElements<TValue>(this TValue[] array, int index1, int index2)
        {
            MemoryHelpers.Swap(ref array[index1], ref array[index2]);
        }

        public static void SwapElements<TValue>(this NativeArray<TValue> array, int index1, int index2)
            where TValue : struct
        {
            var temp = array[index1];
            array[index1] = array[index2];
            array[index2] = temp;
        }
    }
}
