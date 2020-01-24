using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;

namespace UnityEngine.InputSystem.Composites
{
    /// <summary>
    /// A button with two additional modifiers. The button only triggers when
    /// both modifiers are pressed.
    /// </summary>
    /// <remarks>
    /// This composite can be used to require two other buttons to be held while
    /// pressing the button that triggers the action. This is most commonly used
    /// on keyboards to require two of the modifier keys (shift, ctrl, or alt)
    /// to be held in combination with another key, e.g. "CTRL+SHIFT+1".
    ///
    /// <example>
    /// <code>
    /// // Create a button action that triggers when CTRL+SHIFT+1
    /// // is pressed on the keyboard.
    /// var action = new InputAction(type: InputActionType.Button);
    /// action.AddCompositeBinding("ButtonWithTwoModifiers")
    ///     .With("Modifier1", "&lt;Keyboard&gt;/leftCtrl")
    ///     .With("Modifier1", "&lt;Keyboard&gt;/rightCtrl")
    ///     .With("Modifier2", "&lt;Keyboard&gt;/leftShift")
    ///     .With("Modifier2", "&lt;Keyboard&gt;/rightShift")
    ///     .With("Button", "&lt;Keyboard&gt;/1")
    /// </code>
    /// </example>
    ///
    /// Note that this is not restricted to the keyboard and will preserve
    /// the full value of the button.
    ///
    /// <example>
    /// <code>
    /// // Create a button action that requires the A and X button on the
    /// // gamepad to be held and will then trigger from the gamepad's
    /// // left trigger button.
    /// var action = new InputAction(type: InputActionType.Button);
    /// action.AddCompositeBinding("ButtonWithTwoModifiers")
    ///     .With("Modifier1", "&lt;Gamepad&gt;/buttonSouth")
    ///     .With("Modifier2", "&lt;Gamepad&gt;/buttonWest")
    ///     .With("Button", "&lt;Gamepad&gt;/leftTrigger");
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="ButtonWithOneModifier"/>
    [Preserve]
    [DisplayStringFormat("{modifier1}+{modifier2}+{button}")]
    public class ButtonWithTwoModifiers : InputBindingComposite<float>
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
        /// Binding for the button that is gated by the <see cref="modifier1"/> and <see cref="modifier2"/>.
        /// The composite will assume the value of this button while both of the modifiers are pressed.
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
        /// Return the value of the <see cref="button"/> part while both <see cref="modifier1"/> and <see cref="modifier2"/>
        /// are pressed. Otherwise return 0.
        /// </summary>
        /// <param name="context">Evaluation context passed in from the input system.</param>
        /// <returns>The current value of the composite.</returns>
        public override float ReadValue(ref InputBindingCompositeContext context)
        {
            if (context.ReadValueAsButton(modifier1) && context.ReadValueAsButton(modifier2))
                return context.ReadValue<float>(button);

            return default;
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
    }
}
