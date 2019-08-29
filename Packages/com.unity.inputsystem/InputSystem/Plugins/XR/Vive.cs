using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// An HTC Vive VR headset.
    /// </summary>
    [InputControlLayout]
    [Scripting.Preserve]
    public class ViveHMD : XRHMD
    {
        [InputControl]
        public IntegerControl trackingState { get; private set; }
        [InputControl]
        public ButtonControl isTracked { get; private set; }
        [InputControl]
        public Vector3Control devicePosition { get; private set; }
        [InputControl]
        public QuaternionControl deviceRotation { get; private set; }
        [InputControl]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl]
        public Vector3Control deviceAngularVelocity { get; private set; }
        [InputControl]
        public Vector3Control leftEyePosition { get; private set; }
        [InputControl]
        public QuaternionControl leftEyeRotation { get; private set; }
        [InputControl]
        public Vector3Control leftEyeVelocity { get; private set; }
        [InputControl]
        public Vector3Control leftEyeAngularVelocity { get; private set; }
        [InputControl]
        public Vector3Control rightEyePosition { get; private set; }
        [InputControl]
        public QuaternionControl rightEyeRotation { get; private set; }
        [InputControl]
        public Vector3Control rightEyeVelocity { get; private set; }
        [InputControl]
        public Vector3Control rightEyeAngularVelocity { get; private set; }
        [InputControl]
        public Vector3Control centerEyePosition { get; private set; }
        [InputControl]
        public QuaternionControl centerEyeRotation { get; private set; }
        [InputControl]
        public Vector3Control centerEyeVelocity { get; private set; }
        [InputControl]
        public Vector3Control centerEyeAngularVelocity { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            leftEyePosition = GetChildControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = GetChildControl<QuaternionControl>("leftEyeRotation");
            leftEyeVelocity = GetChildControl<Vector3Control>("leftEyeVelocity");
            leftEyeAngularVelocity = GetChildControl<Vector3Control>("leftEyeAngularVelocity");
            rightEyePosition = GetChildControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = GetChildControl<QuaternionControl>("rightEyeRotation");
            rightEyeVelocity = GetChildControl<Vector3Control>("rightEyeVelocity");
            rightEyeAngularVelocity = GetChildControl<Vector3Control>("rightEyeAngularVelocity");
            centerEyePosition = GetChildControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = GetChildControl<QuaternionControl>("centerEyeRotation");
            centerEyeVelocity = GetChildControl<Vector3Control>("centerEyeVelocity");
            centerEyeAngularVelocity = GetChildControl<Vector3Control>("centerEyeAngularVelocity");
        }
    }

    /// <summary>
    /// An HTC Vive Wand controller.
    /// </summary>
    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    [Scripting.Preserve]
    public class ViveWand : XRControllerWithRumble
    {
        [InputControl]
        public AxisControl grip { get; private set; }
        [InputControl]
        public ButtonControl gripPressed { get; private set; }
        [InputControl]
        public ButtonControl primary { get; private set; }
        [InputControl(aliases = new[] { "JoystickOrPadPressed" })]
        public ButtonControl trackpadPressed { get; private set; }
        [InputControl(aliases = new[] { "JoystickOrPadTouched" })]
        public ButtonControl trackpadTouched { get; private set; }
        [InputControl(aliases = new[] { "Primary2DAxis" })]
        public Vector2Control trackpad { get; private set; }
        [InputControl]
        public AxisControl trigger { get; private set; }
        [InputControl]
        public ButtonControl triggerPressed { get; private set; }

        [InputControl]
        public IntegerControl trackingState { get; private set; }
        [InputControl]
        public ButtonControl isTracked { get; private set; }
        [InputControl]
        public Vector3Control devicePosition { get; private set; }
        [InputControl]
        public QuaternionControl deviceRotation { get; private set; }
        [InputControl]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl]
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

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
        }
    }

    /// <summary>
    /// A Valve Knuckles VR controller.
    /// </summary>
    [InputControlLayout]
    [Scripting.Preserve]
    public class KnucklesController : XRControllerWithRumble
    {
        [InputControl(aliases = new[] { "B",  "Primary"})]
        public ButtonControl primaryButton { get; private set; }

        [InputControl(aliases = new[] { "JoystickOrPadPressed" })]
        public ButtonControl trackpadPressed { get; private set; }
        [InputControl(aliases = new[] { "JoystickOrPadTouched" })]
        public ButtonControl trackpadTouched { get; private set; }
        [InputControl(aliases = new[] { "Primary2DAxis" })]
        public Vector2Control trackpad { get; private set; }

        [InputControl]
        public AxisControl grip { get; private set; }

        [InputControl(aliases = new[] { "A",  "GripButton" })]
        public ButtonControl gripPressed { get; private set; }

        [InputControl]
        public AxisControl trigger { get; private set; }
        [InputControl]
        public ButtonControl triggerPressed { get; private set; }

        [InputControl]
        public IntegerControl trackingState { get; private set; }
        [InputControl]
        public ButtonControl isTracked { get; private set; }
        [InputControl]
        public Vector3Control devicePosition { get; private set; }
        [InputControl]
        public QuaternionControl deviceRotation { get; private set; }
        [InputControl]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl]
        public Vector3Control deviceAngularVelocity { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            primaryButton = GetChildControl<ButtonControl>("primary");
            trackpadPressed = GetChildControl<ButtonControl>("trackpadPressed");
            trackpadTouched = GetChildControl<ButtonControl>("trackpadTouched");
            trackpad = GetChildControl<Vector2Control>("trackpad");
            trigger = GetChildControl<AxisControl>("trigger");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
        }
    }

    /// <summary>
    /// An HTC Vive lighthouse.
    /// </summary>
    [InputControlLayout]
    [Scripting.Preserve]
    public class ViveLighthouse : InputDevice
    {
        [InputControl]
        public IntegerControl trackingState { get; private set; }
        [InputControl]
        public ButtonControl isTracked { get; private set; }
        [InputControl]
        public Vector3Control devicePosition { get; private set; }
        [InputControl]
        public QuaternionControl deviceRotation { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
        }
    }

    /// <summary>
    /// An HTC Vive tracker.
    /// </summary>
    [Scripting.Preserve]
    public class ViveTracker : InputDevice
    {
        [InputControl]
        public IntegerControl trackingState { get; private set; }
        [InputControl]
        public ButtonControl isTracked { get; private set; }
        [InputControl]
        public Vector3Control devicePosition { get; private set; }
        [InputControl]
        public QuaternionControl deviceRotation { get; private set; }
        [InputControl]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl]
        public Vector3Control deviceAngularVelocity { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
        }
    }

    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    [Scripting.Preserve]
    public class HandedViveTracker : ViveTracker
    {
        [InputControl]
        public AxisControl grip { get; private set; }
        [InputControl]
        public ButtonControl gripPressed { get; private set; }
        [InputControl]
        public ButtonControl primary { get; private set; }
        [InputControl(aliases = new[] { "JoystickOrPadPressed" })]
        public ButtonControl trackpadPressed { get; private set; }
        [InputControl]
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
}
