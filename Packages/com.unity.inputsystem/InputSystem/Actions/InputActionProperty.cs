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
        /// The effective action held on to by the property.
        /// </summary>
        /// <value>The effective action object contained in the property.</value>
        /// <remarks>
        /// This property will return <c>null</c> if the property is using a <see cref="reference"/> and
        /// the referenced action cannot be found. Also, it will be <c>null</c> if the property
        /// has been manually initialized with a <c>null</c> <see cref="InputAction"/> using
        /// <see cref="InputActionProperty(InputAction)"/>.
        /// </remarks>
        public InputAction action => m_UseReference ? m_Reference != null ? m_Reference.action : null : m_Action;

        /// <summary>
        /// If the property is set to use a reference to the action, this property returns
        /// the reference. Otherwise it returns <c>null</c>.
        /// </summary>
        /// <value>Reference to external input action, if defined.</value>
        public InputActionReference reference => m_UseReference ? m_Reference : null;

        /// <summary>
        /// The serialized loose action created in code serialized with this property.
        /// </summary>
        /// <value>The serialized action field.</value>
        internal InputAction serializedAction => m_Action;

        /// <summary>
        /// The serialized reference to an external action.
        /// </summary>
        /// <value>The serialized reference field.</value>
        internal InputActionReference serializedReference => m_Reference;

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

        /// <summary>
        /// Compare two action properties to see whether they refer to the same action.
        /// </summary>
        /// <param name="other">Another action property.</param>
        /// <returns>True if both properties refer to the same action.</returns>
        public bool Equals(InputActionProperty other)
        {
            return m_Reference == other.m_Reference &&
                m_UseReference == other.m_UseReference &&
                m_Action == other.m_Action;
        }

        /// <summary>
        /// Check whether the property refers to the same action.
        /// </summary>
        /// <param name="other">An action.</param>
        /// <returns>True if <see cref="action"/> is the same as <paramref name="other"/>.</returns>
        public bool Equals(InputAction other)
        {
            return ReferenceEquals(action, other);
        }

        /// <summary>
        /// Check whether the property references the same action.
        /// </summary>
        /// <param name="other">An action reference.</param>
        /// <returns>True if the property and <paramref name="other"/> reference the same action.</returns>
        public bool Equals(InputActionReference other)
        {
            return m_Reference == other;
        }

        /// <summary>
        /// Check whether the given object is an InputActionProperty referencing the same action.
        /// </summary>
        /// <param name="obj">An object or <c>null</c>.</param>
        /// <returns>True if the given <paramref name="obj"/> is an InputActionProperty equivalent to this one.</returns>
        /// <seealso cref="Equals(InputActionProperty)"/>
        public override bool Equals(object obj)
        {
            if (m_UseReference)
                return Equals(obj as InputActionReference);
            return Equals(obj as InputAction);
        }

        /// <summary>
        /// Compute a hash code for the object.
        /// </summary>
        /// <returns>A hash code.</returns>
        public override int GetHashCode()
        {
            if (m_UseReference)
                return m_Reference != null ? m_Reference.GetHashCode() : 0;
            return m_Action != null ? m_Action.GetHashCode() : 0;
        }

        /// <summary>
        /// Compare the two properties for equivalence.
        /// </summary>
        /// <param name="left">The first property.</param>
        /// <param name="right">The second property.</param>
        /// <returns>True if the two action properties are equivalent.</returns>
        /// <seealso cref="Equals(InputActionProperty)"/>
        public static bool operator==(InputActionProperty left, InputActionProperty right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compare the two properties for not being equivalent.
        /// </summary>
        /// <param name="left">The first property.</param>
        /// <param name="right">The second property.</param>
        /// <returns>True if the two action properties are not equivalent.</returns>
        /// <seealso cref="Equals(InputActionProperty)"/>
        public static bool operator!=(InputActionProperty left, InputActionProperty right)
        {
            return !left.Equals(right);
        }

        [SerializeField] private bool m_UseReference;
        [SerializeField] private InputAction m_Action;
        [SerializeField] private InputActionReference m_Reference;
    }
}
