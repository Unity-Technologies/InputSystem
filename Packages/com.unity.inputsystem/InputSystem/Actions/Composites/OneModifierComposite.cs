using System;
using System.ComponentModel;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;

////TODO: allow making modifier optional; maybe alter the value (e.g. 0=unpressed, 0.5=pressed without modifier, 1=pressed with modifier)

namespace UnityEngine.InputSystem.Composites
{
    /// <summary>
    /// A binding with an additional modifier. The bound controls only trigger when
    /// the modifier is pressed.
    /// </summary>
    /// <remarks>
    /// This composite can be used to require a button to be held in order to "activate"
    /// another binding.  This is most commonly used on keyboards to require one of the
    /// modifier keys (shift, ctrl, or alt) to be held in combination with another control,
    /// e.g. "CTRL+1".
    ///
    /// <example>
    /// <code>
    /// // Create a button action that triggers when CTRL+1
    /// // is pressed on the keyboard.
    /// var action = new InputAction(type: InputActionType.Button);
    /// action.AddCompositeBinding("OneModifier")
    ///     .With("Modifier", "&lt;Keyboard&gt;/ctrl")
    ///     .With("Binding", "&lt;Keyboard&gt;/1")
    /// </code>
    /// </example>
    ///
    /// However, this can also be used to "gate" other types of controls. For example, a "look"
    /// action could be bound to mouse <see cref="Pointer.delta"/> such that the <see cref="Keyboard.altKey"/> on the
    /// keyboard has to be pressed in order for the player to be able to look around.
    ///
    /// <example>
    /// <code>
    /// lookAction.AddCompositeBinding("OneModifier")
    ///     .With("Modifier", "&lt;Keyboard&gt;/alt")
    ///     .With("Binding", "&lt;Mouse&gt;/delta")
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="TwoModifiersComposite"/>
    [DisplayStringFormat("{modifier}+{binding}")]
    [DisplayName("Binding With One Modifier")]
    public class OneModifierComposite : InputBindingComposite
    {
        /// <summary>
        /// Binding for the button that acts as a modifier, e.g. <c>&lt;Keyboard/ctrl</c>.
        /// </summary>
        /// <value>Part index to use with <see cref="InputBindingCompositeContext.ReadValue{T}(int)"/>.</value>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once UnassignedField.Global
        [InputControl(layout = "Button")] public int modifier;

        /// <summary>
        /// Binding for the control that is gated by the modifier. The composite will assume the value
        /// of this control while the modifier is considered pressed (that is, has a magnitude equal to or
        /// greater than the button press point).
        /// </summary>
        /// <value>Part index to use with <see cref="InputBindingCompositeContext.ReadValue{T}(int)"/>.</value>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once UnassignedField.Global
        [InputControl] public int binding;

        /// <summary>
        /// Type of values read from controls bound to <see cref="binding"/>.
        /// </summary>
        public override Type valueType => m_ValueType;

        /// <summary>
        /// Size of the largest value that may be read from the controls bound to <see cref="binding"/>.
        /// </summary>
        public override int valueSizeInBytes => m_ValueSizeInBytes;

        /// <summary>
        /// If set to <c>true</c>, the built-in logic to determine if modifiers need to be pressed first is overridden.
        /// Default value is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// By default, if <see cref="binding"/> is bound to only <see cref="Controls.ButtonControl"/>s, then the composite requires
        /// <see cref="modifier"/> to be pressed <em>before</em> pressing <see cref="binding"/>. This means that binding to, for example,
        /// <c>Ctrl+B</c>, the <c>ctrl</c> keys have to be pressed before pressing the <c>B</c> key. This is the behavior usually expected
        /// with keyboard shortcuts.
        ///
        /// However, when binding, for example, <c>Ctrl+MouseDelta</c>, it should be possible to press <c>ctrl</c> at any time. The default
        /// logic will automatically detect the difference between this binding and the button binding in the example above and behave
        /// accordingly.
        ///
        /// This field allows you to explicitly override this default inference and make it so that regardless of what <see cref="binding"/>
        /// is bound to, any press sequence is acceptable. For the example binding to <c>Ctrl+B</c>, it would mean that pressing <c>B</c> and
        /// only then pressing <c>Ctrl</c> will still trigger the binding.
        /// </remarks>
        public bool overrideModifiersNeedToBePressedFirst;

        private int m_ValueSizeInBytes;
        private Type m_ValueType;
        private bool m_BindingIsButton;

        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            if (ModifierIsPressed(ref context))
                return context.EvaluateMagnitude(binding);
            return default;
        }

        /// <inheritdoc/>
        public override unsafe void ReadValue(ref InputBindingCompositeContext context, void* buffer, int bufferSize)
        {
            if (ModifierIsPressed(ref context))
                context.ReadValue(binding, buffer, bufferSize);
            else
                UnsafeUtility.MemClear(buffer, m_ValueSizeInBytes);
        }

        private bool ModifierIsPressed(ref InputBindingCompositeContext context)
        {
            var modifierDown = context.ReadValueAsButton(modifier);

            // When the modifiers are gating a button, we require the modifiers to be pressed *first*.
            if (modifierDown && m_BindingIsButton && !overrideModifiersNeedToBePressedFirst)
            {
                var timestamp = context.GetPressTime(binding);
                var timestamp1 = context.GetPressTime(modifier);

                return timestamp1 <= timestamp;
            }

            return modifierDown;
        }

        /// <inheritdoc/>
        protected override void FinishSetup(ref InputBindingCompositeContext context)
        {
            DetermineValueTypeAndSize(ref context, binding, out m_ValueType, out m_ValueSizeInBytes, out m_BindingIsButton);

            if (!overrideModifiersNeedToBePressedFirst)
                overrideModifiersNeedToBePressedFirst = !InputSystem.settings.shortcutKeysConsumeInput;
        }

        public override object ReadValueAsObject(ref InputBindingCompositeContext context)
        {
            if (context.ReadValueAsButton(modifier))
                return context.ReadValueAsObject(binding);
            return null;
        }

        internal static void DetermineValueTypeAndSize(ref InputBindingCompositeContext context, int part, out Type valueType, out int valueSizeInBytes, out bool isButton)
        {
            valueSizeInBytes = 0;
            isButton = true;

            Type type = null;
            foreach (var control in context.controls)
            {
                if (control.part != part)
                    continue;

                var controlType = control.control.valueType;
                if (type == null || controlType.IsAssignableFrom(type))
                    type = controlType;
                else if (!type.IsAssignableFrom(controlType))
                    type = typeof(Object);

                valueSizeInBytes = Math.Max(control.control.valueSizeInBytes, valueSizeInBytes);

                // *All* bound controls need to be buttons for us to classify this part as a "Button" part.
                isButton &= control.control.isButton;
            }

            valueType = type;
        }
    }
}
