using System;

namespace ISX
{
    public static class ArrayHelpers
    {
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
                Array.Resize(ref array, length);
                array[length] = value;
            }
        }
    }
}