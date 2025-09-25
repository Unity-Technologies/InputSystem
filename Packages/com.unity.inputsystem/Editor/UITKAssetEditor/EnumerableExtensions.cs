#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Editor
{
    internal static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
        {
            int index = 0;
            foreach (var item in enumerable)
                action(item, index++);
        }

        public static string Join<T>(this IEnumerable<T> enumerable, string separator)
        {
            return string.Join(separator, enumerable);
        }
    }
}

#endif
