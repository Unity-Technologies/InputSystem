using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Plugins.XR
{
    [InputControlLayout]
    public class ViveHMD : XRHMD
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceVelocity { get; private set; }
        public Vector3Control deviceAngularVelocity { get; private set; }
        public Vector3Control leftEyePosition { get; private set; }
        public QuaternionControl leftEyeRotation { get; private set; }
        public Vector3Control leftEyeVelocity { get; private set; }
        public Vector3Control leftEyeAngularVelocity { get; private set; }
        public Vector3Control rightEyePosition { get; private set; }
        public QuaternionControl rightEyeRotation { get; private set; }
        public Vector3Control rightEyeVelocity { get; private set; }
        public Vector3Control rightEyeAngularVelocity { get; private set; }
        public Vector3Control centerEyePosition { get; private set; }
        public QuaternionControl centerEyeRotation { get; private set; }
        public Vector3Control centerEyeVelocity { get; private set; }
        public Vector3Control centerEyeAngularVelocity { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = builder.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");
            leftEyePosition = builder.GetControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = builder.GetControl<QuaternionControl>("leftEyeRotation");
            leftEyeVelocity = builder.GetControl<Vector3Control>("leftEyeVelocity");
            leftEyeAngularVelocity = builder.GetControl<Vector3Control>("leftEyeAngularVelocity");
            rightEyePosition = builder.GetControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = builder.GetControl<QuaternionControl>("rightEyeRotation");
            rightEyeVelocity = builder.GetControl<Vector3Control>("rightEyeVelocity");
            rightEyeAngularVelocity = builder.GetControl<Vector3Control>("rightEyeAngularVelocity");
            centerEyePosition = builder.GetControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = builder.GetControl<QuaternionControl>("centerEyeRotation");
            centerEyeVelocity = builder.GetControl<Vector3Control>("centerEyeVelocity");
            centerEyeAngularVelocity = builder.GetControl<Vector3Control>("centerEyeAngularVelocity");
        }
    }

    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class ViveWand : XRControllerWithRumble
    {
        public AxisControl grip { get; private set; }
        public ButtonControl gripPressed { get; private set; }
        public ButtonControl primary { get; private set; }
        [InputControl(aliases = new[] { "JoystickOrPadPressed" })]
        public ButtonControl trackpadPressed { get; private set; }
        [InputControl(aliases = new[] { "JoystickOrPadTouched" })]
        public ButtonControl trackpadTouched { get; private set; }
        [InputControl(aliases = new[] { "Primary2DAxis" })]
        public Vector2Control trackpad { get; private set; }
        public AxisControl trigger { get; private set; }
        public ButtonControl triggerPressed { get; private set; }

        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceVelocity { get; private set; }
        public Vector3Control deviceAngularVelocity { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            grip = builder.GetControl<AxisControl>("grip");
            primary = builder.GetControl<ButtonControl>("primary");
            gripPressed = builder.GetControl<ButtonControl>("gripPressed");
            trackpadPressed = builder.GetControl<ButtonControl>("trackpadPressed");
            trackpadTouched = builder.GetControl<ButtonControl>("trackpadTouched");
            trackpad = builder.GetControl<Vector2Control>("trackpad");
            trigger = builder.GetControl<AxisControl>("trigger");
            triggerPressed = builder.GetControl<ButtonControl>("triggerPressed");

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = builder.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");
        }
    }

    [InputControlLayout]
    public class KnucklesController : XRControllerWithRumble
    {
        [InputControl(aliases = new[] { "B",  "Primary"})]
        public ButtonControl primaryButton { get; private set; }

        [InputControl(aliases = new[] { "JoystickOrPadPressed" })]
        public ButtonControl trackpadPressed { get; private set; }
        [InputControl(aliases = new[] { "JoystickOrPadTouched" })]
        public ButtonControl trackpadTouched { get; private set; }
        [InputControl(aliases = new[] { "Primary2DAxis" })]
        public Vector2Control trackpad { get; private set; }

        public AxisControl grip { get; private set; }

        [InputControl(aliases = new[] { "A",  "GripButton" })]
        public ButtonControl gripPressed { get; private set; }


        public AxisControl trigger { get; private set; }
        public ButtonControl triggerPressed { get; private set; }

        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceVelocity { get; private set; }
        public Vector3Control deviceAngularVelocity { get; private set; }


        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            gripPressed = builder.GetControl<ButtonControl>("gripPressed");
            primaryButton = builder.GetControl<ButtonControl>("primary");
            trackpadPressed = builder.GetControl<ButtonControl>("trackpadPressed");
            trackpadTouched = builder.GetControl<ButtonControl>("trackpadTouched");
            trackpad = builder.GetControl<Vector2Control>("trackpad");
            trigger = builder.GetControl<AxisControl>("trigger");
            triggerPressed = builder.GetControl<ButtonControl>("triggerPressed");

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = builder.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");
        }
    }

    [InputControlLayout]
    public class ViveLighthouse : InputDevice
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
        }
    }

    public class ViveTracker : InputDevice
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceVelocity { get; private set; }
        public Vector3Control deviceAngularVelocity { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = builder.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");
        }
    }

    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class HandedViveTracker : ViveTracker
    {
        public AxisControl grip { get; private set; }
        public ButtonControl gripPressed { get; private set; }
        public ButtonControl primary { get; private set; }
        [InputControl(aliases = new[] { "JoystickOrPadPressed" })]
        public ButtonControl trackpadPressed { get; private set; }

        public ButtonControl triggerPressed { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            grip = builder.GetControl<AxisControl>("grip");
            primary = builder.GetControl<ButtonControl>("primary");
            gripPressed = builder.GetControl<ButtonControl>("gripPressed");
            trackpadPressed = builder.GetControl<ButtonControl>("trackpadPressed");
            triggerPressed = builder.GetControl<ButtonControl>("triggerPressed");

            base.FinishSetup(builder);
        }
    }
}
