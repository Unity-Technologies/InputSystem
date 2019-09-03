using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.XR
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


        protected override void FinishSetup()
        {
            base.FinishSetup();

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            centerEyePosition = GetChildControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = GetChildControl<QuaternionControl>("centerEyeRotation");
            confidence = GetChildControl<AxisControl>("confidence");
            fixationPointConfidence = GetChildControl<AxisControl>("fixationPointConfidence");
            eyeLeftCenterConfidence = GetChildControl<AxisControl>("eyeLeftCenterConfidence");
            eyeRightCenterConfidence = GetChildControl<AxisControl>("eyeRightCenterConfidence");
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

        protected override void FinishSetup()
        {
            base.FinishSetup();

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            center = GetChildControl<Vector3Control>("center");
            rotation = GetChildControl<QuaternionControl>("rotation");

            handConfidence = GetChildControl<AxisControl>("handConfidence");
            normalizeCenter = GetChildControl<Vector3Control>("normalizeCenter");
            wristCenter = GetChildControl<Vector3Control>("wristCenter");
            wristUlnar = GetChildControl<Vector3Control>("wristUlnar");
            wristRadial = GetChildControl<Vector3Control>("wristRadial");
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

        protected override void FinishSetup()
        {
            base.FinishSetup();

            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            position = GetChildControl<Vector3Control>("devicePosition");
            rotation = GetChildControl<QuaternionControl>("deviceRotation");

            touchpad1Pressed = GetChildControl<ButtonControl>("touchpad1Pressed");
            touchpad1Position = GetChildControl<Vector2Control>("touchpad1Position");
            touchpad1Force = GetChildControl<AxisControl>("touchpad1Force");

            touchpad2Pressed = GetChildControl<ButtonControl>("touchpad2Pressed");
            touchpad2Position = GetChildControl<Vector2Control>("touchpad2Position");
            touchpad2Force = GetChildControl<AxisControl>("touchpad2Force");

            triggerButton = GetChildControl<ButtonControl>("triggerButton");
            trigger = GetChildControl<AxisControl>("trigger");
            bumper = GetChildControl<ButtonControl>("bumper");
            menu = GetChildControl<ButtonControl>("menu");
        }
    }
}
