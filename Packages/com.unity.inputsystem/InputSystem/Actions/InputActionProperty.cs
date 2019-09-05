using System;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A serializable property type that can either reference an action externally defined
    /// in an <see cref="InputActionAsset"/> or define a new action directly on the property.
    /// </summary>
    /// <remarks>
    /// This struct is meant to be used for serialized fields in <c>MonoBehaviour</c> and
    /// <c>ScriptableObject</c> classes. It has a custom property drawer attached to it
    /// that allows to switch between using the property as a reference and using it
    /// to define an action in place.
    ///
    /// <example>
    /// <code>
    /// public class MyBehavior : MonoBehaviour
    /// {
    ///     // This can be edited in the inspector to either reference an existing
    ///     // action or to define an action directly on the component.
    ///     public InputActionProperty myAction;
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="InputAction"/>
    /// <seealso cref="InputActionReference"/>
    [Serializable]
    public struct InputActionProperty : IEquatable<InputActionProperty>, IEquatable<InputAction>, IEquatable<InputActionReference>
    {
        /// <summary>
        /// The action held on to by the property.
        /// </summary>
        /// <value>The action object contained in the property.</value>
        /// <remarks>
        /// This property will return <c>null</c> if the property using a <see cref="reference"/> and
        /// the referenced action cannot be found. Also, it will be <c>null</c> if the property
        /// has been manually initialized with a <c>null</c> <see cref="InputAction"/> using
        /// <see cref="InputActionProperty(InputAction)"/>.
        /// </remarks>
        public InputAction action => m_UseReference ? m_Reference.action : m_Action;

        /// <summary>
        /// If the property contains a reference to the action, this property returns
        /// the reference. Otherwise it returns <c>null</c>.
        /// </summary>
        /// <value>Reference to external input action, if defined.</value>
        public InputActionReference reference => m_UseReference ? m_Reference : null;

        /// <summary>
        /// Initialize the property to contain the given action.
        /// </summary>
        /// <param name="action">An action.</param>
        /// <remarks>
        /// When the struct is serialized, it will serialize the given action as part of it.
        /// The <see cref="reference"/> property will return <c>null</c>.
        /// </remarks>
        public InputActionProperty(InputAction action)
        {
            m_UseReference = false;
            m_Action = action;
            m_Reference = null;
        }

        /// <summary>
        /// Initialize the property to use the given action reference.
        /// </summary>
        /// <param name="reference">Reference to an <see cref="InputAction"/>.</param>
        /// <remarks>
        /// When the struct is serialized, it will only serialize a reference to
        /// the given <paramref name="reference"/> object.
        /// </remarks>
        public InputActionProperty(InputActionReference reference)
        {
            m_UseReference = true;
            m_Action = null;
            m_Reference = reference;
        }

        public bool Equals(InputActionProperty other)
        {
            return m_Reference == other.m_Reference &&
                m_UseReference == other.m_UseReference &&
                m_Action == other.m_Action;
        }

        public bool Equals(InputAction other)
        {
            return ReferenceEquals(action, other);
        }

        public bool Equals(InputActionReference other)
        {
            return m_Reference == other;
        }

        public override bool Equals(object obj)
        {
            if (m_UseReference)
                return Equals(obj as InputActionReference);
            return Equals(obj as InputAction);
        }

        public override int GetHashCode()
        {
            if (m_UseReference)
                return m_Reference.GetHashCode();
            return m_Action.GetHashCode();
        }

        public static bool operator==(InputActionProperty left, InputActionProperty right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(InputActionProperty left, InputActionProperty right)
        {
            return !left.Equals(right);
        }

        public static bool operator==(InputActionProperty left, InputAction right)
        {
            return ReferenceEquals(left.action, right);
        }

        public static bool operator!=(InputActionProperty left, InputAction right)
        {
            return !ReferenceEquals(left.action, right);
        }

        public static bool operator==(InputAction left, InputActionProperty right)
        {
            return ReferenceEquals(left, right.action);
        }

        public static bool operator!=(InputAction left, InputActionProperty right)
        {
            return !ReferenceEquals(left, right.action);
        }

        public static implicit operator InputActionProperty(InputAction action)
        {
            return new InputActionProperty(action);
        }

        public static InputActionProperty ToInputActionProperty(InputAction action)
        {
            return new InputActionProperty(action);
        }

        [SerializeField] private bool m_UseReference;
        [SerializeField] private InputAction m_Action;
        [SerializeField] private InputActionReference m_Reference;
    }
}
