using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    [InputControlLayout]
    public class GearVRHMD : XRHMD
    {
        public Vector2Control touchpad { get; private set; }
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceVelocity { get; private set; }
        public Vector3Control deviceAngularVelocity { get; private set; }
        public Vector3Control deviceAcceleration { get; private set; }
        public Vector3Control deviceAngularAcceleration { get; private set; }
        public Vector3Control leftEyePosition { get; private set; }
        public QuaternionControl leftEyeRotation { get; private set; }
        public Vector3Control leftEyeVelocity { get; private set; }
        public Vector3Control leftEyeAngularVelocity { get; private set; }
        public Vector3Control leftEyeAcceleration { get; private set; }
        public Vector3Control leftEyeAngularAcceleration { get; private set; }
        public Vector3Control rightEyePosition { get; private set; }
        public QuaternionControl rightEyeRotation { get; private set; }
        public Vector3Control rightEyeVelocity { get; private set; }
        public Vector3Control rightEyeAngularVelocity { get; private set; }
        public Vector3Control rightEyeAcceleration { get; private set; }
        public Vector3Control rightEyeAngularAcceleration { get; private set; }
        public Vector3Control centerEyePosition { get; private set; }
        public QuaternionControl centerEyeRotation { get; private set; }
        public Vector3Control centerEyeVelocity { get; private set; }
        public Vector3Control centerEyeAngularVelocity { get; private set; }
        public Vector3Control centerEyeAcceleration { get; private set; }
        public Vector3Control centerEyeAngularAcceleration { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            touchpad = builder.GetControl<Vector2Control>("touchpad");
            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = builder.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = builder.GetControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = builder.GetControl<Vector3Control>("deviceAngularAcceleration");
            leftEyePosition = builder.GetControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = builder.GetControl<QuaternionControl>("leftEyeRotation");
            leftEyeVelocity = builder.GetControl<Vector3Control>("leftEyeVelocity");
            leftEyeAngularVelocity = builder.GetControl<Vector3Control>("leftEyeAngularVelocity");
            leftEyeAcceleration = builder.GetControl<Vector3Control>("leftEyeAcceleration");
            leftEyeAngularAcceleration = builder.GetControl<Vector3Control>("leftEyeAngularAcceleration");
            rightEyePosition = builder.GetControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = builder.GetControl<QuaternionControl>("rightEyeRotation");
            rightEyeVelocity = builder.GetControl<Vector3Control>("rightEyeVelocity");
            rightEyeAngularVelocity = builder.GetControl<Vector3Control>("rightEyeAngularVelocity");
            rightEyeAcceleration = builder.GetControl<Vector3Control>("rightEyeAcceleration");
            rightEyeAngularAcceleration = builder.GetControl<Vector3Control>("rightEyeAngularAcceleration");
            centerEyePosition = builder.GetControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = builder.GetControl<QuaternionControl>("centerEyeRotation");
            centerEyeVelocity = builder.GetControl<Vector3Control>("centerEyeVelocity");
            centerEyeAngularVelocity = builder.GetControl<Vector3Control>("centerEyeAngularVelocity");
            centerEyeAcceleration = builder.GetControl<Vector3Control>("centerEyeAcceleration");
            centerEyeAngularAcceleration = builder.GetControl<Vector3Control>("centerEyeAngularAcceleration");
        }
    }

    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class GearVRTrackedController : XRController
    {
        public AxisControl combinedTrigger { get; private set; }
        public Vector2Control joystick { get; private set; }
        public AxisControl trigger { get; private set; }
        public ButtonControl back { get; private set; }
        public ButtonControl triggerPressed { get; private set; }
        public ButtonControl touchpadClicked { get; private set; }
        public ButtonControl touchpadTouched { get; private set; }
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
            back = builder.GetControl<ButtonControl>("back");
            triggerPressed = builder.GetControl<ButtonControl>("triggerPressed");
            touchpadClicked = builder.GetControl<ButtonControl>("touchpadClicked");
            touchpadTouched = builder.GetControl<ButtonControl>("touchpadTouched");

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
}
