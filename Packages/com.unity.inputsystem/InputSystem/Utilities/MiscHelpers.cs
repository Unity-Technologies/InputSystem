using System.Collections.Generic;
using UnityEditor;

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

        public static int IndexOf<TValue>(this IEnumerable<TValue> enumerable, TValue value)
        {
            var index = 0;
            foreach (var element in enumerable)
                if (EqualityComparer<TValue>.Default.Equals(element, value))
                    return index;
                else
                    ++index;
            return -1;
        }

        // We have this function to hide away instanceId -> entityId migration that happened in Unity 6.4
        public static bool HasNativeObject(Object obj)
        {
#if UNITY_6000_4_OR_NEWER
            return EditorUtility.EntityIdToObject(obj.GetEntityId()) != null;
#else
            return EditorUtility.InstanceIDToObject(obj.GetInstanceID()) != null;
#endif
        }
    }
}
