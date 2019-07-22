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
    /// can react to changes in values on the controls it is bound to. The most straightforward type
    /// of behavior is <see cref="PassThrough"/> which does not expect any kind of value change pattern
    /// but simply triggers the action on every value change.
    ///
    /// In addition, there is <see cref="Button"/> which TODO
    /// </remarks>
    public enum InputActionType
    {
        /// <summary>
        /// An action that reads a value from its connected sources.
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
        /// stick would be ignored.
        ///
        /// The second key difference is that only <see cref="InputActionPhase.Performed"/> is used and will get triggered
        /// on every value change regardless of what the value is. This is different from <see cref="Value"/> where the
        /// action will trigger <see cref="InputActionPhase.Started"/> when moving away from its default value and will
        /// trigger <see cref="InputActionPhase.Canceled"/> when going back to the default value.
        /// </remarks>
        PassThrough,
    }
}
