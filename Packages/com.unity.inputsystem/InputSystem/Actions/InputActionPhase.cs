using UnityEngine.Experimental.Input.Modifiers;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Trigger phase of an <see cref="InputAction">action</see>.
    /// </summary>
    /// <remarks>
    /// Actions can be triggered in steps. For example, a <see cref="SlowTapModifier">
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
        /// This phase will only be invoked if there are modifiers on the respective control
        /// binding. Without any modifiers, an action will go straight from <see cref="Waiting"/>
        /// into <see cref="Performed"/> and back into <see cref="Waiting"/> whenever an associated
        /// control changes value.
        /// </remarks>
        Started,

        Performed,
        Cancelled
    }
}
