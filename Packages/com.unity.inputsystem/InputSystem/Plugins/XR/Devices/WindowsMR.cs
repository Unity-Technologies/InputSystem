// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
// Docs generation is skipped because these are intended to be replaced with the com.unity.xr.windowsmr package.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !DISABLE_BUILTIN_INPUT_SYSTEM_WINDOWSMR && !UNITY_FORCE_INPUTSYSTEM_XR_OFF && !PACKAGE_DOCS_GENERATION
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;

namespace UnityEngine.XR.WindowsMR.Input
{
    /// <summary>
    /// A Windows Mixed Reality XR headset.
    /// </summary>
    [InputControlLayout(displayName = "Windows MR Headset", hideInUI = true)]
    public class WMRHMD : XRHMD
    {
        [InputControl]
        [InputControl(name = "devicePosition", layout = "Vector3", aliases = new[] { "HeadPosition" })]
        [InputControl(name = "deviceRotation", layout = "Quaternion", aliases = new[] { "HeadRotation" })]
        public ButtonControl userPresence { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            userPresence = GetChildControl<ButtonControl>("userPresence");
        }
    }

    /// <summary>
    /// A Windows Mixed Reality XR controller.
    /// </summary>
    [InputControlLayout(displayName = "HoloLens Hand", commonUsages = new[] { "LeftHand", "RightHand" }, hideInUI = true)]
    public class HololensHand : XRController
    {
        [InputControl(noisy = true, aliases = new[] { "gripVelocity" })]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl(aliases = new[] { "triggerbutton" })]
        public ButtonControl airTap { get; private set; }
        [InputControl(noisy = true)]
        public AxisControl sourceLossRisk { get; private set; }
        [InputControl(noisy = true)]
        public Vector3Control sourceLossMitigationDirection { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            airTap = GetChildControl<ButtonControl>("airTap");
            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            sourceLossRisk = GetChildControl<AxisControl>("sourceLossRisk");
            sourceLossMitigationDirection = GetChildControl<Vector3Control>("sourceLossMitigationDirection");
        }
    }

    [InputControlLayout(displayName = "Windows MR Controller", commonUsages = new[] { "LeftHand", "RightHand" }, hideInUI = true)]
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
        [InputControl(noisy = true, aliases = new[] { "gripVelocity" })]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl(noisy = true, aliases = new[] { "gripAngularVelocity" })]
        public Vector3Control deviceAngularVelocity { get; private set; }

        [InputControl(noisy = true)]
        public AxisControl batteryLevel { get; private set; }
        [InputControl(noisy = true)]
        public AxisControl sourceLossRisk { get; private set; }
        [InputControl(noisy = true)]
        public Vector3Control sourceLossMitigationDirection { get; private set; }
        [InputControl(noisy = true)]
        public Vector3Control pointerPosition { get; private set; }
        [InputControl(noisy = true, aliases = new[] { "PointerOrientation" })]
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
#endif
