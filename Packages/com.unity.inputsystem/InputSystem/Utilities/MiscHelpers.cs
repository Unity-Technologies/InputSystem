using System.Collections.Generic;

namespace UnityEngine.InputSystem.Utilities
{
    internal static class MiscHelpers
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, out var value) ? value : default;
        }

        public static IEnumerable<TValue> EveryNth<TValue>(this IEnumerable<TValue> enumerable, int n, int start = 0)
        {
            var index = 0;
            foreach (var element in enumerable)
            {
                if (index < start)
                {
                    ++index;
                    continue;
                }

                if ((index - start) % n == 0)
                    yield return element;
                ++index;
            }
        }
    }
}
