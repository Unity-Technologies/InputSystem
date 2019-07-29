using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// An Oculus VR headset (such as the Oculus Rift series of devices).
    /// </summary>
    [InputControlLayout]
    public class OculusHMD : XRHMD
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control leftEyePosition { get; private set; }
        public QuaternionControl leftEyeRotation { get; private set; }
        public Vector3Control rightEyePosition { get; private set; }
        public QuaternionControl rightEyeRotation { get; private set; }
        public Vector3Control centerEyePosition { get; private set; }
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
    public class OculusTouchController : XRControllerWithRumble
    {
        [InputControl(aliases = new[] { "Primary2DAxis", "Joystick" })]
        public Vector2Control thumbstick { get; private set; }

        public AxisControl trigger { get; private set; }

        public AxisControl grip { get; private set; }
        public AxisControl indexNearTouched { get; private set; }
        public AxisControl thumbNearTouched { get; private set; }

        [InputControl(aliases = new[] { "A", "X", "Alternate" })]
        public ButtonControl primaryButton { get; private set; }
        [InputControl(aliases = new[] { "B", "Y", "Primary" })]
        public ButtonControl secondaryButton { get; private set; }
        public ButtonControl gripPressed { get; private set; }
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
        public ButtonControl thumbrestTouched { get; private set; }

        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceVelocity { get; private set; }
        public Vector3Control deviceAngularVelocity { get; private set; }
        public Vector3Control deviceAcceleration { get; private set; }
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
    public class OculusTrackingReference : InputDevice
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
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

    public class OculusRemote : InputDevice
    {
        public ButtonControl back { get; private set; }
        public ButtonControl start { get; private set; }
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
