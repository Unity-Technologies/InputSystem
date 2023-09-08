#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A collection that can intelligently iterate over the bindings of an input action.
    /// </summary>
    /// <remarks>
    /// Input bindings are stored such that normal bindings, composite bindings, and composite part bindings
    /// e.g. the 'negative' and 'positive' bindings of a 1D axis composite, or the 'up', 'down', 'left', 'right'
    /// bindings of a 2D vector composite, are all stored together in a single contiguous array. This collection
    /// enables easy iteration over, or direct indexing into, the bindings of a specified Input Action without
    /// having to manually keep track of whether a binding is a composite part or a normal binding.
    ///
    /// The iterator and indexer of this type return BindingWithIndex instances
    ///
    /// Note that an Input Actions' bindings can change due to binding re-resolution, which can be caused by
    /// something as simple as adding a new binding, or a device connecting or disconnecting, so it is not
    /// advised to store instances of this type as the underlying data can change.
    /// </remarks>
    public readonly struct InputBindingsCollection : IEnumerable<BindingWithIndex>
    {
        private readonly InputAction m_InputAction;
        private readonly ReadOnlyArray<InputBinding> m_Bindings;
        private readonly EnumerationBehaviour m_EnumerationBehaviour;

        public InputBindingsCollection(InputAction inputAction, EnumerationBehaviour enumerationBehaviour)
        {
            Debug.Assert(inputAction != null);

            m_InputAction = inputAction;
            m_Bindings = m_InputAction.bindings;
            m_EnumerationBehaviour = enumerationBehaviour;
        }

        /// <summary>
        /// An indexer into the bindings collection that respects the enumeration behaviour.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public BindingWithIndex this[int index]
        {
            get
            {
                if (index < 0 || index > m_Bindings.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (m_EnumerationBehaviour == EnumerationBehaviour.SkipCompositeParts)
                {
                    var currentIndex = 0;
                    var i = 0;
                    while (i < m_Bindings.Count)
                    {
                        if (currentIndex == index)
                            return new BindingWithIndex(m_Bindings[currentIndex],
                                new BindingIndex(m_InputAction, currentIndex,
                                    BindingIndex.IndexType.SkipCompositeParts));

                        if (m_Bindings[i].isPartOfComposite == false)
                            currentIndex++;

                        i++;
                    }
                }
                else
                {
                    return new BindingWithIndex(m_Bindings[index],
                        new BindingIndex(m_InputAction, index, BindingIndex.IndexType.IncludeCompositeParts));
                }

                throw new InvalidOperationException($"Binding at index '{index}' not found.");
            }
        }

        /// <inheritdoc/>
        public IEnumerator<BindingWithIndex> GetEnumerator()
        {
            return new Enumerator(m_InputAction, m_EnumerationBehaviour);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private struct Enumerator : IEnumerator<BindingWithIndex>
        {
            private readonly EnumerationBehaviour m_EnumerationBehaviour;
            private readonly ReadOnlyArray<InputBinding> m_Bindings;
            private readonly InputAction m_InputAction;
            private int m_Current;
            private int m_CurrentWithCompositeParts;
            private int m_InitialBindingsCount;

            public Enumerator(InputAction inputAction, EnumerationBehaviour enumerationBehaviour)
            {
                Debug.Assert(inputAction != null);

                m_InputAction = inputAction;
                m_Bindings = inputAction.bindings;
                m_EnumerationBehaviour = enumerationBehaviour;
                m_Current = -1;
                m_CurrentWithCompositeParts = -1;
                m_InitialBindingsCount = m_Bindings.Count;
            }

            public bool MoveNext()
            {
                // poor mans enumerator versioning. Note that we have to access
                // the bindings array from the Input Action here so that it forces it to be recreated if any bindings have
                // been added or deleted.
                if (m_InitialBindingsCount != m_InputAction.bindings.Count)
                    throw new InvalidOperationException("The collection was modified after the enumerator was created.");

                if (m_CurrentWithCompositeParts >= m_Bindings.Count)
                    return false;

                if (m_EnumerationBehaviour == EnumerationBehaviour.SkipCompositeParts)
                {
                    // ++m_Current;
                    // var i = m_Current;
                    while (++m_CurrentWithCompositeParts < m_Bindings.Count)
                    {
                        if (m_Bindings[m_CurrentWithCompositeParts].isPartOfComposite)
                            continue;

                        ++m_Current;
                        return true;
                    }

                    // if (m_CurrentWithCompositeParts == m_Bindings.Count && m_Bindings[m_CurrentWithCompositeParts - 1].isPartOfComposite)
                    return false;
                }

                ++m_CurrentWithCompositeParts;
                return m_CurrentWithCompositeParts != m_Bindings.Count;
            }

            public void Reset()
            {
                m_Current = -1;
            }

            public BindingWithIndex Current =>
                new BindingWithIndex(
                    m_Bindings[m_CurrentWithCompositeParts],
                    new BindingIndex(m_InputAction,
                        m_EnumerationBehaviour == EnumerationBehaviour.IncludeCompositeParts
                        ? m_CurrentWithCompositeParts
                        : m_Current,
                        m_EnumerationBehaviour == EnumerationBehaviour.IncludeCompositeParts
                        ? BindingIndex.IndexType.IncludeCompositeParts
                        : BindingIndex.IndexType.SkipCompositeParts));

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        public enum EnumerationBehaviour
        {
            SkipCompositeParts,
            IncludeCompositeParts
        }
    }

    /// <summary>
    /// Contains both a BindingIndex and the InputBinding it refers to.
    /// </summary>
    public struct BindingWithIndex
    {
        public BindingWithIndex(InputBinding binding, BindingIndex index)
        {
            this.binding = binding;
            this.index = index;
        }

        public InputBinding binding { get; }
        public BindingIndex index { get; }
    }

    public static class InputBindingsCollectionExtensions
    {
        public static IEnumerable<BindingWithIndex> WithControlScheme(
            this InputBindingsCollection collection, string controlScheme)
        {
            return collection.Where(b =>
                !string.IsNullOrEmpty(b.binding.groups) && b.binding.groups.Contains(controlScheme));
        }
    }
}
#endif
