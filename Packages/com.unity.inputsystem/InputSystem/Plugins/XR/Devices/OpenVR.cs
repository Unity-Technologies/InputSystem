#if UNITY_INPUT_SYSTEM_ENABLE_XR && ENABLE_VR && !DISABLE_BUILTIN_INPUT_SYSTEM_OPENVR && !PACKAGE_DOCS_GENERATION
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;

namespace Unity.XR.OpenVR
{
    [InputControlLayout(displayName = "OpenVR Headset")]
    [Preserve]
    public class OpenVRHMD : XRHMD
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control leftEyeVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control leftEyeAngularVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control rightEyeVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control rightEyeAngularVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control centerEyeVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control centerEyeAngularVelocity { get; private set; }

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

    [InputControlLayout(displayName = "Windows MR Controller (OpenVR)", commonUsages = new[] { "LeftHand", "RightHand" })]
    [Preserve]
    public class OpenVRControllerWMR : XRController
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

        [InputControl(aliases = new[] { "primary2DAxisClick", "joystickOrPadPressed" })]
        [Preserve]
        public ButtonControl touchpadClick { get; private set; }
        [InputControl(aliases = new[] { "primary2DAxisTouch", "joystickOrPadTouched" })]
        [Preserve]
        public ButtonControl touchpadTouch { get; private set; }
        [InputControl]
        [Preserve]
        public ButtonControl gripPressed { get; private set; }
        [InputControl]
        public ButtonControl triggerPressed { get; private set; }
        [InputControl(aliases = new[] { "primary" })]
        [Preserve]
        public ButtonControl menu { get; private set; }

        [InputControl]
        [Preserve]
        public AxisControl trigger { get; private set; }
        [InputControl]
        [Preserve]
        public AxisControl grip { get; private set; }

        [InputControl(aliases = new[] { "secondary2DAxis" })]
        [Preserve]
        public Vector2Control touchpad { get; private set; }
        [InputControl(aliases = new[] { "primary2DAxis" })]
        [Preserve]
        public Vector2Control joystick { get; private set; }

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
    [InputControlLayout(displayName = "Vive Wand", commonUsages = new[] { "LeftHand", "RightHand" })]
    [Preserve]
    public class ViveWand : XRControllerWithRumble
    {
        [InputControl]
        [Preserve]
        public AxisControl grip { get; private set; }
        [InputControl]
        [Preserve]
        public ButtonControl gripPressed { get; private set; }
        [InputControl]
        [Preserve]
        public ButtonControl primary { get; private set; }
        [InputControl(aliases = new[] { "primary2DAxisClick", "joystickOrPadPressed" })]
        [Preserve]
        public ButtonControl trackpadPressed { get; private set; }
        [InputControl(aliases = new[] { "primary2DAxisTouch", "joystickOrPadTouched" })]
        [Preserve]
        public ButtonControl trackpadTouched { get; private set; }
        [InputControl(aliases = new[] { "Primary2DAxis" })]
        [Preserve]
        public Vector2Control trackpad { get; private set; }
        [InputControl]
        [Preserve]
        public AxisControl trigger { get; private set; }
        [InputControl]
        [Preserve]
        public ButtonControl triggerPressed { get; private set; }

        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

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
    [InputControlLayout(displayName = "Vive Lighthouse")]
    [Preserve]
    public class ViveLighthouse : TrackedDevice
    {
    }

    /// <summary>
    /// An HTC Vive tracker.
    /// </summary>
    [InputControlLayout(displayName = "Vive Tracker")]
    [Preserve]
    public class ViveTracker : TrackedDevice
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
        }
    }

    [InputControlLayout(displayName = "Handed Vive Tracker", commonUsages = new[] { "LeftHand", "RightHand" })]
    [Preserve]
    public class HandedViveTracker : ViveTracker
    {
        [InputControl]
        [Preserve]
        public AxisControl grip { get; private set; }
        [InputControl]
        [Preserve]
        public ButtonControl gripPressed { get; private set; }
        [InputControl]
        [Preserve]
        public ButtonControl primary { get; private set; }
        [InputControl(aliases = new[] { "JoystickOrPadPressed" })]
        [Preserve]
        public ButtonControl trackpadPressed { get; private set; }
        [InputControl]
        [Preserve]
        public ButtonControl triggerPressed { get; private set; }

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
    [InputControlLayout(displayName = "Oculus Touch Controller (OpenVR)", commonUsages = new[] { "LeftHand", "RightHand" })]
    [Preserve]
    public class OpenVROculusTouchController : XRControllerWithRumble
    {
        [InputControl]
        [Preserve]
        public Vector2Control thumbstick { get; private set; }

        [InputControl]
        [Preserve]
        public AxisControl trigger { get; private set; }
        [InputControl]
        [Preserve]
        public AxisControl grip { get; private set; }

        // Primary & Secondary are switched in order to retain consistency with the Oculus SDK
        [InputControl(aliases = new[] { "Alternate" })]
        [Preserve]
        public ButtonControl primaryButton { get; private set; }
        [InputControl(aliases = new[] { "Primary" })]
        [Preserve]
        public ButtonControl secondaryButton { get; private set; }

        [InputControl]
        [Preserve]
        public ButtonControl gripPressed { get; private set; }
        [InputControl()]
        [Preserve]
        public ButtonControl triggerPressed { get; private set; }
        [InputControl(aliases = new[] { "primary2DAxisClicked" })]
        [Preserve]
        public ButtonControl thumbstickClicked { get; private set; }
        [InputControl(aliases = new[] { "primary2DAxisTouch" })]
        [Preserve]
        public ButtonControl thumbstickTouched { get; private set; }

        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control deviceAngularVelocity { get; private set; }

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
