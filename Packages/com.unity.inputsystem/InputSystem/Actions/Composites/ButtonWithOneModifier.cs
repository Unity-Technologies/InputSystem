using System.ComponentModel;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;

////TODO: remove this once we can break the API

namespace UnityEngine.InputSystem.Composites
{
    /// <summary>
    /// A button with an additional modifier. The button only triggers when
    /// the modifier is pressed.
    /// </summary>
    /// <remarks>
    /// This composite can be used to require another button to be held while
    /// pressing the button that triggers the action. This is most commonly used
    /// on keyboards to require one of the modifier keys (shift, ctrl, or alt)
    /// to be held in combination with another key, e.g. "CTRL+1".
    ///
    /// <example>
    /// <code>
    /// // Create a button action that triggers when CTRL+1
    /// // is pressed on the keyboard.
    /// var action = new InputAction(type: InputActionType.Button);
    /// action.AddCompositeBinding("ButtonWithOneModifier")
    ///     .With("Modifier", "&lt;Keyboard&gt;/leftCtrl")
    ///     .With("Modifier", "&lt;Keyboard&gt;/rightControl")
    ///     .With("Button", "&lt;Keyboard&gt;/1")
    /// </code>
    /// </example>
    ///
    /// Note that this is not restricted to the keyboard and will preserve
    /// the full value of the button.
    ///
    /// <example>
    /// <code>
    /// // Create a button action that requires the A button on the
    /// // gamepad to be held and will then trigger from the gamepad's
    /// // left trigger button.
    /// var action = new InputAction(type: InputActionType.Button);
    /// action.AddCompositeBinding("ButtonWithOneModifier")
    ///     .With("Modifier", "&lt;Gamepad&gt;/buttonSouth")
    ///     .With("Button", "&lt;Gamepad&gt;/leftTrigger");
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="ButtonWithTwoModifiers"/>
    [DesignTimeVisible(false)] // Obsoleted by OneModifierComposite
    [DisplayStringFormat("{modifier}+{button}")]
    public class ButtonWithOneModifier : InputBindingComposite<float>
    {
        /// <summary>
        /// Binding for the button that acts as a modifier, e.g. <c>&lt;Keyboard/leftCtrl</c>.
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
        /// Binding for the button that is gated by the modifier. The composite will assume the value
        /// of this button while the modifier is pressed.
        /// </summary>
        /// <value>Part index to use with <see cref="InputBindingCompositeContext.ReadValue{T}(int)"/>.</value>
        /// <remarks>
        /// This property is automatically assigned by the input system.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once UnassignedField.Global
        [InputControl(layout = "Button")] public int button;

        /// <summary>
        /// If set to <c>true</c>, <see cref="modifier"/> can be pressed after <see cref="button"/> and the composite will
        /// still trigger. Default is false.
        /// </summary>
        /// <remarks>
        /// By default, <see cref="modifier"/> is required to be in pressed state before or at the same time that <see cref="button"/>
        /// goes into pressed state for the composite as a whole to trigger. This means that binding to, for example, <c>Shift+B</c>,
        /// the <c>shift</c> key has to be pressed before pressing the <c>B</c> key. This is the behavior usually expected with
        /// keyboard shortcuts.
        ///
        /// This parameter can be used to bypass this behavior and allow any timing between <see cref="modifier"/> and <see cref="button"/>.
        /// The only requirement is for them both to concurrently be in pressed state.
        /// </remarks>
        public bool overrideModifiersNeedToBePressedFirst;

        /// <summary>
        /// Return the value of the <see cref="button"/> part if <see cref="modifier"/> is pressed. Otherwise
        /// return 0.
        /// </summary>
        /// <param name="context">Evaluation context passed in from the input system.</param>
        /// <returns>The current value of the composite.</returns>
        public override float ReadValue(ref InputBindingCompositeContext context)
        {
            if (ModifierIsPressed(ref context))
                return context.ReadValue<float>(button);

            return default;
        }

        private bool ModifierIsPressed(ref InputBindingCompositeContext context)
        {
            var modifierDown = context.ReadValueAsButton(modifier);

            if (modifierDown && !overrideModifiersNeedToBePressedFirst)
            {
                var timestamp = context.GetPressTime(button);
                var timestamp1 = context.GetPressTime(modifier);

                return timestamp1 <= timestamp;
            }

            return modifierDown;
        }

        /// <summary>
        /// Same as <see cref="ReadValue"/> in this case.
        /// </summary>
        /// <param name="context">Evaluation context passed in from the input system.</param>
        /// <returns>A >0 value if the composite is currently actuated.</returns>
        public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
        {
            return ReadValue(ref context);
        }

        protected override void FinishSetup(ref InputBindingCompositeContext context)
        {
            if (!overrideModifiersNeedToBePressedFirst)
                overrideModifiersNeedToBePressedFirst = !InputSystem.settings.shortcutKeysConsumeInput;
        }
    }
}
