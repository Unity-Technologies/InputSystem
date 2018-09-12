using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class KnucklesController : XRControllerWithRumble
    {
        public ButtonControl primary { get; private set; }
        public ButtonControl alternate { get; private set; }
        public AxisControl combinedTrigger { get; private set; }
        public AxisControl grip { get; private set; }
        public ButtonControl gripPressed { get; private set; }
        public AxisControl index { get; private set; }
        public AxisControl middle { get; private set; }
        public AxisControl ring { get; private set; }
        public AxisControl pinky { get; private set; }
        public AxisControl trigger { get; private set; }
        public ButtonControl joystickOrPadPressed { get; private set; }
        public ButtonControl joystickOrPadTouched { get; private set; }
        public Vector2Control trackpad { get; private set; }
        public AxisControl triggerPressed { get; private set; }

        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceVelocity { get; private set; }
        public Vector3Control deviceAngularVelocity { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            primary = builder.GetControl<ButtonControl>("primary");
            alternate = builder.GetControl<ButtonControl>("alternate");
            combinedTrigger = builder.GetControl<AxisControl>("combinedTrigger");
            grip = builder.GetControl<AxisControl>("grip");
            index = builder.GetControl<AxisControl>("index");
            middle = builder.GetControl<AxisControl>("middle");
            ring = builder.GetControl<AxisControl>("ring");
            pinky = builder.GetControl<AxisControl>("pinky");
            trigger = builder.GetControl<AxisControl>("trigger");
            gripPressed = builder.GetControl<ButtonControl>("gripPressed");
            joystickOrPadPressed = builder.GetControl<ButtonControl>("joystickOrPadPressed");
            joystickOrPadTouched = builder.GetControl<ButtonControl>("joystickOrPadTouched");
            trackpad = builder.GetControl<Vector2Control>("trackpad");
            triggerPressed = builder.GetControl<AxisControl>("triggerPressed");

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = builder.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");
        }
    }
}
