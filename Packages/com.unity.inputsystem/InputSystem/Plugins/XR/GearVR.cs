using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// Base class for standalone VR headsets powered by Oculus VR.
    /// </summary>
    [InputControlLayout]
    [Scripting.Preserve]
    public class OculusStandaloneHMDBase : XRHMD
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
        public Vector3Control deviceAngularVelocity { get; private set; }
        [InputControl]
        public Vector3Control deviceAcceleration { get; private set; }
        [InputControl]
        public Vector3Control deviceAngularAcceleration { get; private set; }
        [InputControl]
        public Vector3Control leftEyePosition { get; private set; }
        [InputControl]
        public QuaternionControl leftEyeRotation { get; private set; }
        [InputControl]
        public Vector3Control leftEyeAngularVelocity { get; private set; }
        [InputControl]
        public Vector3Control leftEyeAcceleration { get; private set; }
        [InputControl]
        public Vector3Control leftEyeAngularAcceleration { get; private set; }
        [InputControl]
        public Vector3Control rightEyePosition { get; private set; }
        [InputControl]
        public QuaternionControl rightEyeRotation { get; private set; }
        [InputControl]
        public Vector3Control rightEyeAngularVelocity { get; private set; }
        [InputControl]
        public Vector3Control rightEyeAcceleration { get; private set; }
        [InputControl]
        public Vector3Control rightEyeAngularAcceleration { get; private set; }
        [InputControl]
        public Vector3Control centerEyePosition { get; private set; }
        [InputControl]
        public QuaternionControl centerEyeRotation { get; private set; }
        [InputControl]
        public Vector3Control centerEyeAngularVelocity { get; private set; }
        [InputControl]
        public Vector3Control centerEyeAcceleration { get; private set; }
        [InputControl]
        public Vector3Control centerEyeAngularAcceleration { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = GetChildControl<Vector3Control>("deviceAngularAcceleration");
            leftEyePosition = GetChildControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = GetChildControl<QuaternionControl>("leftEyeRotation");
            leftEyeAngularVelocity = GetChildControl<Vector3Control>("leftEyeAngularVelocity");
            leftEyeAcceleration = GetChildControl<Vector3Control>("leftEyeAcceleration");
            leftEyeAngularAcceleration = GetChildControl<Vector3Control>("leftEyeAngularAcceleration");
            rightEyePosition = GetChildControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = GetChildControl<QuaternionControl>("rightEyeRotation");
            rightEyeAngularVelocity = GetChildControl<Vector3Control>("rightEyeAngularVelocity");
            rightEyeAcceleration = GetChildControl<Vector3Control>("rightEyeAcceleration");
            rightEyeAngularAcceleration = GetChildControl<Vector3Control>("rightEyeAngularAcceleration");
            centerEyePosition = GetChildControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = GetChildControl<QuaternionControl>("centerEyeRotation");
            centerEyeAngularVelocity = GetChildControl<Vector3Control>("centerEyeAngularVelocity");
            centerEyeAcceleration = GetChildControl<Vector3Control>("centerEyeAcceleration");
            centerEyeAngularAcceleration = GetChildControl<Vector3Control>("centerEyeAngularAcceleration");
        }
    }

    /// <summary>
    /// An Oculus Go headset.
    /// </summary>
    [Scripting.Preserve]
    public class OculusGo : OculusStandaloneHMDBase
    {}

    /// <summary>
    /// A standalone VR headset powered by Oculus VR.
    /// </summary>
    [InputControlLayout]
    [Scripting.Preserve]
    public class OculusStandaloneHMDExtended : OculusStandaloneHMDBase
    {
        [InputControl]
        public ButtonControl back { get; private set; }
        [InputControl]
        public Vector2Control touchpad { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            back = GetChildControl<ButtonControl>("back");
            touchpad = GetChildControl<Vector2Control>("touchpad");
        }
    }

    /// <summary>
    /// A Gear VR headset.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1501:AvoidExcessiveInheritance")]
    [Scripting.Preserve]
    public class GearVR : OculusStandaloneHMDExtended
    {}

    /// <summary>
    /// A Gear VR controller.
    /// </summary>
    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    [Scripting.Preserve]
    public class GearVRTrackedController : XRController
    {
        [InputControl]
        public Vector2Control touchpad { get; private set; }
        [InputControl]
        public AxisControl trigger { get; private set; }
        [InputControl]
        public ButtonControl back { get; private set; }
        [InputControl]
        public ButtonControl triggerPressed { get; private set; }
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
        public Vector3Control deviceAngularVelocity { get; private set; }
        [InputControl]
        public Vector3Control deviceAcceleration { get; private set; }
        [InputControl]
        public Vector3Control deviceAngularAcceleration { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            touchpad = GetChildControl<Vector2Control>("touchpad");
            trigger = GetChildControl<AxisControl>("trigger");
            back = GetChildControl<ButtonControl>("back");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");
            touchpadClicked = GetChildControl<ButtonControl>("touchpadClicked");
            touchpadTouched = GetChildControl<ButtonControl>("touchpadTouched");

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = GetChildControl<Vector3Control>("deviceAngularAcceleration");
        }
    }
}
