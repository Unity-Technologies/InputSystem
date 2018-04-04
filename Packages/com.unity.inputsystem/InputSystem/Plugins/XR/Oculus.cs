using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    internal static class OculusSupport
    {
        internal static string FilterLayout(XRDeviceDescriptor deviceDescriptor)
        {
            if (deviceDescriptor.manufacturer == "__Oculus__" || deviceDescriptor.manufacturer == "Oculus")
            {
                if ((deviceDescriptor.deviceName == "Oculus Rift" || String.IsNullOrEmpty(deviceDescriptor.deviceName)) && deviceDescriptor.deviceRole == EDeviceRole.Generic)
                {
                    return "OculusHMD";
                }
                else if (deviceDescriptor.deviceName.StartsWith("Oculus Touch Controller") && (deviceDescriptor.deviceRole == EDeviceRole.LeftHanded || deviceDescriptor.deviceRole == EDeviceRole.RightHanded))
                {
                    return "OculusTouchController";
                }
            }

            return null;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 309)]
    public struct OculusHMDState : IInputStateTypeInfo
    {
        [InputControl(layout = "Integer")]
        [FieldOffset(0)]
        public int trackingState;

        [InputControl(layout = "Button")]
        [FieldOffset(4)]
        public bool isTracked;

        [InputControl(layout = "Vector3")]
        [FieldOffset(5)]
        public Vector3 devicePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(17)]
        public Quaternion deviceRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(33)]
        public Vector3 deviceVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(45)]
        public Vector3 deviceAngularVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(57)]
        public Vector3 deviceAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(69)]
        public Vector3 deviceAngularAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(81)]
        public Vector3 leftEyePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(93)]
        public Quaternion leftEyeRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(109)]
        public Vector3 leftEyeVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(121)]
        public Vector3 leftEyeAngularVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(133)]
        public Vector3 leftEyeAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(145)]
        public Vector3 leftEyeAngularAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(157)]
        public Vector3 rightEyePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(169)]
        public Quaternion rightEyeRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(185)]
        public Vector3 rightEyeVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(197)]
        public Vector3 rightEyeAngularVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(209)]
        public Vector3 rightEyeAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(221)]
        public Vector3 rightEyeAngularAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(233)]
        public Vector3 centerEyePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(245)]
        public Quaternion centerEyeRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(261)]
        public Vector3 centerEyeVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(273)]
        public Vector3 centerEyeAngularVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(285)]
        public Vector3 centerEyeAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(297)]
        public Vector3 centerEyeAngularAcceleration;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputControlLayout(stateType = typeof(OculusHMDState))]
    public class OculusHMD : XRHMD
    {
        new public OculusHMD active { get; private set; }

        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control leftEyePosition { get; private set; }
        public QuaternionControl leftEyeRotation { get; private set; }
        public Vector3Control rightEyePosition { get; private set; }
        public QuaternionControl rightEyeRotation { get; private set; }
        public Vector3Control centerEyePosition { get; private set; }
        public QuaternionControl centerEyeRotation { get; private set; }


        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);
            active = this;

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            leftEyePosition = builder.GetControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = builder.GetControl<QuaternionControl>("leftEyeRotation");
            rightEyePosition = builder.GetControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = builder.GetControl<QuaternionControl>("rightEyeRotation");
            centerEyePosition = builder.GetControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = builder.GetControl<QuaternionControl>("centerEyeRotation");
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 118)]
    public struct OculusTouchControllerState : IInputStateTypeInfo
    {
        [InputControl(layout = "Analog")]
        [FieldOffset(0)]
        public float combinedTrigger;
        [InputControl(layout = "Vector2")]
        [FieldOffset(4)]
        public Vector2 joystick;

        [InputControl(layout = "Analog")]
        [FieldOffset(12)]
        public float trigger;
        [InputControl(layout = "Analog")]
        [FieldOffset(16)]
        public float grip;
        [InputControl(layout = "Analog")]
        [FieldOffset(20)]
        public float indexNearTouch;
        [InputControl(layout = "Analog")]
        [FieldOffset(24)]
        public float thumbNearTouch;

        [InputControl(layout = "Button", aliases = new[] { "a", "x"})]
        [FieldOffset(28)]
        public bool primaryButton;
        [InputControl(layout = "Button", aliases = new[] { "b", "y" })]
        [FieldOffset(29)]
        public bool secondaryButton;
        [InputControl(layout = "Button")]
        [FieldOffset(30)]
        public bool start;
        [InputControl(layout = "Button")]
        [FieldOffset(31)]
        public bool thumbstickClick;
        [InputControl(layout = "Button", aliases = new[] { "aTouch", "xTouch" })]
        [FieldOffset(32)]
        public bool primaryTouch;
        [InputControl(layout = "Button", aliases = new[] { "bTouch", "yTouch" })]
        [FieldOffset(33)]
        public bool secondaryTouch;
        [InputControl(layout = "Button")]
        [FieldOffset(34)]
        public bool indexTouch;
        [InputControl(layout = "Button")]
        [FieldOffset(35)]
        public bool thumbstickTouch;
        [InputControl(layout = "Button")]
        [FieldOffset(36)]
        public bool thumbrestTouch;

        [InputControl(layout = "Integer")]
        [FieldOffset(37)]
        public int trackingState;
        [InputControl(layout = "Button")]
        [FieldOffset(41)]
        public bool isTracked;
        [InputControl(layout = "Vector3")]
        [FieldOffset(42)]
        public Vector3 devicePosition;
        [InputControl(layout = "Quaternion")]
        [FieldOffset(54)]
        public Quaternion deviceRotation;
        [InputControl(layout = "Vector3")]
        [FieldOffset(70)]
        public Vector3 deviceVelocity;
        [InputControl(layout = "Vector3")]
        [FieldOffset(82)]
        public Vector3 deviceAngularVelocity;
        [InputControl(layout = "Vector3")]
        [FieldOffset(94)]
        public Vector3 deviceAcceleration;
        [InputControl(layout = "Vector3")]
        [FieldOffset(106)]
        public Vector3 deviceAngularAcceleration;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputControlLayout(stateType = typeof(OculusTouchControllerState), commonUsages = new[] { "LeftHand", "RightHand" })]
    public class OculusTouchController : XRControllerWithRumble
    {
        new public static OculusTouchController leftHand { get; private set; }
        new public static OculusTouchController rightHand { get; private set; }

        public AxisControl combinedTrigger { get; private set; }
        public Vector2Control joystick { get; private set; }

        public AxisControl trigger { get; private set; }
        public AxisControl grip { get; private set; }
        public AxisControl indexNearTouch { get; private set; }
        public AxisControl thumbNearTouch { get; private set; }

        public ButtonControl primaryButton { get; private set; }
        public ButtonControl secondaryButton { get; private set; }
        public ButtonControl start { get; private set; }
        public ButtonControl thumbstickClick { get; private set; }
        public ButtonControl primaryTouch { get; private set; }
        public ButtonControl secondaryTouch { get; private set; }
        public ButtonControl indexTouch { get; private set; }
        public ButtonControl thumbstickTouch { get; private set; }
        public ButtonControl thumbrestTouch { get; private set; }

        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceVelocity { get; private set; }
        public Vector3Control deviceAngularVelocity { get; private set; }
        public Vector3Control deviceAcceleration { get; private set; }
        public Vector3Control deviceAngularAcceleration { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            try
            {
                XRDeviceDescriptor deviceDescriptor = XRDeviceDescriptor.FromJson(description.capabilities);

                switch (deviceDescriptor.deviceRole)
                {
                    case EDeviceRole.LeftHanded:
                    {
                        InputSystem.SetUsage(this, CommonUsages.LeftHand);
                        leftHand = this;
                        break;
                    }
                    case EDeviceRole.RightHanded:
                    {
                        InputSystem.SetUsage(this, CommonUsages.RightHand);
                        rightHand = this;
                        break;
                    }
                    default:
                        break;
                }
            }
            catch (Exception)
            {}

            combinedTrigger = builder.GetControl<AxisControl>("combinedTrigger");
            joystick = builder.GetControl<Vector2Control>("joystick");
            trigger = builder.GetControl<AxisControl>("trigger");
            grip = builder.GetControl<AxisControl>("grip");
            indexNearTouch = builder.GetControl<AxisControl>("indexNearTouch");
            thumbNearTouch = builder.GetControl<AxisControl>("thumbNearTouch");

            primaryButton = builder.GetControl<ButtonControl>("primaryButton");
            secondaryButton = builder.GetControl<ButtonControl>("secondaryButton");
            start = builder.GetControl<ButtonControl>("start");
            thumbstickClick = builder.GetControl<ButtonControl>("thumbstickClick");
            primaryTouch = builder.GetControl<ButtonControl>("primaryTouch");
            secondaryTouch = builder.GetControl<ButtonControl>("secondaryTouch");
            indexTouch = builder.GetControl<ButtonControl>("indexTouch");
            thumbstickTouch = builder.GetControl<ButtonControl>("thumbstickTouch");
            thumbrestTouch = builder.GetControl<ButtonControl>("thumbrestTouch");

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = builder.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = builder.GetControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = builder.GetControl<Vector3Control>("deviceAngularAcceleration");
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 33)]
    public struct OculusTrackingReferenceState : IInputStateTypeInfo
    {
        [InputControl(layout = "Integer")]
        [FieldOffset(0)]
        public int trackingState;
        [InputControl(layout = "Button")]
        [FieldOffset(4)]
        public bool isTracked;
        [InputControl(layout = "Vector3")]
        [FieldOffset(5)]
        public Vector3 devicePosition;
        [InputControl(layout = "Quaternion")]
        [FieldOffset(17)]
        public Quaternion deviceRotation;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputControlLayout(stateType = typeof(OculusTouchControllerState))]
    public class OculusTrackingReference : InputDevice
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
}
