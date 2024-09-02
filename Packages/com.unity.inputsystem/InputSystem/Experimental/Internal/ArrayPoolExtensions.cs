using System;
using System.Buffers;

namespace UnityEngine.InputSystem.Experimental.Internal
{
    public static class ArrayPoolExtensions
    {
        public static void Append<T>(this ArrayPool<T> pool, ref T[] array, T item, ref int count)
        {
            if (array == null)
            {
                Debug.Assert(count == 0);
                array = ArrayPool<T>.Shared.Rent(1);
            }
            else if (count == array.Length)
            {
                var old = array;
                array = ArrayPool<T>.Shared.Rent( count + 1);
                Array.Copy(old, 0, array, 0, count);
                ArrayPool<T>.Shared.Return(old);
            }
            
            array[count++] = item;
        }
        
        public static bool Remove<T>(this ArrayPool<T> pool, ref T[] array, T item, ref int count)
        {
            var index = Array.IndexOf(array, item);
            if (index < 0)
                return false;
            
            Array.Copy(array, index + 1, array, index, count - index - 1);
            if (--count == 0)
            {
                ArrayPool<T>.Shared.Return(array);
                array = null;
                count = 0;
            }
            return true;
        }
    }
}