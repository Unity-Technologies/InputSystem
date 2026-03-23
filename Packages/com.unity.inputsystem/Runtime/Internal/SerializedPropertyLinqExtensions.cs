#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal static class SerializedPropertyLinqExtensions
    {
        public static IEnumerable<T> Select<T>(this SerializedProperty property, Func<SerializedProperty, T> selector)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            if (property.isArray == false)
                yield break;

            for (var i = 0; i < property.arraySize; i++)
            {
                yield return selector(property.GetArrayElementAtIndex(i));
            }
        }

        public static IEnumerable<SerializedProperty> Where(this SerializedProperty property,
            Func<SerializedProperty, bool> predicate)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            if (property.isArray == false)
                yield break;

            for (var i = 0; i < property.arraySize; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                if (predicate(element))
                    yield return element;
            }
        }

        public static SerializedProperty FindLast(this SerializedProperty property, Func<SerializedProperty, bool> predicate)
        {
            Debug.Assert(predicate != null, "Missing predicate for FindLast function.");
            Debug.Assert(property != null, "SerializedProperty missing for FindLast function.");

            if (property.isArray == false)
                return null;

            for (int i = property.arraySize - 1; i >= 0; i--)
            {
                var element = property.GetArrayElementAtIndex(i);
                if (predicate(element))
                    return element;
            }
            return null;
        }

        public static SerializedProperty FirstOrDefault(this SerializedProperty property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (property.isArray == false || property.arraySize == 0)
                return null;

            return property.GetArrayElementAtIndex(0);
        }

        public static SerializedProperty FirstOrDefault(this SerializedProperty property,
            Func<SerializedProperty, bool> predicate)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            if (property.isArray == false)
                return null;

            for (var i = 0; i < property.arraySize; i++)
            {
                var arrayElementAtIndex = property.GetArrayElementAtIndex(i);
                if (predicate(arrayElementAtIndex) == false)
                    continue;

                return arrayElementAtIndex;
            }

            return null;
        }

        public static IEnumerable<SerializedProperty> Skip(this SerializedProperty property, int count)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            return SkipIterator(property, count);
        }

        public static IEnumerable<SerializedProperty> Take(this SerializedProperty property, int count)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (count < 0 || count > property.arraySize)
                throw new ArgumentOutOfRangeException(nameof(count));

            return TakeIterator(property, count);
        }

        private static IEnumerable<SerializedProperty> SkipIterator(SerializedProperty source, int count)
        {
            var enumerator = source.GetEnumerator();
            while (count > 0 && enumerator.MoveNext()) count--;
            if (count <= 0)
            {
                while (enumerator.MoveNext())
                    yield return (SerializedProperty)enumerator.Current;
            }
        }

        private static IEnumerable<SerializedProperty> TakeIterator(SerializedProperty source, int count)
        {
            if (count > 0)
            {
                foreach (SerializedProperty element in source)
                {
                    yield return element;
                    if (--count == 0) break;
                }
            }
        }
    }
}

#endif
