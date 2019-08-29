using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// An Oculus VR headset (such as the Oculus Rift series of devices).
    /// </summary>
    [InputControlLayout]
    [Scripting.Preserve]
    public class OculusHMD : XRHMD
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
        public Vector3Control leftEyePosition { get; private set; }
        [InputControl]
        public QuaternionControl leftEyeRotation { get; private set; }
        [InputControl]
        public Vector3Control rightEyePosition { get; private set; }
        [InputControl]
        public QuaternionControl rightEyeRotation { get; private set; }
        [InputControl]
        public Vector3Control centerEyePosition { get; private set; }
        [InputControl]
        public QuaternionControl centerEyeRotation { get; private set; }


        protected override void FinishSetup()
        {
            base.FinishSetup();

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            leftEyePosition = GetChildControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = GetChildControl<QuaternionControl>("leftEyeRotation");
            rightEyePosition = GetChildControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = GetChildControl<QuaternionControl>("rightEyeRotation");
            centerEyePosition = GetChildControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = GetChildControl<QuaternionControl>("centerEyeRotation");
        }
    }

    /// <summary>
    /// An Oculus Touch controller.
    /// </summary>
    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    [Scripting.Preserve]
    public class OculusTouchController : XRControllerWithRumble
    {
        [InputControl(aliases = new[] { "Primary2DAxis", "Joystick" })]
        public Vector2Control thumbstick { get; private set; }

        [InputControl]
        public AxisControl trigger { get; private set; }

        [InputControl]
        public AxisControl grip { get; private set; }
        [InputControl]
        public AxisControl indexNearTouched { get; private set; }
        [InputControl]
        public AxisControl thumbNearTouched { get; private set; }

        [InputControl(aliases = new[] { "A", "X", "Alternate" })]
        public ButtonControl primaryButton { get; private set; }
        [InputControl(aliases = new[] { "B", "Y", "Primary" })]
        public ButtonControl secondaryButton { get; private set; }
        [InputControl]
        public ButtonControl gripPressed { get; private set; }
        [InputControl]
        public ButtonControl start { get; private set; }
        [InputControl(aliases = new[] { "JoystickOrPadPressed" })]
        public ButtonControl thumbstickClicked { get; private set; }
        [InputControl(aliases = new[] { "ATouched", "XTouched" })]
        public ButtonControl primaryTouched { get; private set; }
        [InputControl(aliases = new[] { "BTouched", "YTouched" })]
        public ButtonControl secondaryTouched { get; private set; }
        [InputControl(aliases = new[] { "TriggerPressed" })]
        public ButtonControl indexTouched { get; private set; }
        [InputControl(aliases = new[] { "JoystickOrPadTouched" })]
        public ButtonControl thumbstickTouched { get; private set; }
        [InputControl]
        public ButtonControl thumbrestTouched { get; private set; }

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
        public Vector3Control deviceAcceleration { get; private set; }
        [InputControl]
        public Vector3Control deviceAngularAcceleration { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            thumbstick = GetChildControl<Vector2Control>("thumbstick");
            trigger = GetChildControl<AxisControl>("trigger");
            grip = GetChildControl<AxisControl>("grip");
            indexNearTouched = GetChildControl<AxisControl>("indexNearTouched");
            thumbNearTouched = GetChildControl<AxisControl>("thumbNearTouched");

            primaryButton = GetChildControl<ButtonControl>("primaryButton");
            secondaryButton = GetChildControl<ButtonControl>("secondaryButton");
            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            start = GetChildControl<ButtonControl>("start");
            thumbstickClicked = GetChildControl<ButtonControl>("thumbstickClicked");
            primaryTouched = GetChildControl<ButtonControl>("primaryTouched");
            secondaryTouched = GetChildControl<ButtonControl>("secondaryTouched");
            indexTouched = GetChildControl<ButtonControl>("indexTouched");
            thumbstickTouched = GetChildControl<ButtonControl>("thumbstickTouched");
            thumbrestTouched = GetChildControl<ButtonControl>("thumbrestTouched");

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = GetChildControl<Vector3Control>("deviceAngularAcceleration");
        }
    }

    [InputControlLayout]
    [Scripting.Preserve]
    public class OculusTrackingReference : InputDevice
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
    /// An Oculus Remote controller.
    /// </summary>

    [Scripting.Preserve]
    public class OculusRemote : InputDevice
    {
        [InputControl]
        public ButtonControl back { get; private set; }
        [InputControl]
        public ButtonControl start { get; private set; }
        [InputControl]
        public Vector2Control touchpad { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            back = GetChildControl<ButtonControl>("back");
            start = GetChildControl<ButtonControl>("start");
            touchpad = GetChildControl<Vector2Control>("touchpad");
        }
    }
}
