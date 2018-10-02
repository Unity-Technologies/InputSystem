using System;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A serializable property type that can either reference an action externally defined
    /// in an <see cref="InputActionAsset"/> or define a new action directly on the property.
    /// </summary>
    [Serializable]
    public struct InputActionProperty : IEquatable<InputActionProperty>, IEquatable<InputAction>, IEquatable<InputActionReference>
    {
        /// <summary>
        /// The action held on to by the property.
        /// </summary>
        public InputAction action
        {
            get
            {
                if (m_UseReference)
                    return m_Reference.action;
                return m_Action;
            }
        }

        public InputActionProperty(InputAction action)
        {
            m_UseReference = false;
            m_Action = action;
            m_Reference = null;
        }

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

        [SerializeField] private bool m_UseReference;
        [SerializeField] private InputAction m_Action;
        [SerializeField] private InputActionReference m_Reference;
    }
}
