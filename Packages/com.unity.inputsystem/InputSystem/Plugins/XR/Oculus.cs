using UnityEngine.Experimental.Input.Controls;

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
        public Vector2Control joystick { get; private set; }

        public AxisControl trigger { get; private set; }
        public AxisControl grip { get; private set; }
        public AxisControl indexNearTouch { get; private set; }
        public AxisControl thumbNearTouch { get; private set; }

        public ButtonControl primaryButton { get; private set; }
        public ButtonControl secondaryButton { get; private set; }
        public ButtonControl start { get; private set; }
        public ButtonControl thumbstickClick { get; private set; }
        public ButtonControl primaryTouch { get; private set; }
        public ButtonControl secondaryTouch { get; private set; }
        public ButtonControl indexTouch { get; private set; }
        public ButtonControl thumbstickTouch { get; private set; }
        public ButtonControl thumbrestTouch { get; private set; }

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
            indexNearTouch = builder.GetControl<AxisControl>("indexNearTouch");
            thumbNearTouch = builder.GetControl<AxisControl>("thumbNearTouch");

            primaryButton = builder.GetControl<ButtonControl>("primaryButton");
            secondaryButton = builder.GetControl<ButtonControl>("secondaryButton");
            start = builder.GetControl<ButtonControl>("start");
            thumbstickClick = builder.GetControl<ButtonControl>("thumbstickClick");
            primaryTouch = builder.GetControl<ButtonControl>("primaryTouch");
            secondaryTouch = builder.GetControl<ButtonControl>("secondaryTouch");
            indexTouch = builder.GetControl<ButtonControl>("indexTouch");
            thumbstickTouch = builder.GetControl<ButtonControl>("thumbstickTouch");
            thumbrestTouch = builder.GetControl<ButtonControl>("thumbrestTouch");

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
