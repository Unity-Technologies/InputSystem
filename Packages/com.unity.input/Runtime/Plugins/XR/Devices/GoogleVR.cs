#if UNITY_XR_AVAILABLE && ENABLE_VR && !DISABLE_BUILTIN_INPUT_SYSTEM_GOOGLEVR && !PACKAGE_DOCS_GENERATION && !UNITY_FORCE_INPUTSYSTEM_XR_OFF
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;

namespace Unity.XR.GoogleVr
{
    /// <summary>
    /// A head-mounted display powered by Google Daydream.
    /// </summary>
    [InputControlLayout(displayName = "Daydream Headset")]
    public class DaydreamHMD : XRHMD
    {
    }

    /// <summary>
    /// An XR controller powered by Google Daydream.
    /// </summary>
    [InputControlLayout(displayName = "Daydream Controller", commonUsages = new[] { "LeftHand", "RightHand" })]
    public class DaydreamController : XRController
    {
        [InputControl]
        public Vector2Control touchpad { get; private set; }
        [InputControl]
        public ButtonControl volumeUp { get; private set; }
        [InputControl]
        public ButtonControl recentered { get; private set; }
        [InputControl]
        public ButtonControl volumeDown { get; private set; }
        [InputControl]
        public ButtonControl recentering { get; private set; }
        [InputControl]
        public ButtonControl app { get; private set; }
        [InputControl]
        public ButtonControl home { get; private set; }
        [InputControl]
        public ButtonControl touchpadClicked { get; private set; }
        [InputControl]
        public ButtonControl touchpadTouched { get; private set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl(noisy = true)]
        public Vector3Control deviceAcceleration { get; private set; }

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
