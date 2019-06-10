using System;

namespace UnityEngine.InputSystem
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
        public InputAction action => m_UseReference ? m_Reference.action : m_Action;
        public InputActionReference reference => m_UseReference ? m_Reference : null;

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

        public override bool Equals(object o)
        {
            if (m_UseReference)
                return Equals(o as InputActionReference);
            return Equals(o as InputAction);
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

        [SerializeField] private bool m_UseReference;
        [SerializeField] private InputAction m_Action;
        [SerializeField] private InputActionReference m_Reference;
    }
}
