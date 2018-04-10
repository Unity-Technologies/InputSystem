using UnityEngine.Experimental.Input.Controls;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class KnucklesController : XRControllerWithRumble
    {
        public ButtonControl primary { get; private set; }
        public ButtonControl alternate { get; private set; }
        public AxisControl combinedTrigger { get; private set; }
        public AxisControl grip { get; private set; }
        public AxisControl index { get; private set; }
        public AxisControl middle { get; private set; }
        public AxisControl ring { get; private set; }
        public AxisControl pinky { get; private set; }
        public AxisControl trigger { get; private set; }
        public ButtonControl stickOrPadPress { get; private set; }
        public ButtonControl stickOrPadTouch { get; private set; }
        public Vector2Control trackpad { get; private set; }
        public AxisControl triggerTouch { get; private set; }

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
            stickOrPadPress = builder.GetControl<ButtonControl>("stickOrPadPress");
            stickOrPadTouch = builder.GetControl<ButtonControl>("stickOrPadTouch");
            trackpad = builder.GetControl<Vector2Control>("trackpad");
            triggerTouch = builder.GetControl<AxisControl>("triggerTouch");

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = builder.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");
        }
    }
}
