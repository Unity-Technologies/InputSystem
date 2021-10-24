using System;
using System.ComponentModel;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Composites
{
    /// <summary>
    /// A binding with two additional modifiers modifier. The bound controls only trigger when
    /// both modifiers are pressed.
    /// </summary>
    /// <remarks>
    /// This composite can be used to require two buttons to be held in order to "activate"
    /// another binding.  This is most commonly used on keyboards to require two of the
    /// modifier keys (shift, ctrl, or alt) to be held in combination with another control,
    /// e.g. "SHIFT+CTRL+1".
    ///
    /// <example>
    /// <code>
    /// // Create a button action that triggers when SHIFT+CTRL+1
    /// // is pressed on the keyboard.
    /// var action = new InputAction(type: InputActionType.Button);
    /// action.AddCompositeBinding("TwoModifiers")
    ///     .With("Modifier", "&lt;Keyboard&gt;/ctrl")
    ///     .With("Modifier", "&lt;Keyboard&gt;/shift")
    ///     .With("Binding", "&lt;Keyboard&gt;/1")
    /// </code>
    /// </example>
    ///
    /// However, this can also be used to "gate" other types of controls. For example, a "look"
    /// action could be bound to mouse <see cref="Pointer.delta"/> such that the <see cref="Keyboard.altKey"/> and
    /// <see cref="Keyboard.shiftKey"/> on the keyboard have to be pressed in order for the player to be able to
    /// look around.
    ///
    /// <example>
    /// <code>
    /// lookAction.AddCompositeBinding("OneModifier")
    ///     .With("Modifier1", "&lt;Keyboard&gt;/alt")
    ///     .With("Modifier2", "&lt;Keyboard&gt;/shift")
    ///     .With("Binding", "&lt;Mouse&gt;/delta")
    /// </code>
    /// </example>
    ///
    /// <example>
    /// <code>
    /// // Create a button action that requires LMB on the mouse
    /// // to be held for the mouse delta to come through.
    /// var action = new InputAction(type: InputActionType.Button);
    /// action.AddCompositeBinding("OneModifier")
    ///     .With("Modifier", "&lt;Mouse&gt;/leftButton")
    ///     .With("Binding", "&lt;Mouse&gt;/delta");
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="OneModifierComposite"/>
    [DisplayStringFormat("{modifier1}+{modifier2}+{binding}")]
    [DisplayName("Binding With Two Modifiers")]
    public class TwoModifiersComposite : InputBindingComposite
    {
        /// <summary>
        /// Binding for the first button that acts as a modifier, e.g. <c>&lt;Keyboard/leftCtrl</c>.
        /// </summary>
        /// <value>Part index to use with <see cref="InputBindingCompositeContext.ReadValue{T}(int)"/>.</value>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once UnassignedField.Global
        [InputControl(layout = "Button")] public int modifier1;

        /// <summary>
        /// Binding for the second button that acts as a modifier, e.g. <c>&lt;Keyboard/leftCtrl</c>.
        /// </summary>
        /// <value>Part index to use with <see cref="InputBindingCompositeContext.ReadValue{T}(int)"/>.</value>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once UnassignedField.Global
        [InputControl(layout = "Button")] public int modifier2;

        /// <summary>
        /// Binding for the control that is gated by <see cref="modifier1"/> and <see cref="modifier2"/>.
        /// The composite will assume the value of this button while both of the modifiers are pressed.
        /// </summary>
        /// <value>Part index to use with <see cref="InputBindingCompositeContext.ReadValue{T}(int)"/>.</value>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once UnassignedField.Global
        [InputControl(layout = "Button")] public int binding;

        /// <summary>
        /// Type of values read from controls bound to <see cref="binding"/>.
        /// </summary>
        public override Type valueType => m_ValueType;

        /// <summary>
        /// Size of the largest value that may be read from the controls bound to <see cref="binding"/>.
        /// </summary>
        public override int valueSizeInBytes => m_ValueSizeInBytes;

        private int m_ValueSizeInBytes;
        private Type m_ValueType;

        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            if (context.ReadValueAsButton(modifier1) && context.ReadValueAsButton(modifier2))
                return context.EvaluateMagnitude(binding);
            return default;
        }

        /// <inheritdoc/>
        public override unsafe void ReadValue(ref InputBindingCompositeContext context, void* buffer, int bufferSize)
        {
            if (context.ReadValueAsButton(modifier1) && context.ReadValueAsButton(modifier2))
                context.ReadValue(binding, buffer, bufferSize);
            else
                UnsafeUtility.MemClear(buffer, m_ValueSizeInBytes);
        }

        /// <inheritdoc/>
        protected override void FinishSetup(ref InputBindingCompositeContext context)
        {
            OneModifierComposite.DetermineValueTypeAndSize(ref context, binding, out m_ValueType, out m_ValueSizeInBytes);
        }

        public override object ReadValueAsObject(ref InputBindingCompositeContext context)
        {
            if (context.ReadValueAsButton(modifier1) && context.ReadValueAsButton(modifier2))
                return context.ReadValueAsObject(binding);
            return null;
        }
    }
}
