#if !DISABLE_BUILTIN_INPUT_SYSTEM_OPENVR
// Docs generation is skipped because these are intended to be replaced with the com.unity.xr.openvr package.
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;

namespace Unity.XR.OpenVR
{
    [InputControlLayout(displayName = "OpenVR Headset", hideInUI = true)]
    public class OpenVRHMD : XRHMD
    {
        [InputControl(noisy = true)]
        public Vector3Control deviceVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control leftEyeVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control leftEyeAngularVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control rightEyeVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control rightEyeAngularVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control centerEyeVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control centerEyeAngularVelocity { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            leftEyeVelocity = GetChildControl<Vector3Control>("leftEyeVelocity");
            leftEyeAngularVelocity = GetChildControl<Vector3Control>("leftEyeAngularVelocity");
            rightEyeVelocity = GetChildControl<Vector3Control>("rightEyeVelocity");
            rightEyeAngularVelocity = GetChildControl<Vector3Control>("rightEyeAngularVelocity");
            centerEyeVelocity = GetChildControl<Vector3Control>("centerEyeVelocity");
            centerEyeAngularVelocity = GetChildControl<Vector3Control>("centerEyeAngularVelocity");
        }
    }

    [InputControlLayout(displayName = "Windows MR Controller (OpenVR)", commonUsages = new[] { "LeftHand", "RightHand" }, hideInUI = true)]
    public class OpenVRControllerWMR : XRController
    {
        [InputControl(noisy = true)]
        public Vector3Control deviceVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularVelocity { get; protected set; }

        [InputControl(aliases = new[] { "primary2DAxisClick", "joystickOrPadPressed" })]
        public ButtonControl touchpadClick { get; protected set; }
        [InputControl(aliases = new[] { "primary2DAxisTouch", "joystickOrPadTouched" })]
        public ButtonControl touchpadTouch { get; protected set; }
        [InputControl]
        public ButtonControl gripPressed { get; protected set; }
        [InputControl]
        public ButtonControl triggerPressed { get; protected set; }
        [InputControl(aliases = new[] { "primary" })]
        public ButtonControl menu { get; protected set; }

        [InputControl]
        public AxisControl trigger { get; protected set; }
        [InputControl]
        public AxisControl grip { get; protected set; }

        [InputControl(aliases = new[] { "secondary2DAxis" })]
        public Vector2Control touchpad { get; protected set; }
        [InputControl(aliases = new[] { "primary2DAxis" })]
        public Vector2Control joystick { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");

            touchpadClick = GetChildControl<ButtonControl>("touchpadClick");
            touchpadTouch = GetChildControl<ButtonControl>("touchpadTouch");
            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");
            menu = GetChildControl<ButtonControl>("menu");

            trigger = GetChildControl<AxisControl>("trigger");
            grip = GetChildControl<AxisControl>("grip");

            touchpad = GetChildControl<Vector2Control>("touchpad");
            joystick = GetChildControl<Vector2Control>("joystick");
        }
    }

    /// <summary>
    /// An HTC Vive Wand controller.
    /// </summary>
    [InputControlLayout(displayName = "Vive Wand", commonUsages = new[] { "LeftHand", "RightHand" }, hideInUI = true)]
    public class ViveWand : XRControllerWithRumble
    {
        [InputControl]
        public AxisControl grip { get; protected set; }
        [InputControl]
        public ButtonControl gripPressed { get; protected set; }
        [InputControl]
        public ButtonControl primary { get; protected set; }
        [InputControl(aliases = new[] { "primary2DAxisClick", "joystickOrPadPressed" })]
        public ButtonControl trackpadPressed { get; protected set; }
        [InputControl(aliases = new[] { "primary2DAxisTouch", "joystickOrPadTouched" })]
        public ButtonControl trackpadTouched { get; protected set; }
        [InputControl(aliases = new[] { "Primary2DAxis" })]
        public Vector2Control trackpad { get; protected set; }
        [InputControl]
        public AxisControl trigger { get; protected set; }
        [InputControl]
        public ButtonControl triggerPressed { get; protected set; }

        [InputControl(noisy = true)]
        public Vector3Control deviceVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularVelocity { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            grip = GetChildControl<AxisControl>("grip");
            primary = GetChildControl<ButtonControl>("primary");
            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            trackpadPressed = GetChildControl<ButtonControl>("trackpadPressed");
            trackpadTouched = GetChildControl<ButtonControl>("trackpadTouched");
            trackpad = GetChildControl<Vector2Control>("trackpad");
            trigger = GetChildControl<AxisControl>("trigger");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
        }
    }

    /// <summary>
    /// An HTC Vive lighthouse.
    /// </summary>
    [InputControlLayout(displayName = "Vive Lighthouse", hideInUI = true)]
    public class ViveLighthouse : TrackedDevice
    {
    }

    /// <summary>
    /// An HTC Vive tracker.
    /// </summary>
    [InputControlLayout(displayName = "Vive Tracker")]
    public class ViveTracker : TrackedDevice
    {
        [InputControl(noisy = true)]
        public Vector3Control deviceVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularVelocity { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
        }
    }

    [InputControlLayout(displayName = "Handed Vive Tracker", commonUsages = new[] { "LeftHand", "RightHand" }, hideInUI = true)]
    public class HandedViveTracker : ViveTracker
    {
        [InputControl]
        public AxisControl grip { get; protected set; }
        [InputControl]
        public ButtonControl gripPressed { get; protected set; }
        [InputControl]
        public ButtonControl primary { get; protected set; }
        [InputControl(aliases = new[] { "JoystickOrPadPressed" })]
        public ButtonControl trackpadPressed { get; protected set; }
        [InputControl]
        public ButtonControl triggerPressed { get; protected set; }

        protected override void FinishSetup()
        {
            grip = GetChildControl<AxisControl>("grip");
            primary = GetChildControl<ButtonControl>("primary");
            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            trackpadPressed = GetChildControl<ButtonControl>("trackpadPressed");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");

            base.FinishSetup();
        }
    }

    /// <summary>
    /// An Oculus Touch controller.
    /// </summary>
    [InputControlLayout(displayName = "Oculus Touch Controller (OpenVR)", commonUsages = new[] { "LeftHand", "RightHand" }, hideInUI = true)]
    public class OpenVROculusTouchController : XRControllerWithRumble
    {
        [InputControl]
        public Vector2Control thumbstick { get; protected set; }

        [InputControl]
        public AxisControl trigger { get; protected set; }
        [InputControl]
        public AxisControl grip { get; protected set; }

        // Primary & Secondary are switched in order to retain consistency with the Oculus SDK
        [InputControl(aliases = new[] { "Alternate" })]
        public ButtonControl primaryButton { get; protected set; }
        [InputControl(aliases = new[] { "Primary" })]
        public ButtonControl secondaryButton { get; protected set; }

        [InputControl]
        public ButtonControl gripPressed { get; protected set; }
        [InputControl]
        public ButtonControl triggerPressed { get; protected set; }
        [InputControl(aliases = new[] { "primary2DAxisClicked" })]
        public ButtonControl thumbstickClicked { get; protected set; }
        [InputControl(aliases = new[] { "primary2DAxisTouch" })]
        public ButtonControl thumbstickTouched { get; protected set; }

        [InputControl(noisy = true)]
        public Vector3Control deviceVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularVelocity { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            thumbstick = GetChildControl<Vector2Control>("thumbstick");

            trigger = GetChildControl<AxisControl>("trigger");
            grip = GetChildControl<AxisControl>("grip");

            primaryButton = GetChildControl<ButtonControl>("primaryButton");
            secondaryButton = GetChildControl<ButtonControl>("secondaryButton");
            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            thumbstickClicked = GetChildControl<ButtonControl>("thumbstickClicked");
            thumbstickTouched = GetChildControl<ButtonControl>("thumbstickTouched");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
        }
    }
}
#endif
