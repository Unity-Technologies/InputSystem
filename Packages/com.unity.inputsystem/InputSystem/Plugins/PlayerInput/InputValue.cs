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
        /// Read the current value as an object.
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
        /// Read the current value of the action.
        /// </summary>
        /// <returns>The current value from the action cast to the specified type.</returns>
        /// <typeparam name="TValue">Type of value to read. This must correspond to the
        /// <see cref="InputControl.valueType"/> of the action or, if it is a composite, by the
        /// <see cref="InputBindingComposite.valueType"/>.
        /// The type depends on what type of controls the action is bound to.
        /// Common types are <c>float</c> and <see cref="UnityEngine.Vector2"/></typeparam>
        /// <exception cref="InvalidOperationException">The given type <typeparamref name="TValue"/>
        /// does not match the value type expected by the control or binding composite.</exception>
        /// <remarks>
        /// The following example shows how to read a value from a <see cref="PlayerInput"/> message.
        /// The given <c>InputValue</c> is only valid for the duration of the callback. Storing the <c>InputValue</c> references somewhere and calling Get&lt;T&gt;() later does not work correctly.
        /// </remarks>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.InputSystem;
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
        ///     public void Update()
        ///     {
        ///         // Update transform from m_Move
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="InputAction.CallbackContext.ReadValue{TValue}"/>
        public TValue Get<TValue>()
            where TValue : struct
        {
            if (!m_Context.HasValue)
                throw new InvalidOperationException($"Values can only be retrieved while in message callbacks");

            return m_Context.Value.ReadValue<TValue>();
        }

        ////TODO: proper message if value type isn't right
        /// <summary>
        /// Check if the action button is pressed.
        /// </summary>
        /// <remarks>
        /// True if the button is activated over the button threshold. False otherwise
        /// The following example check if a button is pressed when receiving a <see cref="PlayerInput"/> message.
        /// The given <c>InputValue</c> is only valid for the duration of the callback. Storing the <c>InputValue</c> references somewhere and calling Get&lt;T&gt;() later does not work correctly.
        /// </remarks>
        /// <example>
        /// <code>
        /// [RequireComponent(typeof(PlayerInput))]
        /// public class MyPlayerLogic : MonoBehaviour
        /// {
        ///     // 'Fire' input action has been triggered.
        ///     public void OnFire(InputValue value)
        ///     {
        ///         if (value.isPressed)
        ///             FireWeapon();
        ///     }
        ///
        ///     public void FireWeapon()
        ///     {
        ///         // Weapon firing code
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="ButtonControl.pressPointOrDefault"/>
        public bool isPressed => Get<float>() >= ButtonControl.s_GlobalDefaultButtonPressPoint;

        internal InputAction.CallbackContext? m_Context;
    }
}
