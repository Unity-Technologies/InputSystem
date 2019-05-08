using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Plugins.XR
{
    [InputControlLayout]
    public class OculusStandaloneHMDBase : XRHMD
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceAngularVelocity { get; private set; }
        public Vector3Control deviceAcceleration { get; private set; }
        public Vector3Control deviceAngularAcceleration { get; private set; }
        public Vector3Control leftEyePosition { get; private set; }
        public QuaternionControl leftEyeRotation { get; private set; }
        public Vector3Control leftEyeAngularVelocity { get; private set; }
        public Vector3Control leftEyeAcceleration { get; private set; }
        public Vector3Control leftEyeAngularAcceleration { get; private set; }
        public Vector3Control rightEyePosition { get; private set; }
        public QuaternionControl rightEyeRotation { get; private set; }
        public Vector3Control rightEyeAngularVelocity { get; private set; }
        public Vector3Control rightEyeAcceleration { get; private set; }
        public Vector3Control rightEyeAngularAcceleration { get; private set; }
        public Vector3Control centerEyePosition { get; private set; }
        public QuaternionControl centerEyeRotation { get; private set; }
        public Vector3Control centerEyeAngularVelocity { get; private set; }
        public Vector3Control centerEyeAcceleration { get; private set; }
        public Vector3Control centerEyeAngularAcceleration { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = builder.GetControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = builder.GetControl<Vector3Control>("deviceAngularAcceleration");
            leftEyePosition = builder.GetControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = builder.GetControl<QuaternionControl>("leftEyeRotation");
            leftEyeAngularVelocity = builder.GetControl<Vector3Control>("leftEyeAngularVelocity");
            leftEyeAcceleration = builder.GetControl<Vector3Control>("leftEyeAcceleration");
            leftEyeAngularAcceleration = builder.GetControl<Vector3Control>("leftEyeAngularAcceleration");
            rightEyePosition = builder.GetControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = builder.GetControl<QuaternionControl>("rightEyeRotation");
            rightEyeAngularVelocity = builder.GetControl<Vector3Control>("rightEyeAngularVelocity");
            rightEyeAcceleration = builder.GetControl<Vector3Control>("rightEyeAcceleration");
            rightEyeAngularAcceleration = builder.GetControl<Vector3Control>("rightEyeAngularAcceleration");
            centerEyePosition = builder.GetControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = builder.GetControl<QuaternionControl>("centerEyeRotation");
            centerEyeAngularVelocity = builder.GetControl<Vector3Control>("centerEyeAngularVelocity");
            centerEyeAcceleration = builder.GetControl<Vector3Control>("centerEyeAcceleration");
            centerEyeAngularAcceleration = builder.GetControl<Vector3Control>("centerEyeAngularAcceleration");
        }
    }

    public class OculusGo : OculusStandaloneHMDBase
    {}

    [InputControlLayout]
    public class OculusStandaloneHMDExtended : OculusStandaloneHMDBase
    {
        public ButtonControl back { get; private set; }
        public Vector2Control touchpad { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            back = builder.GetControl<ButtonControl>("back");
            touchpad = builder.GetControl<Vector2Control>("touchpad");
        }
    }

    public class GearVR : OculusStandaloneHMDExtended
    {}

    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class GearVRTrackedController : XRController
    {
        public Vector2Control touchpad { get; private set; }
        public AxisControl trigger { get; private set; }
        public ButtonControl back { get; private set; }
        public ButtonControl triggerPressed { get; private set; }
        public ButtonControl touchpadClicked { get; private set; }
        public ButtonControl touchpadTouched { get; private set; }
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceAngularVelocity { get; private set; }
        public Vector3Control deviceAcceleration { get; private set; }
        public Vector3Control deviceAngularAcceleration { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            touchpad = builder.GetControl<Vector2Control>("touchpad");
            trigger = builder.GetControl<AxisControl>("trigger");
            back = builder.GetControl<ButtonControl>("back");
            triggerPressed = builder.GetControl<ButtonControl>("triggerPressed");
            touchpadClicked = builder.GetControl<ButtonControl>("touchpadClicked");
            touchpadTouched = builder.GetControl<ButtonControl>("touchpadTouched");

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = builder.GetControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = builder.GetControl<Vector3Control>("deviceAngularAcceleration");
        }
    }
}
