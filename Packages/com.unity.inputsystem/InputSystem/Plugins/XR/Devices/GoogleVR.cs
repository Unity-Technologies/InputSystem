// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
// Docs generation is skipped because these are intended to be replaced with the com.unity.xr.googlevr package.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !DISABLE_BUILTIN_INPUT_SYSTEM_GOOGLEVR && !UNITY_FORCE_INPUTSYSTEM_XR_OFF && !PACKAGE_DOCS_GENERATION
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;

namespace Unity.XR.GoogleVr
{
    /// <summary>
    /// A head-mounted display powered by Google Daydream.
    /// </summary>
    [InputControlLayout(displayName = "Daydream Headset", hideInUI = true)]
    public class DaydreamHMD : XRHMD
    {
    }

    /// <summary>
    /// An XR controller powered by Google Daydream.
    /// </summary>
    [InputControlLayout(displayName = "Daydream Controller", commonUsages = new[] { "LeftHand", "RightHand" }, hideInUI = true)]
    public class DaydreamController : XRController
    {
        [InputControl]
        public Vector2Control touchpad { get; protected set; }
        [InputControl]
        public ButtonControl volumeUp { get; protected set; }
        [InputControl]
        public ButtonControl recentered { get; protected set; }
        [InputControl]
        public ButtonControl volumeDown { get; protected set; }
        [InputControl]
        public ButtonControl recentering { get; protected set; }
        [InputControl]
        public ButtonControl app { get; protected set; }
        [InputControl]
        public ButtonControl home { get; protected set; }
        [InputControl]
        public ButtonControl touchpadClicked { get; protected set; }
        [InputControl]
        public ButtonControl touchpadTouched { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceVelocity { get; protected set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAcceleration { get; protected set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            touchpad = GetChildControl<Vector2Control>("touchpad");
            volumeUp = GetChildControl<ButtonControl>("volumeUp");
            recentered = GetChildControl<ButtonControl>("recentered");
            volumeDown = GetChildControl<ButtonControl>("volumeDown");
            recentering = GetChildControl<ButtonControl>("recentering");
            app = GetChildControl<ButtonControl>("app");
            home = GetChildControl<ButtonControl>("home");
            touchpadClicked = GetChildControl<ButtonControl>("touchpadClicked");
            touchpadTouched = GetChildControl<ButtonControl>("touchpadTouched");
            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
        }
    }
}
#endif
