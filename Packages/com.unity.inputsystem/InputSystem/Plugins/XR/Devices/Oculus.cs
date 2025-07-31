// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
// Docs generation is skipped because these are intended to be replaced with the com.unity.xr.oculus package.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !DISABLE_BUILTIN_INPUT_SYSTEM_OCULUS && !UNITY_FORCE_INPUTSYSTEM_XR_OFF && !PACKAGE_DOCS_GENERATION
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;

namespace Unity.XR.Oculus.Input
{
    /// <summary>
    /// An Oculus VR headset (such as the Oculus Rift series of devices).
    /// </summary>
    [InputControlLayout(displayName = "Oculus Headset", hideInUI = true)]
    public class OculusHMD : XRHMD
    {
        [InputControl]
        [InputControl(name = "trackingState", layout = "Integer", aliases = new[] { "devicetrackingstate" })]
        [InputControl(name = "isTracked", layout = "Button", aliases = new[] { "deviceistracked" })]
        public ButtonControl userPresence { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAcceleration { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularAcceleration { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control leftEyeAngularVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control leftEyeAcceleration { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control leftEyeAngularAcceleration { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control rightEyeAngularVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control rightEyeAcceleration { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control rightEyeAngularAcceleration { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control centerEyeAngularVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control centerEyeAcceleration { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control centerEyeAngularAcceleration { get; protected set; }


        protected override void FinishSetup()
        {
            base.FinishSetup();

            userPresence = GetChildControl<ButtonControl>("userPresence");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = GetChildControl<Vector3Control>("deviceAngularAcceleration");
            leftEyeAngularVelocity = GetChildControl<Vector3Control>("leftEyeAngularVelocity");
            leftEyeAcceleration = GetChildControl<Vector3Control>("leftEyeAcceleration");
            leftEyeAngularAcceleration = GetChildControl<Vector3Control>("leftEyeAngularAcceleration");
            rightEyeAngularVelocity = GetChildControl<Vector3Control>("rightEyeAngularVelocity");
            rightEyeAcceleration = GetChildControl<Vector3Control>("rightEyeAcceleration");
            rightEyeAngularAcceleration = GetChildControl<Vector3Control>("rightEyeAngularAcceleration");
            centerEyeAngularVelocity = GetChildControl<Vector3Control>("centerEyeAngularVelocity");
            centerEyeAcceleration = GetChildControl<Vector3Control>("centerEyeAcceleration");
            centerEyeAngularAcceleration = GetChildControl<Vector3Control>("centerEyeAngularAcceleration");
        }
    }

    /// <summary>
    /// An Oculus Touch controller.
    /// </summary>
    [InputControlLayout(displayName = "Oculus Touch Controller", commonUsages = new[] { "LeftHand", "RightHand" }, hideInUI = true)]
    public class OculusTouchController : XRControllerWithRumble
    {
        [InputControl(aliases = new[] { "Primary2DAxis", "Joystick" })]
        public Vector2Control thumbstick { get; protected set; }

        [InputControl]
        public AxisControl trigger { get; protected set; }
        [InputControl]
        public AxisControl grip { get; protected set; }

        [InputControl(aliases = new[] { "A", "X", "Alternate" })]
        public ButtonControl primaryButton { get; protected set; }
        [InputControl(aliases = new[] { "B", "Y", "Primary" })]
        public ButtonControl secondaryButton { get; protected set; }
        [InputControl(aliases = new[] { "GripButton" })]
        public ButtonControl gripPressed { get; protected set; }
        [InputControl]
        public ButtonControl start { get; protected set; }
        [InputControl(aliases = new[] { "JoystickOrPadPressed", "thumbstickClick" })]
        public ButtonControl thumbstickClicked { get; protected set; }
        [InputControl(aliases = new[] { "ATouched", "XTouched", "ATouch", "XTouch" })]
        public ButtonControl primaryTouched { get; protected set; }
        [InputControl(aliases = new[] { "BTouched", "YTouched", "BTouch", "YTouch" })]
        public ButtonControl secondaryTouched { get; protected set; }
        [InputControl(aliases = new[] { "indexTouch", "indexNearTouched" })]
        public AxisControl triggerTouched { get; protected set; }
        [InputControl(aliases = new[] { "indexButton", "indexTouched" })]
        public ButtonControl triggerPressed { get; protected set; }
        [InputControl(aliases = new[] { "JoystickOrPadTouched", "thumbstickTouch" })]
        [InputControl(name = "trackingState", layout = "Integer", aliases = new[] { "controllerTrackingState" })]
        [InputControl(name = "isTracked", layout = "Button", aliases = new[] { "ControllerIsTracked" })]
        [InputControl(name = "devicePosition", layout = "Vector3", aliases = new[] { "controllerPosition" })]
        [InputControl(name = "deviceRotation", layout = "Quaternion", aliases = new[] { "controllerRotation" })]
        public ButtonControl thumbstickTouched { get; protected set; }
        [InputControl(noisy = true, aliases = new[] { "controllerVelocity" })]
        public Vector3Control deviceVelocity { get; protected set; }
        [InputControl(noisy = true, aliases = new[] { "controllerAngularVelocity" })]
        public Vector3Control deviceAngularVelocity { get; protected set; }
        [InputControl(noisy = true, aliases = new[] { "controllerAcceleration" })]
        public Vector3Control deviceAcceleration { get; protected set; }
        [InputControl(noisy = true, aliases = new[] { "controllerAngularAcceleration" })]
        public Vector3Control deviceAngularAcceleration { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            thumbstick = GetChildControl<Vector2Control>("thumbstick");
            trigger = GetChildControl<AxisControl>("trigger");
            triggerTouched = GetChildControl<AxisControl>("triggerTouched");
            grip = GetChildControl<AxisControl>("grip");

            primaryButton = GetChildControl<ButtonControl>("primaryButton");
            secondaryButton = GetChildControl<ButtonControl>("secondaryButton");
            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            start = GetChildControl<ButtonControl>("start");
            thumbstickClicked = GetChildControl<ButtonControl>("thumbstickClicked");
            primaryTouched = GetChildControl<ButtonControl>("primaryTouched");
            secondaryTouched = GetChildControl<ButtonControl>("secondaryTouched");
            thumbstickTouched = GetChildControl<ButtonControl>("thumbstickTouched");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");

            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = GetChildControl<Vector3Control>("deviceAngularAcceleration");
        }
    }

    public class OculusTrackingReference : TrackedDevice
    {
        [InputControl(aliases = new[] { "trackingReferenceTrackingState" })]
        public new IntegerControl trackingState { get; protected set; }
        [InputControl(aliases = new[] { "trackingReferenceIsTracked" })]
        public new ButtonControl isTracked { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
        }
    }

    /// <summary>
    /// An Oculus Remote controller.
    /// </summary>
    [InputControlLayout(displayName = "Oculus Remote", hideInUI = true)]
    public class OculusRemote : InputDevice
    {
        [InputControl]
        public ButtonControl back { get; protected set; }
        [InputControl]
        public ButtonControl start { get; protected set; }
        [InputControl]
        public Vector2Control touchpad { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            back = GetChildControl<ButtonControl>("back");
            start = GetChildControl<ButtonControl>("start");
            touchpad = GetChildControl<Vector2Control>("touchpad");
        }
    }

    /// <summary>
    /// A Standalone VR headset that includes on-headset controls.
    /// </summary>
    [InputControlLayout(displayName = "Oculus Headset (w/ on-headset controls)", hideInUI = true)]
    public class OculusHMDExtended : OculusHMD
    {
        [InputControl]
        public ButtonControl back { get; protected set; }
        [InputControl]
        public Vector2Control touchpad { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            back = GetChildControl<ButtonControl>("back");
            touchpad = GetChildControl<Vector2Control>("touchpad");
        }
    }

    /// <summary>
    /// A Gear VR controller.
    /// </summary>
    [InputControlLayout(displayName = "GearVR Controller", commonUsages = new[] { "LeftHand", "RightHand" }, hideInUI = true)]
    public class GearVRTrackedController : XRController
    {
        [InputControl]
        public Vector2Control touchpad { get; protected set; }
        [InputControl]
        public AxisControl trigger { get; protected set; }
        [InputControl]
        public ButtonControl back { get; protected set; }
        [InputControl]
        public ButtonControl triggerPressed { get; protected set; }
        [InputControl]
        public ButtonControl touchpadClicked { get; protected set; }
        [InputControl]
        public ButtonControl touchpadTouched { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAcceleration { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAngularAcceleration { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            touchpad = GetChildControl<Vector2Control>("touchpad");
            trigger = GetChildControl<AxisControl>("trigger");
            back = GetChildControl<ButtonControl>("back");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");
            touchpadClicked = GetChildControl<ButtonControl>("touchpadClicked");
            touchpadTouched = GetChildControl<ButtonControl>("touchpadTouched");

            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = GetChildControl<Vector3Control>("deviceAngularAcceleration");
        }
    }
}
#endif
