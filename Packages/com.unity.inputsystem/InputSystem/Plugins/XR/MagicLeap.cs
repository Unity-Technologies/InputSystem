using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    [InputControlLayout]
    public class MagicLeapLightwear : XRHMD
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control centerEyePosition { get; private set; }
        public QuaternionControl centerEyeRotation { get; private set; }
        public AxisControl confidence { get; private set; }
        public AxisControl fixationPointConfidence { get; private set; }
        public AxisControl eyeLeftCenterConfidence { get; private set; }
        public AxisControl eyeRightCenterConfidence { get; private set; }

        //Need Discrete State for CalibrationStatus
        //Need Eyes type Control


        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            centerEyePosition = builder.GetControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = builder.GetControl<QuaternionControl>("centerEyeRotation");
            confidence = builder.GetControl<AxisControl>("confidence");
            fixationPointConfidence = builder.GetControl<AxisControl>("fixationPointConfidence");
            eyeLeftCenterConfidence = builder.GetControl<AxisControl>("eyeLeftCenterConfidence");
            eyeRightCenterConfidence = builder.GetControl<AxisControl>("eyeRightCenterConfidence");
        }
    }

    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class MagicLeapHand : XRController
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control center { get; private set; }
        public QuaternionControl rotation { get; private set; }

        public AxisControl handConfidence { get; private set; }
        public Vector3Control normalizeCenter { get; private set; }
        public Vector3Control wristCenter { get; private set; }
        public Vector3Control wristUlnar { get; private set; }
        public Vector3Control wristRadial { get; private set; }

        //Need Bone control and Hand Control

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            center = builder.GetControl<Vector3Control>("center");
            rotation = builder.GetControl<QuaternionControl>("rotation");

            handConfidence = builder.GetControl<AxisControl>("handConfidence");
            normalizeCenter = builder.GetControl<Vector3Control>("normalizeCenter");
            wristCenter = builder.GetControl<Vector3Control>("wristCenter");
            wristUlnar = builder.GetControl<Vector3Control>("wristUlnar");
            wristRadial = builder.GetControl<Vector3Control>("wristRadial");
        }
    }


    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class MagicLeapController : XRController
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control position { get; private set; }
        public QuaternionControl rotation { get; private set; }

        public ButtonControl touchpad1Pressed { get; private set; }
        public Vector2Control touchpad1Position { get; private set; }
        public AxisControl touchpad1Force { get; private set; }

        public ButtonControl touchpad2Pressed { get; private set; }
        public Vector2Control touchpad2Position { get; private set; }
        public AxisControl touchpad2Force { get; private set; }

        public ButtonControl triggerButton { get; private set; }
        public AxisControl trigger { get; private set; }
        public ButtonControl bumper { get; private set; }
        public ButtonControl menu { get; private set; }

        //Need Discrete State for DOF and Type and CalibrationAccuracy

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            position = builder.GetControl<Vector3Control>("devicePosition");
            rotation = builder.GetControl<QuaternionControl>("deviceRotation");

            touchpad1Pressed = builder.GetControl<ButtonControl>("touchpad1Pressed");
            touchpad1Position = builder.GetControl<Vector2Control>("touchpad1Position");
            touchpad1Force = builder.GetControl<AxisControl>("touchpad1Force");

            touchpad2Pressed = builder.GetControl<ButtonControl>("touchpad2Pressed");
            touchpad2Position = builder.GetControl<Vector2Control>("touchpad2Position");
            touchpad2Force = builder.GetControl<AxisControl>("touchpad2Force");

            triggerButton = builder.GetControl<ButtonControl>("triggerButton");
            trigger = builder.GetControl<AxisControl>("trigger");
            bumper = builder.GetControl<ButtonControl>("bumper");
            menu = builder.GetControl<ButtonControl>("menu");
        }
    }
}
