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
        
        /// <summary>
        /// Similar to IEnumerable interface FirstOrDefault() extension operation but allows providing an explicit
        /// fallback if sequence is empty. 
        /// </summary>
        /// <remarks>
        /// Note that this implies a performance hit compared to FirstOrDefault() since fallback needs
        /// to be constructed even if the source sequence is not empty. 
        /// </remarks>
        /// <param name="source">°The source sequence.</param>
        /// <param name="fallback">The fallback value.</param>
        /// <typeparam name="TSource">The source sequence element type.</typeparam>
        /// <returns>First element of sequence <c>source</c> or <c>fallback</c> if no such element exists.</returns>
        /// <exception cref="ArgumentNullException">If source is null.</exception>
        public static TSource FirstOrFallback<TSource>(this IEnumerable<TSource> source, TSource fallback = default(TSource))
        {
            switch (source)
            {
                case null:
                {
                    throw new ArgumentNullException(nameof(source));
                }
                case IList<TSource> sourceList:
                {
                    if (sourceList.Count > 0)
                        return sourceList[0];
                    break;
                }
                default:
                {
                    using var enumerator = source.GetEnumerator();
                    if (enumerator.MoveNext())
                        return enumerator.Current;
                    break;
                }
            }
            
            return fallback;
        }
    }
}

#endif
