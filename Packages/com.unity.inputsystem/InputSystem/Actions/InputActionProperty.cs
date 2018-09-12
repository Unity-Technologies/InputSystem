using System;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A serializable property type that can either reference an action externally defined
    /// in an <see cref="InputActionAsset"/> or define a new action directly on the property.
    /// </summary>
    [Serializable]
    public struct InputActionProperty
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

        public void Set(InputActionAsset asset, InputAction action)
        {
            m_UseReference = true;
            m_Action = null;
            m_Reference.Set(asset, action);
        }

        public void Set(InputAction action)
        {
            m_UseReference = false;
            m_Action = action;
            m_Reference = null;
        }

        public void Enable()
        {
            var resolvedAction = action;
            if (resolvedAction != null)
                resolvedAction.Enable();
        }

        [SerializeField] private bool m_UseReference;
        [SerializeField] private InputAction m_Action;
        [SerializeField] private InputActionReference m_Reference;
    }
}
