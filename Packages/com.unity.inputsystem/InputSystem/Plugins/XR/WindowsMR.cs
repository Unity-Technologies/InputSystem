using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// A Windows Mixed Reality XR headset.
    /// </summary>
    [InputControlLayout]
    public class WMRHMD : XRHMD
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
    /// A Windows Mixed Reality XR controller.
    /// </summary>
    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class WMRSpatialController : XRControllerWithRumble
    {
        [InputControl(aliases = new[] { "Primary2DAxis" })]
        public Vector2Control joystick { get; private set; }
        public AxisControl trigger { get; private set; }
        [InputControl(aliases = new[] { "Secondary2DAxis" })]
        public Vector2Control touchpad { get; private set; }
        public AxisControl grip { get; private set; }
        public ButtonControl gripPressed { get; private set; }
        [InputControl(aliases = new[] { "Primary" })]
        public ButtonControl menu { get; private set; }
        public ButtonControl joystickClicked { get; private set; }
        public ButtonControl triggerPressed { get; private set; }
        [InputControl(aliases = new[] { "joystickorpadpressed" })]
        public ButtonControl touchpadClicked { get; private set; }
        [InputControl(aliases = new[] { "joystickorpadtouched" })]
        public ButtonControl touchpadTouched { get; private set; }
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceVelocity { get; private set; }
        public Vector3Control deviceAngularVelocity { get; private set; }

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
        }
    }
}
