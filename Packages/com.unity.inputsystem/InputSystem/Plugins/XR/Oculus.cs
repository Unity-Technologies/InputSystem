using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
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


        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            leftEyePosition = builder.GetControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = builder.GetControl<QuaternionControl>("leftEyeRotation");
            rightEyePosition = builder.GetControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = builder.GetControl<QuaternionControl>("rightEyeRotation");
            centerEyePosition = builder.GetControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = builder.GetControl<QuaternionControl>("centerEyeRotation");
        }
    }

    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class OculusTouchController : XRControllerWithRumble
    {
        public AxisControl combinedTrigger { get; private set; }
        [InputControl(aliases = new[] { "Primary2DAxis" })]
        public Vector2Control joystick { get; private set; }

        public AxisControl trigger { get; private set; }

        public AxisControl grip { get; private set; }
        [InputControl(aliases = new[] { "TriggerPressed" })]
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
        [InputControl(aliases = new[] { "ATouch", "XTouch" })]
        public ButtonControl primaryTouched { get; private set; }
        [InputControl(aliases = new[] { "BTouch", "YTouch" })]
        public ButtonControl secondaryTouched { get; private set; }
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

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            combinedTrigger = builder.GetControl<AxisControl>("combinedTrigger");
            joystick = builder.GetControl<Vector2Control>("joystick");
            trigger = builder.GetControl<AxisControl>("trigger");
            grip = builder.GetControl<AxisControl>("grip");
            indexNearTouched = builder.GetControl<AxisControl>("indexNearTouched");
            thumbNearTouched = builder.GetControl<AxisControl>("thumbNearTouched");

            primaryButton = builder.GetControl<ButtonControl>("primaryButton");
            secondaryButton = builder.GetControl<ButtonControl>("secondaryButton");
            gripPressed = builder.GetControl<ButtonControl>("gripPressed");
            start = builder.GetControl<ButtonControl>("start");
            thumbstickClicked = builder.GetControl<ButtonControl>("thumbstickClicked");
            primaryTouched = builder.GetControl<ButtonControl>("primaryTouched");
            secondaryTouched = builder.GetControl<ButtonControl>("secondaryTouched");
            indexTouched = builder.GetControl<ButtonControl>("indexTouched");
            thumbstickTouched = builder.GetControl<ButtonControl>("thumbstickTouched");
            thumbrestTouched = builder.GetControl<ButtonControl>("thumbrestTouched");

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = builder.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = builder.GetControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = builder.GetControl<Vector3Control>("deviceAngularAcceleration");
        }
    }

    [InputControlLayout]
    public class OculusTrackingReference : InputDevice
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
        }
    }
}
