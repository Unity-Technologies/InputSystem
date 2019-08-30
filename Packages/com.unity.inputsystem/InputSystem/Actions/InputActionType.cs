// value: initialStateCheck=true, passThrough=false
// button: initialStateCheck=false, passThrough=false
// passThrough: initialStateCheck=false, passThrough=true

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Determines the behavior with which an <see cref="InputAction"/> triggers.
    /// </summary>
    /// <remarks>
    /// While all actions essentially function the same way, there are differences in how an action
    /// will react to changes in values on the controls it is bound to.
    ///
    /// The most straightforward type of behavior is <see cref="PassThrough"/> which does not expect
    /// any kind of value change pattern but simply triggers the action on every single value change.
    /// A pass-through action will not use <see cref="InputAction.started"/> or
    /// <see cref="InputAction.canceled"/> except on bindings that have an interaction added to them.
    /// Pass-through actions are most useful for sourcing input from arbitrary many controls and
    /// simply piping all input through without much processing on the side of the action.
    ///
    /// <example>
    /// <code>
    /// // An action that triggers every time any button on the gamepad is
    /// // pressed or released.
    /// var action = new InputAction(
    ///     type: InputActionType.PassThrough,
    ///     binding: "&lt;Gamepad&gt;/&lt;Button&gt;");
    ///
    /// action.performed +=
    ///     ctx =>
    ///     {
    ///         var button = (ButtonControl)ctx.control;
    ///         if (button.wasPressedThisFrame)
    ///             Debug.Log($"Button {ctx.control} was pressed");
    ///         else if (button.wasReleasedThisFrame)
    ///             Debug.Log($"Button {ctx.control} was released");
    ///         // NOTE: We may get calls here in which neither the if nor the else
    ///         //       clause are true here. A button like the gamepad left and right
    ///         //       triggers, for example, do not just have a binary on/off state
    ///         //       but rather a [0..1] value range.
    ///     };
    ///
    /// action.Enable();
    /// </code>
    /// </example>
    ///
    /// Note that pass-through actions do not perform any kind of disambiguation of input
    /// which makes them great for just forwarding input from any connected controls but
    /// makes them a poor choice when only one input should be generated from however
    /// many connected controls there are. For more details, see <a
    /// href="../manual/ActionBindings.md#disambiguation">here</a>.
    ///
    /// The other two behavior types are <see cref="Button"/> and <see cref="Value"/>.
    ///
    /// A <see cref="Value"/> action starts
    ///
    /// <example>
    /// <code>
    /// </code>
    /// </example>
    ///
    /// A <see cref="Button"/> action, on the other hand,
    ///
    /// There is one final difference between <see cref="Value"/> compared to <see cref="Button"/>
    /// and <see cref="PassThrough"/> actions. A <see cref="Value"/> action will perform an
    /// initial state check on the first input system update after the action was enabled. What
    /// this means in practice is that when a value action is bound to, say, the left stick on a
    /// gamepad and the stick is already moved out of its resting position, then the action will
    /// immediately trigger instead of first requiring the stick to be moved slightly.
    ///
    /// <see cref="Button"/> and <see cref="PassThrough"/> actions, on the other hand, perform
    /// no such initial state check. For buttons, for example, this means that if a button is
    /// already pressed when an action is enabled, it first has to be released and then
    /// pressed again for the action to be triggered.
    ///
    /// For more details about initial state checks, see <a
    /// href="../manual/ActionBindings.md#initial-state-check">here</a>.
    /// </remarks>
    /// <seealso cref="InputAction.type"/>
    public enum InputActionType
    {
        /// <summary>
        /// An action that reads a single value from its connected sources. If multiple bindings
        /// actuate at the same time, performs disambiguation (see <see
        /// cref="../manual/ActionBindings.md#disambiguation"/>) to detect the highest value contributor
        /// at any one time.
        /// </summary>
        /// <remarks>
        /// A value action starts (<see cref="InputActionPhase.Started"/>) and then performs (<see cref="InputActionPhase.Performed"/>)
        /// as soon as a bound control changes to a non-default value. For example, if an action is bound to <see cref="Gamepad.leftStick"/>
        /// and the stick moves from (0,0) to (0.5,0.5), the action starts and performs.
        ///
        /// After being started, the action will perform on every value change that is not the default value. In the example here, if
        /// the stick goes to (0.75,0.75) and then to (1,1), the action will perform twice.
        ///
        /// Finally, if the control value changes back to the default value, the action is canceled (<see cref="InputActionPhase.Canceled"/>).
        /// Meaning that if the stick moves back to (0,0), <see cref="InputAction.canceled"/> will be triggered.
        ///
        /// If multiple controls are bound to the TODO
        ///
        /// Note that unlike <see cref="Button"/> actions, value actions perform an initial state check after being enabled.
        /// What this means is that if, for example, a stick is already actuated when an action is en TODO
        /// </remarks>
        Value,

        /// <summary>
        /// An action that acts as a trigger.
        /// </summary>
        /// <remarks>
        /// A button action has a defined trigger point that corresponds to <see cref="InputActionPhase.Performed"/>.
        /// After being performed, the action goes back to waiting state to await the next triggering.
        ///
        /// Note that a button action may still use <see cref="InputActionPhase.Started"/> and does not necessarily
        /// trigger immediately on input. For example, if <see cref="Interactions.HoldInteraction"/> is used, the
        /// action will start as soon as a bound button crosses its press threshold but will not trigger until the
        /// button is held for the set hold duration (<see cref="Interactions.HoldInteraction.duration"/>).
        /// </remarks>
        Button,

        /// <summary>
        /// An action that has no specific type of behavior and instead acts as a simple pass-through for
        /// any value change on any bound control.
        /// </summary>
        /// <remarks>
        /// This is in some ways similar to <see cref="Value"/>. However, there are two key differences.
        ///
        /// For one,  the action will not perform any disambiguation when bound to multiple controls concurrently.
        /// This means that if, for example, the action is bound to both the left and the right stick on a <see cref="Gamepad"/>,
        /// and the left stick goes to (0.5,0.5) and the right stick then goes to (0.25,0.25), the action will perform
        /// twice yielding a value of (0.5,0.5) first and a value of (0.25, 0.25) next. This is different from <see cref="Value"/>
        /// where upon actuation to (0.5,0.5), the left stick would get to drive the action and the actuation of the right
        /// stick would be ignored as it does not exceed the magnitude of the actuation on the left stick.
        ///
        /// The second key difference is that only <see cref="InputActionPhase.Performed"/> is used and will get triggered
        /// on every value change regardless of what the value is. This is different from <see cref="Value"/> where the
        /// action will trigger <see cref="InputActionPhase.Started"/> when moving away from its default value and will
        /// trigger <see cref="InputActionPhase.Canceled"/> when going back to the default value.
        /// </remarks>
        PassThrough,
    }
}
