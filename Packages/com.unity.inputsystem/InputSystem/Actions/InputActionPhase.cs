using UnityEngine.Experimental.Input.Interactions;

////REVIEW: this goes beyond just actions; is there a better name? just InputPhase?

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Trigger phase of an <see cref="InputAction">action</see>.
    /// </summary>
    /// <remarks>
    /// Actions can be triggered in steps. For example, a <see cref="SlowTapInteraction">
    /// 'slow tap'</see> will put an action into <see cref="Started"/> phase when a button
    /// the action is bound to is pressed. At that point, however, the action still
    /// has to wait for the expiration of a timer in order to make it a 'slow tap'. If
    /// the button is release before the timer expires, the action will be <see cref="Cancelled"/>
    /// whereas if the button is held long enough, the action will be <see cref="Performed"/>.
    /// </remarks>
    public enum InputActionPhase
    {
        /// <summary>
        /// The action is not enabled.
        /// </summary>
        Disabled,

        /// <summary>
        /// The action is enabled and waiting for input on its associated controls.
        /// </summary>
        /// <remarks>
        /// This is the phase that an action goes back to once it has been <see cref="Performed"/>
        /// or <see cref="Cancelled"/>.
        /// </remarks>
        Waiting,

        /// <summary>
        /// An associated control has been actuated such that it may lead to the action
        /// being triggered.
        /// </summary>
        /// <remarks>
        /// This phase will only be invoked if there are interactions on the respective control
        /// binding. Without any interactions, an action will go straight from <see cref="Waiting"/>
        /// into <see cref="Performed"/> and back into <see cref="Waiting"/> whenever an associated
        /// control changes value.
        ///
        /// An example of an interaction that uses the <see cref="Started"/> phase is <see cref="SlowTapInteraction"/>.
        /// When the button it is bound to is pressed, the associated action goes into the <see cref="Started"/>
        /// phase. At this point, the interaction does not yet know whether the button press will result in just
        /// a tap or will indeed result in slow tap. If the button is released before the time it takes to
        /// recognize a slow tap, then the action will go to <see cref="Cancelled"/> and then back to <see cref="Waiting"/>.
        /// If, however, the button is held long enough for it to qualify as a slow tap, the action will progress
        /// to <see cref="Performed"/> and then go back to <see cref="Waiting"/>.
        ///
        /// <see cref="Started"/> can be useful for UI feedback. For example, in a game where the weapon can be charged,
        /// UI feedback can be initiated when the action is <see cref="Started"/>.
        /// </remarks>
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
        /// fireAction.cancelled +=
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
        Started,

        /// <summary>
        ///
        /// </summary>
        /// <seealso cref="InputAction.performed"/>
        Performed,

        /// <summary>
        ///
        /// </summary>
        /// <seealso cref="InputAction.cancelled"/>
        Cancelled
    }
}
