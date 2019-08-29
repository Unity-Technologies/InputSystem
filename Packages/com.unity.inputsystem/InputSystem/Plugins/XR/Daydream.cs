using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// A head-mounted display powered by Google Daydream.
    /// </summary>
    [InputControlLayout]
    [Scripting.Preserve]
    public class DaydreamHMD : XRHMD
    {
        [InputControl]
        public IntegerControl trackingState { get; private set; }
        [InputControl]
        public ButtonControl isTracked { get; private set; }
        [InputControl]
        public Vector3Control devicePosition { get; private set; }
        [InputControl]
        public QuaternionControl deviceRotation { get; private set; }
        [InputControl]
        public Vector3Control leftEyePosition { get; private set; }
        [InputControl]
        public QuaternionControl leftEyeRotation { get; private set; }
        [InputControl]
        public Vector3Control rightEyePosition { get; private set; }
        [InputControl]
        public QuaternionControl rightEyeRotation { get; private set; }
        [InputControl]
        public Vector3Control centerEyePosition { get; private set; }
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
    /// An XR controller powered by Google Daydream.
    /// </summary>
    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    [Scripting.Preserve]
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

        [InputControl]
        public IntegerControl trackingState { get; private set; }
        [InputControl]
        public ButtonControl isTracked { get; private set; }
        [InputControl]
        public Vector3Control devicePosition { get; private set; }
        [InputControl]
        public QuaternionControl deviceRotation { get; private set; }
        [InputControl]
        public Vector3Control deviceVelocity { get; private set; }
        [InputControl]
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

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
        }
    }
}
