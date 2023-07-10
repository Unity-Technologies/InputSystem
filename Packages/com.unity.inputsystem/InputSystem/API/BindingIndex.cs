using System;

namespace UnityEngine.InputSystem.HighLevel
{
    /// <summary>
    /// A type to encapsulate and simplify access to input bindings by index.
    /// </summary>
    /// <remarks>
    /// A binding index can be interpreted in multiple ways:
    ///  * An index into the action map bindings collection
    ///  * An index into the action bindings collection
    ///  * An index that includes composite binding parts
    ///  * An index the ignores composite binding parts
    /// This struct abstracts the indexing of bindings by being always specified relative to the bindings
    /// on the Input Action i.e. index 0 is the first binding on the action, even though in the action map
    /// it might have a different index, and by allowing the user to specify whether the index includes
    /// composite parts or not. An index that skips composite parts can always be converted to one that
    /// includes them, and vice versa, and the matching index on the action map can always be extracted.
    /// </remarks>
    public readonly struct BindingIndex
    {
        public static BindingIndex None = new BindingIndex(InputActionState.kInvalidIndex);

        private readonly InputAction m_InputAction;
        private readonly int m_Value;

        public int value => m_Value;

        public IndexType type { get; }

        public int ToMapIndex()
        {
            var bindingIndex = m_Value;
            if (type == IndexType.SkipCompositeParts)
                bindingIndex = ToIndexIncludingCompositeParts().m_Value;

            if (m_InputAction.GetOrCreateActionMap().bindingsAreContiguous)
                return m_InputAction.m_BindingsStartIndex + bindingIndex;

            // do a slower lookup by name and id
            return m_InputAction.BindingIndexOnActionToBindingIndexOnMap(bindingIndex);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="inputAction"></param>
        /// <param name="index"></param>
        /// <param name="type"></param>
        public BindingIndex(InputAction inputAction, int index, IndexType type)
        {
            Debug.Assert(inputAction != null);
            Debug.Assert(index >= 0);

            m_InputAction = inputAction;
            m_Value = index;
            this.type = type;
        }

        // Constructor exists so we can create the None BindingIndex
        private BindingIndex(int index)
        {
            m_InputAction = null;
            m_Value = index;
            type = IndexType.SkipCompositeParts;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public BindingIndex ToIndexIncludingCompositeParts()
        {
            if (type == IndexType.IncludeCompositeParts)
                return this;

            var bindings = m_InputAction.bindings;
            var indexSkippingCompositeParts = 0;
            var found = false;
            var index = 0;
            while (index < bindings.Count)
            {
                if (indexSkippingCompositeParts == m_Value)
                {
                    found = true;
                    break;
                }

                if (bindings[++index].isPartOfComposite == false)
                    ++indexSkippingCompositeParts;
            }

            if (!found)
                throw new ArgumentOutOfRangeException($"The index '{m_Value}' is out of range ");

            return new BindingIndex(m_InputAction, index, IndexType.IncludeCompositeParts);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        public BindingIndex ToIndexWithoutCompositeParts()
        {
            if (type == IndexType.SkipCompositeParts)
                return this;

            var bindings = m_InputAction.bindings;

            // first back up to find the parent composite if we're pointing at a composite part
            var normalOrCompositeIndex = m_Value;
            while (normalOrCompositeIndex > 0 && bindings[normalOrCompositeIndex].isPartOfComposite)
                --normalOrCompositeIndex;

            // then count forwards to find the matching normal or composite binding
            var bindingIndex = 0;
            var i = 0;
            while (bindingIndex != normalOrCompositeIndex && i < bindings.Count)
            {
                if (!bindings[i++].isPartOfComposite)
                    ++bindingIndex;
            }

            if (i == bindings.Count - 1 && bindings[i].isPartOfComposite)
                throw new InvalidOperationException($"Couldn't convert binding index '{m_Value}' that includes composite parts into " +
                    $"a binding index that ignores composite parts.");

            return new BindingIndex(m_InputAction, bindingIndex, IndexType.SkipCompositeParts);
        }

        /// <summary>
        /// Implicitly cast a BindingWithIndex type to a BindingIndex.
        /// </summary>
        /// <param name="bindingWithIndex"></param>
        public static implicit operator BindingIndex(BindingWithIndex bindingWithIndex)
        {
            return bindingWithIndex.index;
        }

        public bool Equals(BindingIndex other)
        {
            return Equals(m_InputAction, other.m_InputAction) && m_Value == other.m_Value && type == other.type;
        }

        public override bool Equals(object obj)
        {
            return obj is BindingIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (m_InputAction != null ? m_InputAction.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ m_Value;
                hashCode = (hashCode * 397) ^ (int)type;
                return hashCode;
            }
        }

        public static bool operator==(BindingIndex left, BindingIndex right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(BindingIndex left, BindingIndex right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// An enumeration to control how binding indexes are counted.
        /// </summary>
        public enum IndexType
        {
            /// <summary>
            /// When iterating an input binding collection, skip over composite part bindings.
            /// </summary>
            /// <remarks>
            /// Given the following set up:
            ///
            /// <example>
            /// <code>
            /// var action = new InputAction();
            /// action.AddBinding("&lt;keyboard&gt;/a");
            /// action.AddCompositeBinding("1DAxis")
            ///   .With("negative", "&lt;keyboard&gt;/w")
            ///   .With("positive", "&lt;keyboard&gt;/s");
            /// action.AddBinding("&lt;keyboard&gt;/b");
            /// </code>
            /// </example>
            ///
            /// indexing would look as follows:
            ///
            /// Binding         Index
            /// ---------------------
            /// keyboard/a      0
            /// 1DAxis          1
            ///  keyboard/w     -
            ///  keyboard/s     -
            /// keyboard/b      2
            ///
            /// </remarks>
            SkipCompositeParts,

            /// <summary>
            /// When iterating an input binding collection, count the composite part bindings.
            /// </summary>
            /// <remarks>
            /// Given the following set up:
            ///
            /// <example>
            /// <code>
            /// var action = new InputAction();
            /// action.AddBinding("&lt;keyboard&gt;/a");
            /// action.AddCompositeBinding("1DAxis")
            ///   .With("negative", "&lt;keyboard&gt;/w")
            ///   .With("positive", "&lt;keyboard&gt;/s");
            /// action.AddBinding("&lt;keyboard&gt;/b");
            /// </code>
            /// </example>
            ///
            /// indexing would look as follows:
            ///
            /// Binding         Index
            /// ---------------------
            /// keyboard/a      0
            /// 1DAxis          1
            ///  keyboard/w     2
            ///  keyboard/s     3
            /// keyboard/b      4
            ///
            /// </remarks>
            IncludeCompositeParts
        }
    }
}
