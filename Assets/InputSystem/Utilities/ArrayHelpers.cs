using System;
using System.Collections.Generic;

namespace ISX
{
    public static class ArrayHelpers
    {
        public static bool Contains<TValue>(TValue[] array, TValue value)
        {
            if (array == null)
                return false;

            for (var i = 0; i < array.Length; ++i)
                if (EqualityComparer<TValue>.Default.Equals(array[i], value))
                    return true;

            return false;
        }

        public static int Append<TValue>(ref TValue[] array, TValue value)
        {
            if (array == null)
            {
                array = new TValue[1];
                array[0] = value;
                return 0;
            }
            else
            {
                var length = array.Length;
                Array.Resize(ref array, length + 1);
                array[length] = value;
                return length;
            }
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
    }
}
