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
    /// href="../manual/ActionBindings.html#disambiguation">here</a>.
    ///
    /// The other two behavior types are <see cref="Button"/> and <see cref="Value"/>.
    ///
    /// A <see cref="Value"/> action starts (<see cref="InputAction.started"/>) as soon as its
    /// input moves away from its default value. After that it immediately performs (<see cref="InputAction.performed"/>)
    /// and every time the input changes value it performs again except if the input moves back
    /// to the default value -- in which case the action cancels (<see cref="InputAction.canceled"/>).
    ///
    /// Also, unlike both <see cref="Button"/> and <see cref="PassThrough"/> actions, <see cref="Value"/>
    /// actions perform what's called "initial state check" on the first input update after the action
    /// was enabled. What this does is check controls bound to the action and if they are already actuated
    /// (i.e. at non-default value), the action will immediately be started and performed. What
    /// this means in practice is that when a value action is bound to, say, the left stick on a
    /// gamepad and the stick is already moved out of its resting position, then the action will
    /// immediately trigger instead of first requiring the stick to be moved slightly.
    ///
    /// <see cref="Button"/> and <see cref="PassThrough"/> actions, on the other hand, perform
    /// no such initial state check. For buttons, for example, this means that if a button is
    /// already pressed when an action is enabled, it first has to be released and then
    /// pressed again for the action to be triggered.
    ///
    /// <example>
    /// <code>
    /// // An action that starts when the left stick on the gamepad is actuated
    /// // and stops when the stick is released.
    /// var action = new InputAction(
    ///     type: InputActionType.Value,
    ///     binding: "&lt;Gamepad&gt;/leftStick");
    ///
    /// action.started +=
    ///     ctx =>
    ///     {
    ///         Debug.Log("--- Stick Starts ---");
    ///     };
    /// action.performed +=
    ///     ctx =>
    ///     {
    ///         Debug.Log("Stick Value: " + ctx.ReadValue&lt;Vector2D&gt;();
    ///     };
    /// action.canceled +=
    ///     ctx =>
    ///     {
    ///         Debug.Log("# Stick Released");
    ///     };
    ///
    /// action.Enable();
    /// </code>
    /// </example>
    ///
    /// A <see cref="Button"/> action essentially operates like a <see cref="Value"/> action except
    /// that it does not perform an initial state check.
    ///
    /// One final noteworthy difference of both <see cref="Button"/> and <see cref="Value"/> compared
    /// to <see cref="PassThrough"/> is that both of them perform what is referred to as "disambiguation"
    /// when multiple actions are bound to the control. <see cref="PassThrough"/> does not care how
    /// many controls are bound to the action -- it simply passes every input through as is, no matter
    /// where it comes from.
    ///
    /// <see cref="Button"/> and <see cref="Value"/>, on the other hand, will treat input differently
    /// if it is coming from several sources at the same time. Note that this can only happen when there
    /// are multiple controls bound to a single actions -- either by a single binding resolving to
    /// more than one control (e.g. <c>"*/{PrimaryAction}"</c>) or by multiple bindings all targeting
    /// the same action and being active at the same time. If only a single control is bound to an
    /// action, then the disambiguation code is automatically bypassed.
    ///
    /// Disambiguation works the following way: when an action has not yet been started, it will react
    /// to the first input that has a non-default value. Once it receives such an input, it will start
    /// tracking the source of that input. While the action is in-progress, if it receives input from
    /// a source other than the control it is currently tracking, it will check whether the input has
    /// a greater magnitude (see <see cref="InputControl.EvaluateMagnitude()"/>) than the control the
    /// action is already tracking. If so, the action will switch from its current control to the control
    /// with the stronger input.
    ///
    /// Note that this process does also works in reverse. When the control currently driving the action
    /// lowers its value below that of another control that is also actuated and bound to the action,
    /// the action will switch to that control.
    ///
    /// Put simply, a <see cref="Button"/> or <see cref="Value"/> action bound to multiple controls will
    /// always track the control with the strongest input.
    /// </remarks>
    /// <seealso cref="InputAction.type"/>
    public enum InputActionType
    {
        /// <summary>
        /// An action that reads a single value from its connected sources. If multiple bindings
        /// actuate at the same time, performs disambiguation (see <see
        /// href="../manual/ActionBindings.html#disambiguation"/>) to detect the highest value contributor
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
