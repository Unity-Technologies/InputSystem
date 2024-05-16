#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Provides a <see cref="AsReadOnly{TKey, TValue, TReadOnlyValue}(IDictionary{TKey, TValue})"/>
    /// method on any generic dictionary that allows up-casting the value type.
    /// </summary>
    /// <example>
    /// var map = new Dictionary&lt;string, List&lt;int&gt;;
    /// return map.AsReadOnly&lt;string, List&lt;int&gt;, IReadOnlyList&lt;int&gt;&gt;();
    /// </example>
    internal static class DictionaryExtension
    {
        sealed class ReadOnlyDictionaryWrapper<TKey, TValue, TReadOnlyValue> : IReadOnlyDictionary<TKey, TReadOnlyValue>
            where TValue : TReadOnlyValue
            where TKey : notnull
        {
            private readonly IDictionary<TKey, TValue> m_Dictionary;

            public ReadOnlyDictionaryWrapper(IDictionary<TKey, TValue> dictionary)
            {
                if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
                m_Dictionary = dictionary;
            }

            public bool ContainsKey(TKey key) => m_Dictionary.ContainsKey(key);

            public IEnumerable<TKey> Keys => m_Dictionary.Keys;

            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TReadOnlyValue value)
            {
                var r = m_Dictionary.TryGetValue(key, out var v);
                value = v !;
                return r;
            }

            public IEnumerable<TReadOnlyValue> Values => m_Dictionary.Values.Cast<TReadOnlyValue>();

            public TReadOnlyValue this[TKey key] => m_Dictionary[key];

            public int Count => m_Dictionary.Count;

            public IEnumerator<KeyValuePair<TKey, TReadOnlyValue>> GetEnumerator() => m_Dictionary.Select(x => new KeyValuePair<TKey, TReadOnlyValue>(x.Key, x.Value)).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Creates a wrapper on a dictionary that adapts the type of the values.
        /// </summary>
        /// <typeparam name="TKey">The dictionary key.</typeparam>
        /// <typeparam name="TValue">The dictionary value.</typeparam>
        /// <typeparam name="TReadOnlyValue">The base type of the <typeparamref name="TValue"/>.</typeparam>
        /// <param name="this">This dictionary.</param>
        /// <returns>A dictionary where values are a base type of this dictionary.</returns>
        public static IReadOnlyDictionary<TKey, TReadOnlyValue> AsReadOnly<TKey, TValue, TReadOnlyValue>(this IDictionary<TKey, TValue> @this)
            where TValue : TReadOnlyValue
            where TKey : notnull
        {
            return new ReadOnlyDictionaryWrapper<TKey, TValue, TReadOnlyValue>(@this);
        }
    }
}
#endif // UNITY_EDITOR
