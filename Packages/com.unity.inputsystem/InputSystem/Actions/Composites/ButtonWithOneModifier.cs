using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;

////TODO: generalize the WithModifier composites so that they work with any kind of control, not just buttons

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
    [Preserve]
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
        /// Return the value of the <see cref="button"/> part if <see cref="modifier"/> is pressed. Otherwise
        /// return 0.
        /// </summary>
        /// <param name="context">Evaluation context passed in from the input system.</param>
        /// <returns>The current value of the composite.</returns>
        public override float ReadValue(ref InputBindingCompositeContext context)
        {
            if (context.ReadValueAsButton(modifier))
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
