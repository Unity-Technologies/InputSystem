using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    [InputControlLayout]
    public class WMRHMD : XRHMD
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        [InputControl(aliases = new[] { "HeadPosition" })]
        public Vector3Control devicePosition { get; private set; }
        [InputControl(aliases = new[] { "HeadRotation" })]
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
    public class HololensHand : XRController
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        [InputControl(aliases = new[] { "gripPosition" })]
        public Vector3Control devicePosition { get; private set; }
        [InputControl(aliases = new[] { "gripOrientation" })]
        public QuaternionControl deviceRotation { get; private set; }
        [InputControl(aliases = new[] { "triggerbutton" })]
        public ButtonControl airTap { get; private set; }
        public AxisControl sourceLossRisk { get; private set; }
        public Vector3Control sourceLossMitigationDirection { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            airTap = builder.GetControl<ButtonControl>("airTap");
            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            sourceLossRisk = builder.GetControl<AxisControl>("sourceLossRisk");
            sourceLossMitigationDirection = builder.GetControl<Vector3Control>("sourceLossMitigationDirection");
        }
    }

    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class WMRSpatialController : XRControllerWithRumble
    {
        [InputControl(aliases = new[] { "Primary2DAxis", "thumbstickaxes" })]
        public Vector2Control joystick { get; private set; }
        [InputControl(aliases = new[] { "Secondary2DAxis", "touchpadaxes" })]
        public Vector2Control touchpad { get; private set; }
        [InputControl(aliases = new[] { "gripaxis" })]
        public AxisControl grip { get; private set; }
        [InputControl(aliases = new[] { "gripbutton" })]
        public ButtonControl gripPressed { get; private set; }
        [InputControl(aliases = new[] { "Primary", "menubutton" })]
        public ButtonControl menu { get; private set; }
        [InputControl(aliases = new[] { "triggeraxis" })]
        public AxisControl trigger { get; private set; }
        [InputControl(aliases = new[] { "triggerbutton" })]
        public ButtonControl triggerPressed { get; private set; }
        [InputControl(aliases = new[] { "thumbstickpressed" })]
        public ButtonControl joystickClicked { get; private set; }
        [InputControl(aliases = new[] { "joystickorpadpressed", "touchpadpressed" })]
        public ButtonControl touchpadClicked { get; private set; }
        [InputControl(aliases = new[] { "joystickorpadtouched", "touchpadtouched" })]
        public ButtonControl touchpadTouched { get; private set; }
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        [InputControl(aliases = new[] { "gripPosition" })]
        public Vector3Control devicePosition { get; private set; }
        [InputControl(aliases = new[] { "gripOrientation" })]
        public QuaternionControl deviceRotation { get; private set; }
        [InputControl(aliases = new[] { "gripVelocity" })]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl(aliases = new[] { "gripAngularVelocity" })]
        public Vector3Control deviceAngularVelocity { get; private set; }

        public AxisControl batteryLevel { get; private set; }
        public AxisControl sourceLossRisk { get; private set; }
        public Vector3Control sourceLossMitigationDirection { get; private set; }
        public Vector3Control pointerPosition { get; private set; }
        [InputControl(aliases = new[] { "PointerOrientation" })]
        public QuaternionControl pointerRotation { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            joystick = builder.GetControl<Vector2Control>("joystick");
            trigger = builder.GetControl<AxisControl>("trigger");
            touchpad = builder.GetControl<Vector2Control>("touchpad");
            grip = builder.GetControl<AxisControl>("grip");
            gripPressed = builder.GetControl<ButtonControl>("gripPressed");
            menu = builder.GetControl<ButtonControl>("menu");
            joystickClicked = builder.GetControl<ButtonControl>("joystickClicked");
            triggerPressed = builder.GetControl<ButtonControl>("triggerPressed");
            touchpadClicked = builder.GetControl<ButtonControl>("touchpadClicked");
            touchpadTouched = builder.GetControl<ButtonControl>("touchPadTouched");
            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = builder.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");

            batteryLevel = builder.GetControl<AxisControl>("batteryLevel");
            sourceLossRisk = builder.GetControl<AxisControl>("sourceLossRisk");
            sourceLossMitigationDirection = builder.GetControl<Vector3Control>("sourceLossMitigationDirection");
            pointerPosition = builder.GetControl<Vector3Control>("pointerPosition");
            pointerRotation = builder.GetControl<QuaternionControl>("pointerRotation");
        }
    }
}
