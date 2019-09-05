using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// A Windows Mixed Reality XR headset.
    /// </summary>
    [InputControlLayout]
    [Scripting.Preserve]
    public class WMRHMD : XRHMD
    {
        [Scripting.Preserve]
        [InputControl]
        public IntegerControl trackingState { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public ButtonControl isTracked { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "HeadPosition" })]
        public Vector3Control devicePosition { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "HeadRotation" })]
        public QuaternionControl deviceRotation { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public Vector3Control leftEyePosition { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public QuaternionControl leftEyeRotation { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public Vector3Control rightEyePosition { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public QuaternionControl rightEyeRotation { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public Vector3Control centerEyePosition { get; private set; }
        [Scripting.Preserve]
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
    /// A Windows Mixed Reality XR controller.
    /// </summary>
    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    [Scripting.Preserve]
    public class HololensHand : XRController
    {
        [Scripting.Preserve]
        [InputControl]
        public IntegerControl trackingState { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public ButtonControl isTracked { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "gripPosition" })]
        public Vector3Control devicePosition { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "gripOrientation" })]
        public QuaternionControl deviceRotation { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "gripVelocity" })]
        public Vector3Control deviceVelocity { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "triggerbutton" })]
        public ButtonControl airTap { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public AxisControl sourceLossRisk { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public Vector3Control sourceLossMitigationDirection { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            airTap = GetChildControl<ButtonControl>("airTap");
            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            sourceLossRisk = GetChildControl<AxisControl>("sourceLossRisk");
            sourceLossMitigationDirection = GetChildControl<Vector3Control>("sourceLossMitigationDirection");
        }
    }

    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    [Scripting.Preserve]
    public class WMRSpatialController : XRControllerWithRumble
    {
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "Primary2DAxis", "thumbstickaxes" })]
        public Vector2Control joystick { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "Secondary2DAxis", "touchpadaxes" })]
        public Vector2Control touchpad { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "gripaxis" })]
        public AxisControl grip { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "gripbutton" })]
        public ButtonControl gripPressed { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "Primary", "menubutton" })]
        public ButtonControl menu { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "triggeraxis" })]
        public AxisControl trigger { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "triggerbutton" })]
        public ButtonControl triggerPressed { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "thumbstickpressed" })]
        public ButtonControl joystickClicked { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "joystickorpadpressed", "touchpadpressed" })]
        public ButtonControl touchpadClicked { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "joystickorpadtouched", "touchpadtouched" })]
        public ButtonControl touchpadTouched { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public IntegerControl trackingState { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public ButtonControl isTracked { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "gripPosition" })]
        public Vector3Control devicePosition { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "gripOrientation" })]
        public QuaternionControl deviceRotation { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "gripVelocity" })]
        public Vector3Control deviceVelocity { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "gripAngularVelocity" })]
        public Vector3Control deviceAngularVelocity { get; private set; }

        [Scripting.Preserve]
        [InputControl]
        public AxisControl batteryLevel { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public AxisControl sourceLossRisk { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public Vector3Control sourceLossMitigationDirection { get; private set; }
        [Scripting.Preserve]
        [InputControl]
        public Vector3Control pointerPosition { get; private set; }
        [Scripting.Preserve]
        [InputControl(aliases = new[] { "PointerOrientation" })]
        public QuaternionControl pointerRotation { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            joystick = GetChildControl<Vector2Control>("joystick");
            trigger = GetChildControl<AxisControl>("trigger");
            touchpad = GetChildControl<Vector2Control>("touchpad");
            grip = GetChildControl<AxisControl>("grip");
            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            menu = GetChildControl<ButtonControl>("menu");
            joystickClicked = GetChildControl<ButtonControl>("joystickClicked");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");
            touchpadClicked = GetChildControl<ButtonControl>("touchpadClicked");
            touchpadTouched = GetChildControl<ButtonControl>("touchPadTouched");
            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");

            batteryLevel = GetChildControl<AxisControl>("batteryLevel");
            sourceLossRisk = GetChildControl<AxisControl>("sourceLossRisk");
            sourceLossMitigationDirection = GetChildControl<Vector3Control>("sourceLossMitigationDirection");
            pointerPosition = GetChildControl<Vector3Control>("pointerPosition");
            pointerRotation = GetChildControl<QuaternionControl>("pointerRotation");
        }
    }
}
