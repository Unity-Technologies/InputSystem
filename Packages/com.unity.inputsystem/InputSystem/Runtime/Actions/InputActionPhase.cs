using UnityEngine.InputSystem.Interactions;

////REVIEW: this goes beyond just actions; is there a better name? just InputPhase?

////REVIEW: what about opening up phases completely to interactions and allow them to come up with whatever custom phases?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Trigger phase of an <see cref="InputAction"/>.
    /// </summary>
    /// <remarks>
    /// Actions can be triggered in steps. For example, a <see cref="SlowTapInteraction">
    /// 'slow tap'</see> will put an action into <see cref="Started"/> phase when a button
    /// the action is bound to is pressed. At that point, however, the action still
    /// has to wait for the expiration of a timer in order to make it a 'slow tap'. If
    /// the button is release before the timer expires, the action will be <see cref="Canceled"/>
    /// whereas if the button is held long enough, the action will be <see cref="Performed"/>.
    /// </remarks>
    /// <seealso cref="InputAction.phase"/>
    /// <seealso cref="InputAction.CallbackContext.phase"/>
    /// <seealso cref="InputAction.started"/>
    /// <seealso cref="InputAction.performed"/>
    /// <seealso cref="InputAction.canceled"/>
    public enum InputActionPhase
    {
        /// <summary>
        /// The action is not enabled.
        /// </summary>
        Disabled,

        /// <summary>
        /// The action is enabled and waiting for input on its associated controls.
        ///
        /// This is the phase that an action goes back to once it has been <see cref="Performed"/>
        /// or <see cref="Canceled"/>.
        /// </summary>
        Waiting,

        /// <summary>
        /// An associated control has been actuated such that it may lead to the action
        /// being triggered. Will lead to <see cref="InputAction.started"/> getting called.
        ///
        /// This phase will only be invoked if there are interactions on the respective control
        /// binding. Without any interactions, an action will go straight from <see cref="Waiting"/>
        /// into <see cref="Performed"/> and back into <see cref="Waiting"/> whenever an associated
        /// control changes value.
        ///
        /// An example of an interaction that uses the <see cref="Started"/> phase is <see cref="SlowTapInteraction"/>.
        /// When the button it is bound to is pressed, the associated action goes into the <see cref="Started"/>
        /// phase. At this point, the interaction does not yet know whether the button press will result in just
        /// a tap or will indeed result in slow tap. If the button is released before the time it takes to
        /// recognize a slow tap, then the action will go to <see cref="Canceled"/> and then back to <see cref="Waiting"/>.
        /// If, however, the button is held long enough for it to qualify as a slow tap, the action will progress
        /// to <see cref="Performed"/> and then go back to <see cref="Waiting"/>.
        ///
        /// <see cref="Started"/> can be useful for UI feedback. For example, in a game where the weapon can be charged,
        /// UI feedback can be initiated when the action is <see cref="Started"/>.
        ///
        /// <example>
        /// <code>
        /// fireAction.started +=
        ///     ctx =>
        ///     {
        ///         if (ctx.interaction is SlowTapInteraction)
        ///         {
        ///             weaponCharging = true;
        ///             weaponChargeStartTime = ctx.time;
        ///         }
        ///     }
        /// fireAction.canceled +=
        ///     ctx =>
        ///     {
        ///         weaponCharging = false;
        ///     }
        /// fireAction.performed +=
        ///     ctx =>
        ///     {
        ///         Fire();
        ///         weaponCharging = false;
        ///     }
        /// </code>
        /// </example>
        ///
        /// By default, an action is started as soon as a control moves away from its default value. This is
        /// the case for both <see cref="InputActionType.Button"/> actions (which, however, does not yet have to mean
        /// that the button press threshold has been reached; refer to <see cref="InputSettings.defaultButtonPressPoint"/>)
        /// and <see cref="InputActionType.Value"/> actions. <see cref="InputActionType.PassThrough"/> does not use
        /// the <c>Started</c> phase and instead goes straight to <see cref="Performed"/>.
        ///
        /// For <see cref="InputActionType.Value"/> actions, <c>Started</c> will immediately be followed by <see cref="Performed"/>.
        ///
        /// Note that interactions (refer to <see cref="IInputInteraction"/>) can alter how an action does or does not progress through
        /// the phases.
        /// </summary>
        Started,

        /// <summary>
        /// The action has been performed. Leads to <see cref="InputAction.performed"/> getting called.
        ///
        /// By default, a <see cref="InputActionType.Button"/> action performs when a control crosses the button
        /// press threshold (refer to <see cref="InputSettings.defaultButtonPressPoint"/>), a <see cref="InputActionType.Value"/>
        /// action performs on any value change that isn't the default value, and a <see cref="InputActionType.PassThrough"/>
        /// action performs on any value change including going back to the default value.
        ///
        /// Note that interactions (refer to <see cref="IInputInteraction"/>) can alter how an action does or does not progress through
        /// the phases.
        ///
        /// For a given action, finding out whether it was performed in the current frame can be done with <see cref="InputAction.WasPerformedThisFrame"/>.
        ///
        /// <example>
        /// <code>
        /// action.WasPerformedThisFrame();
        /// </code>
        /// </example>
        /// </summary>
        Performed,

        /// <summary>
        /// The action has stopped. Leads to <see cref="InputAction.canceled"/> getting called.
        ///
        /// By default, a <see cref="InputActionType.Button"/> action cancels when a control falls back below the button
        /// press threshold (refer to <see cref="InputSettings.defaultButtonPressPoint"/>) and a <see cref="InputActionType.Value"/>
        /// action cancels when a control moves back to its default value. A <see cref="InputActionType.PassThrough"/> action
        /// does not generally cancel based on input on its controls.
        ///
        /// An action will also get canceled when it is disabled while in progress (refer to <see cref="InputAction.Disable"/>).
        ///
        /// Note that interactions (refer to <see cref="IInputInteraction"/>) can alter how an action does or does not progress through
        /// the phases.
        /// </summary>
        Canceled
    }
}
