using System;
using System.Diagnostics;

////TODO: API to get the control and device from the internal context

////TODO: ToString()

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Wraps around values provided by input actions.
    /// </summary>
    /// <remarks>
    /// This is a wrapper around <see cref="InputAction.CallbackContext"/> chiefly for use
    /// with GameObject messages (i.e. <see cref="GameObject.SendMessage(string,object)"/>). It exists
    /// so that action callback data can be represented as an object, can be reused, and shields
    /// the receiver from having to know about action callback specifics.
    /// </remarks>
    /// <seealso cref="InputAction"/>
    [DebuggerDisplay("Value = {Get()}")]
    public class InputValue
    {
        /// <summary>
        /// Read the value as an object.
        /// </summary>
        /// <remarks>
        /// This method allocates GC memory and will thus created garbage. If used during gameplay,
        /// it will lead to GC spikes.
        /// </remarks>
        /// <returns>The current value in the form of a boxed object.</returns>
        public object Get()
        {
            return m_Context.Value.ReadValueAsObject();
        }

        ////TODO: add automatic conversions
        public TValue Get<TValue>()
            where TValue : struct
        {
            if (!m_Context.HasValue)
                throw new InvalidOperationException($"Values can only be retrieved while in message callbacks");

            return m_Context.Value.ReadValue<TValue>();
        }

        ////TODO: proper message if value type isn't right
        public bool isPressed => Get<float>() >= InputSystem.settings.defaultButtonPressPoint;

        internal InputAction.CallbackContext? m_Context;
    }
}
