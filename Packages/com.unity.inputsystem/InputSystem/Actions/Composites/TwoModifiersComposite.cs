using System;
using System.ComponentModel;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

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
    /// action could be bound to mouse <see cref="Pointer.delta"/> such that the <see cref="Keyboard.ctrlKey"/> and
    /// <see cref="Keyboard.shiftKey"/> on the keyboard have to be pressed in order for the player to be able to
    /// look around.
    ///
    /// <example>
    /// <code>
    /// var action = new InputAction();
    /// action.AddCompositeBinding("TwoModifiers")
    ///     .With("Modifier1", "&lt;Keyboard&gt;/ctrl")
    ///     .With("Modifier2", "&lt;Keyboard&gt;/shift")
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
        [InputControl] public int binding;

        /// <summary>
        /// If set to <c>true</c>, the built-in logic to determine if modifiers need to be pressed first is overridden.
        /// Default value is <c>false</c>.
        /// </summary>
        /// <remarks>
        /// By default, if the setting <see cref="InputSettings.shortcutKeysConsumeInput"/> is enabled,
        /// if <see cref="binding"/> is bound to only <see cref="Controls.ButtonControl"/>s, then the composite requires
        /// both <see cref="modifier1"/> and <see cref="modifier2"/> to be pressed <em>before</em> pressing <see cref="binding"/>.
        /// This means that binding to, for example, <c>Ctrl+Shift+B</c>, the <c>ctrl</c> and <c>shift</c> keys have to be pressed, in any order,
        /// before pressing the <c>B</c> key. This is the behavior usually expected with keyboard shortcuts.
        ///
        /// However, when binding, for example, <c>Ctrl+Shift+MouseDelta</c>, it should be possible to press <c>ctrl</c> and <c>shift</c>
        /// at any time and in any order. The default logic will automatically detect the difference between this binding and the button
        /// binding in the example above and behave accordingly.
        ///
        /// This field allows you to explicitly override this default inference and make it so that regardless of what <see cref="binding"/>
        /// is bound to, any press sequence is acceptable. For the example binding to <c>Ctrl+Shift+B</c>, it would mean that pressing
        /// <c>B</c> and only then pressing <c>Ctrl</c> and <c>Shift</c> will still trigger the binding.
        ///
        /// To don't depends on the setting please consider using <see cref="modifiersOrder"/> instead.
        /// </remarks>
        [Tooltip("Obsolete please use modifiers Order. If enabled, this will override the Input Consumption setting, allowing the modifier keys to be pressed after the button and the composite will still trigger.")]
        [Obsolete("Use ModifiersOrder.Unordered with 'modifiersOrder' instead")]
        public bool overrideModifiersNeedToBePressedFirst;

        /// <summary>
        /// Determines how a <c>modifiers</c> keys need to be pressed in order or not.
        /// </summary>
        public enum ModifiersOrder
        {
            /// <summary>
            /// By default, if the setting <see cref="InputSettings.shortcutKeysConsumeInput"/> is enabled,
            /// if <see cref="binding"/> is bound to only <see cref="Controls.ButtonControl"/>s, then the composite requires
            /// both <see cref="modifier1"/> and <see cref="modifier2"/> to be pressed <em>before</em> pressing <see cref="binding"/>.
            /// This means that binding to, for example, <c>Ctrl+Shift+B</c>, the <c>ctrl</c> and <c>shift</c> keys have to be pressed, in any order,
            /// before pressing the <c>B</c> key. This is the behavior usually expected with keyboard shortcuts.
            ///
            /// If the setting <see cref="InputSettings.shortcutKeysConsumeInput"/> is disabled,
            /// modifiers can be pressed after the button and the composite will still trigger.
            /// </summary>
            Default = 0,

            /// <summary>
            /// if <see cref = "binding" /> is bound to only <see cref = "Controls.ButtonControl" /> s, then the composite requires
            /// both <see cref="modifier1"/> and <see cref="modifier2"/> to be pressed <em>before</em> pressing <see cref="binding"/>.
            /// This means that binding to, for example, <c>Ctrl+Shift+B</c>, the <c>ctrl</c> and <c>shift</c> keys have to be pressed, in any order,
            /// before pressing the <c>B</c> key. This is the behavior usually expected with keyboard shortcuts.
            /// </summary>
            Ordered = 1,

            /// <summary>
            /// <see cref="modifier1"/> and/or <see cref="modifier2"/> can be pressed after <see cref="binding"/>
            /// and the composite will still trigger. The only requirement is for all of them to concurrently be in pressed state.
            /// </summary>
            Unordered = 2
        }

        /// <summary>
        /// If set to <c>Ordered</c> or <c>Unordered</c>, the built-in logic to determine if modifiers need to be pressed first is overridden.
        /// </summary>
        /// <remarks>
        /// By default, if the setting <see cref="InputSettings.shortcutKeysConsumeInput"/> is enabled,
        /// if <see cref="binding"/> is bound to only <see cref="Controls.ButtonControl"/>s, then the composite requires
        /// both <see cref="modifier1"/> and <see cref="modifier2"/> to be pressed <em>before</em> pressing <see cref="binding"/>.
        /// This means that binding to, for example, <c>Ctrl+Shift+B</c>, the <c>ctrl</c> and <c>shift</c> keys have to be pressed, in any order,
        /// before pressing the <c>B</c> key. This is the behavior usually expected with keyboard shortcuts.
        ///
        /// If the setting <see cref="InputSettings.shortcutKeysConsumeInput"/> is disabled,
        /// modifiers can be pressed after the button and the composite will still trigger.
        ///
        /// This field allows you to explicitly override this default inference and make the order mandatory or make it so that regardless of what <see cref="binding"/>
        /// is bound to, any press sequence is acceptable. For the example binding to <c>Ctrl+Shift+B</c>, it would mean that pressing
        /// <c>B</c> and only then pressing <c>Ctrl</c> and <c>Shift</c> will still trigger the binding.
        ///
        /// </remarks>
        [Tooltip("By default it follows the Input Consumption setting to determine if the modifers keys need to be pressed first.")]
        public ModifiersOrder modifiersOrder = ModifiersOrder.Default;

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
        private bool m_BindingIsButton;

        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            if (ModifiersArePressed(ref context))
                return context.EvaluateMagnitude(binding);
            return default;
        }

        /// <inheritdoc/>
        public override unsafe void ReadValue(ref InputBindingCompositeContext context, void* buffer, int bufferSize)
        {
            if (ModifiersArePressed(ref context))
                context.ReadValue(binding, buffer, bufferSize);
            else
                UnsafeUtility.MemClear(buffer, m_ValueSizeInBytes);
        }

        private bool ModifiersArePressed(ref InputBindingCompositeContext context)
        {
            var modifiersDown = context.ReadValueAsButton(modifier1) && context.ReadValueAsButton(modifier2);

            // When the modifiers are gating a button, we require the modifiers to be pressed *first*.
            if (modifiersDown && m_BindingIsButton && modifiersOrder == ModifiersOrder.Ordered)
            {
                var timestamp = context.GetPressTime(binding);
                var timestamp1 = context.GetPressTime(modifier1);
                var timestamp2 = context.GetPressTime(modifier2);

                return timestamp1 <= timestamp && timestamp2 <= timestamp;
            }

            return modifiersDown;
        }

        /// <inheritdoc/>
        protected override void FinishSetup(ref InputBindingCompositeContext context)
        {
            OneModifierComposite.DetermineValueTypeAndSize(ref context, binding, out m_ValueType, out m_ValueSizeInBytes, out m_BindingIsButton);

            if (modifiersOrder == ModifiersOrder.Default)
            {
                // Legacy. We need to reference the obsolete member here so temporarily
                // turn off the warning.
#pragma warning disable CS0618
                if (overrideModifiersNeedToBePressedFirst)
#pragma warning restore CS0618
                    modifiersOrder = ModifiersOrder.Unordered;
                else
                    modifiersOrder = InputSystem.settings.shortcutKeysConsumeInput ? ModifiersOrder.Ordered : ModifiersOrder.Unordered;
            }
        }

        public override object ReadValueAsObject(ref InputBindingCompositeContext context)
        {
            if (context.ReadValueAsButton(modifier1) && context.ReadValueAsButton(modifier2))
                return context.ReadValueAsObject(binding);
            return null;
        }
    }
}
