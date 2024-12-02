using System;
using System.Diagnostics;
using UnityEngine.InputSystem.Controls;

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
        /// This method allocates GC memory and will thus create garbage. If used during gameplay,
        /// it will lead to GC spikes.
        /// </remarks>
        /// <returns>The current value in the form of a boxed object.</returns>
        public object Get()
        {
            return m_Context.Value.ReadValueAsObject();
        }

        ////TODO: add automatic conversions
        /// <summary>
        /// Read the value of the action.
        /// </summary>
        /// <returns>The current value from the action cast to the specified type.</returns>
        /// <typeparam name="TValue">Type of value to read. This must correspond to the
        /// expected by either <see cref="control"/> or, if it is a composite, by the
        /// <see cref="InputBindingComposite"/> in use.
        /// Common types are float and Vector2, and depend on the type of the associated action</typeparam>
        /// <exception cref="InvalidOperationException">The given type <typeparamref name="TValue"/>
        /// does not match the value type expected by the control or binding composite.</exception>
        /// <remarks>
        /// The following example shows how to read a value from a <see cref="PlayerInput"/> message.
        /// 
        /// <example>
        /// <code>
        /// [RequireComponent(typeof(PlayerInput))]
        /// public class MyPlayerLogic : MonoBehaviour
        /// {
        ///     private Vector2 m_Move;
        ///
        ///     // 'Move' input action has been triggered.
        ///     public void OnMove(InputValue value)
        ///     {
        ///         // Read value from control. The type depends on what type of controls the action is bound to.
        ///         m_Move = value.Get&lt;Vector2&gt;();
        ///     }
        /// 
        ///     public void OnUpdate()
        ///     {
        ///         // Update transform from m_Move
        ///     }
        /// }
        /// </code>
        /// </example>
        /// The given InputValue is only valid for the duration of the callback. Storing the InputValue references somewhere and calling Get&lt;T&gt;() later does not work correctly.
        /// </remarks>
        /// <seealso cref="CallbackContext.ReadValue{TValue}"/>
        public TValue Get<TValue>()
            where TValue : struct
        {
            if (!m_Context.HasValue)
                throw new InvalidOperationException($"Values can only be retrieved while in message callbacks");

            return m_Context.Value.ReadValue<TValue>();
        }

        ////TODO: proper message if value type isn't right
        /// <summary>
        /// Check if the action button is pressed
        /// </summary>
        /// <returns>True if the button is activated over the button threshold. False otherwise</returns>
        /// <remarks>
        /// The following example shows how to read a value from a <see cref="PlayerInput"/> message.
        /// 
        /// <example>
        /// <code>
        /// [RequireComponent(typeof(PlayerInput))]
        /// public class MyPlayerLogic : MonoBehaviour
        /// {
        ///     private bool m_Fire;
        ///
        ///     // 'Fire' input action has been triggered.
        ///     public void OnFire(InputValue value)
        ///     {
        ///         m_Fire = value.isPressed;
        ///     }
        /// 
        ///     public void OnUpdate()
        ///     {
        ///         // Perform fire action if m_Fire is true
        ///     }
        /// }
        /// </code>
        /// </example>
        /// The given InputValue is only valid for the duration of the callback. Storing the InputValue references somewhere and calling Get&lt;T&gt;() later does not work correctly.
        /// </remarks>
        /// <seealso cref="ButtonControl.pressPointOrDefault"/>
        public bool isPressed => Get<float>() >= ButtonControl.s_GlobalDefaultButtonPressPoint;

        internal InputAction.CallbackContext? m_Context;
    }
}
