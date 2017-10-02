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
        public static void Append<TValue>(ref TValue[] array, TValue value)
        {
            if (array == null)
            {
                array = new TValue[1];
                array[0] = value;
            }
            else
            {
                var length = array.Length;
                Array.Resize(ref array, length + 1);
                array[length] = value;
            }
        }
    }
}