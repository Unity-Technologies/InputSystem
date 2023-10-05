#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A strongly-typed wrapper for an Input Action.
    /// </summary>
    /// <typeparam name="TActionType">The type that will be used in calls to <see cref="InputAction.ReadValue{T}"/></typeparam>
    public class Input<TActionType> where TActionType : struct
    {
        /// <summary>
        /// Enables access to the underlying Input Action for advanced functionality such as rebinding.
        /// </summary>
        public InputAction action => m_Action;

        /// <see cref="InputAction.IsPressed"/>
        public bool isPressed => m_Action.IsPressed();
        /// <see cref="InputAction.WasPressedThisFrame"/>
        public bool wasPressedThisFrame => m_Action.WasPressedThisFrame();
        /// <see cref="InputAction.WasReleasedThisFrame"/>
        public bool wasReleasedThisFrame => m_Action.WasReleasedThisFrame();

        /// <summary>
        /// Returns the current value of the Input Action.
        /// </summary>
        public TActionType value
        {
            get
            {
                try
                {
                    // it should be unusual for ReadValue to throw because in most cases instances of this class
                    // will be created through the source generator, and that can catch mismatched control types
                    // at compile time and throw compiler errors, but it will always be possible to dynamically
                    // add incompatible bindings, and the best we can do then is to catch the exceptions
                    // thrown when we try to read from those controls.
                    return m_Action.ReadValue<TActionType>();
                }
                catch (InvalidOperationException ex)
                {
                    Debug.LogWarning(ex.Message);
                }

                return default(TActionType);
            }
        }

        public Input(InputAction action)
        {
            Debug.Assert(action != null);

            m_Action = action ?? throw new ArgumentNullException(nameof(action));
            m_Action.Enable();
        }

        private InputAction m_Action;
    }
}
#endif
