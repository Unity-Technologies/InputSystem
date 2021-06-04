using System;

////WIP

//how is it achieved that this applies on a per-device level rather than for all devices of a given type?
//do we do this at the event-level?


//where to do perform the input modification? pre-source or post-source?
//pre-source:
//  + can be persisted such that the outcome is agnostic to user profile settings
//  - no longer possible to have input routed to multiple users from same device
//
//post-source:
//  + no need to modify device

////REVIEW: add ability to have per-device (or device layout?) settings?

namespace UnityEngine.InputSystem.Users
{
    /// <summary>
    /// A user profile may alter select aspects of input behavior at runtime.
    /// </summary>
    /// <remarks>
    /// This class implements several user adjustable input behaviors commonly found in games, such
    /// as mouse sensitivity and axis inversion.
    ///
    /// Note that the behaviors only work in combination with actions, that is, for users that have
    /// actions associated with them via <see cref="InputUser.AssociateActionsWithUser(IInputActionCollection)"/>.
    /// The behaviors do not alter the input as present directly on the devices. Meaning that, for example,
    /// <see cref="invertMouseX"/> will not impact <see cref="Vector2.x"/> of <see cref="Mouse.delta"/> but will
    /// rather impact the value read out with <see cref="InputAction.CallbackContext.ReadValue"/> from an action
    /// bound to mouse deltas.
    ///
    /// In other words, all the input behaviors operate at the binding level and modify <see cref="InputBinding"/>s.
    /// ////REVIEW: does this really make sense?
    /// </remarks>
    [Serializable]
    internal class InputUserSettings
    {
        /// <summary>
        /// Customized bindings for the user.
        /// </summary>
        /// <remarks>
        /// This will only contain customizations explicitly applied to the user's bindings
        /// and will not contain default bindings. It is thus not a complete set of bindings
        /// but rather just a set of customizations.
        /// </remarks>
        public string customBindings { get; set; }

        ////REVIEW: for this to impact position, too, we need to know the screen dimensions
        /// <summary>
        /// Invert X on <see cref="Mouse.position"/>  and <see cref="Mouse.delta"/>.
        /// </summary>
        public bool invertMouseX { get; set; }

        /// <summary>
        /// Invert Y on <see cref="Mouse.position"/>  and <see cref="Mouse.delta"/>.
        /// </summary>
        public bool invertMouseY { get; set; }

        /// <summary>
        /// Smooth mouse motion on both X and Y ...
        /// </summary>
        public float? mouseSmoothing { get; set; }

        public float? mouseSensitivity { get; set; }

        /// <summary>
        /// Invert X axis on <see cref="Gamepad.leftStick"/> and <see cref="Gamepad.rightStick"/>.
        /// </summary>
        public bool invertStickX { get; set; }

        /// <summary>
        /// Invert Y axis on <see cref="Gamepad.leftStick"/> and <see cref="Gamepad.rightStick"/>.
        /// </summary>
        public bool invertStickY { get; set; }

        /// <summary>
        /// If true, swap sides
        /// </summary>
        public bool swapSticks { get; set; }

        /// <summary>
        /// Swap <see cref="Gamepad.leftShoulder"/> and <see cref="Gamepad.rightShoulder"/> on gamepads.
        /// </summary>
        public bool swapBumpers { get; set; }

        /// <summary>
        /// Swap <see cref="Gamepad.leftTrigger"/> and <see cref="Gamepad.rightTrigger"/> on gamepads.
        /// </summary>
        public bool swapTriggers { get; set; }

        public bool swapDpadAndLeftStick { get; set; }

        public float vibrationStrength { get; set; }

        public virtual void Apply(IInputActionCollection actions)
        {
            //set overrideProcessors and redirectPaths on respective bindings
        }

        [SerializeField] private string m_CustomBindings;
    }
}
