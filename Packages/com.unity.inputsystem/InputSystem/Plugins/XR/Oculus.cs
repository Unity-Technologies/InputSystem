using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.XR.Haptics;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    internal static class OculusSupport
    {
        internal static string FilterTemplate(XRDeviceDescriptor deviceDescriptor)
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
        [InputControl(template = "Integer")]
        [FieldOffset(0)]
        public int trackingState;

        [InputControl(template = "Button")]
        [FieldOffset(4)]
        public bool isTracked;

        [InputControl(template = "Vector3")]
        [FieldOffset(5)]
        public Vector3 devicePosition;

        [InputControl(template = "Quaternion")]
        [FieldOffset(17)]
        public Quaternion deviceRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(33)]
        public Vector3 deviceVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(45)]
        public Vector3 deviceAngularVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(57)]
        public Vector3 deviceAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(69)]
        public Vector3 deviceAngularAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(81)]
        public Vector3 leftEyePosition;

        [InputControl(template = "Quaternion")]
        [FieldOffset(93)]
        public Quaternion leftEyeRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(109)]
        public Vector3 leftEyeVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(121)]
        public Vector3 leftEyeAngularVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(133)]
        public Vector3 leftEyeAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(145)]
        public Vector3 leftEyeAngularAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(157)]
        public Vector3 rightEyePosition;

        [InputControl(template = "Quaternion")]
        [FieldOffset(169)]
        public Quaternion rightEyeRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(185)]
        public Vector3 rightEyeVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(197)]
        public Vector3 rightEyeAngularVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(209)]
        public Vector3 rightEyeAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(221)]
        public Vector3 rightEyeAngularAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(233)]
        public Vector3 centerEyePosition;

        [InputControl(template = "Quaternion")]
        [FieldOffset(245)]
        public Quaternion centerEyeRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(261)]
        public Vector3 centerEyeVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(273)]
        public Vector3 centerEyeAngularVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(285)]
        public Vector3 centerEyeAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(297)]
        public Vector3 centerEyeAngularAcceleration;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputTemplate(stateType = typeof(OculusHMDState))]
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


        protected override void FinishSetup(InputControlSetup setup)
        {
            base.FinishSetup(setup);
            active = this;

            trackingState = setup.GetControl<IntegerControl>("trackingState");
            isTracked = setup.GetControl<ButtonControl>("isTracked");
            devicePosition = setup.GetControl<Vector3Control>("devicePosition");
            deviceRotation = setup.GetControl<QuaternionControl>("deviceRotation");
            leftEyePosition = setup.GetControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = setup.GetControl<QuaternionControl>("leftEyeRotation");
            rightEyePosition = setup.GetControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = setup.GetControl<QuaternionControl>("rightEyeRotation");
            centerEyePosition = setup.GetControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = setup.GetControl<QuaternionControl>("centerEyeRotation");
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 118)]
    public struct OculusTouchControllerState : IInputStateTypeInfo
    {
        [InputControl(template = "Analog")]
        [FieldOffset(0)]
        public float combinedTrigger;
        [InputControl(template = "Vector2")]
        [FieldOffset(4)]
        public Vector2 joystick;

        [InputControl(template = "Analog")]
        [FieldOffset(12)]
        public float trigger;
        [InputControl(template = "Analog")]
        [FieldOffset(16)]
        public float grip;
        [InputControl(template = "Analog")]
        [FieldOffset(20)]
        public float indexNearTouch;
        [InputControl(template = "Analog")]
        [FieldOffset(24)]
        public float thumbNearTouch;

        [InputControl(template = "Button", aliases = new[] { "a", "x"})]
        [FieldOffset(28)]
        public bool primaryButton;
        [InputControl(template = "Button", aliases = new[] { "b", "y" })]
        [FieldOffset(29)]
        public bool secondaryButton;
        [InputControl(template = "Button")]
        [FieldOffset(30)]
        public bool start;
        [InputControl(template = "Button")]
        [FieldOffset(31)]
        public bool thumbstickClick;
        [InputControl(template = "Button", aliases = new[] { "aTouch", "xTouch" })]
        [FieldOffset(32)]
        public bool primaryTouch;
        [InputControl(template = "Button", aliases = new[] { "bTouch", "yTouch" })]
        [FieldOffset(33)]
        public bool secondaryTouch;
        [InputControl(template = "Button")]
        [FieldOffset(34)]
        public bool indexTouch;
        [InputControl(template = "Button")]
        [FieldOffset(35)]
        public bool thumbstickTouch;
        [InputControl(template = "Button")]
        [FieldOffset(36)]
        public bool thumbrestTouch;

        [InputControl(template = "Integer")]
        [FieldOffset(37)]
        public int trackingState;
        [InputControl(template = "Button")]
        [FieldOffset(41)]
        public bool isTracked;
        [InputControl(template = "Vector3")]
        [FieldOffset(42)]
        public Vector3 devicePosition;
        [InputControl(template = "Quaternion")]
        [FieldOffset(54)]
        public Quaternion deviceRotation;
        [InputControl(template = "Vector3")]
        [FieldOffset(70)]
        public Vector3 deviceVelocity;
        [InputControl(template = "Vector3")]
        [FieldOffset(82)]
        public Vector3 deviceAngularVelocity;
        [InputControl(template = "Vector3")]
        [FieldOffset(94)]
        public Vector3 deviceAcceleration;
        [InputControl(template = "Vector3")]
        [FieldOffset(106)]
        public Vector3 deviceAngularAcceleration;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputTemplate(stateType = typeof(OculusTouchControllerState), commonUsages = new[] { "LeftHand", "RightHand" })]
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

        protected override void FinishSetup(InputControlSetup setup)
        {
            base.FinishSetup(setup);

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
            { }

            combinedTrigger = setup.GetControl<AxisControl>("combinedTrigger");
            joystick = setup.GetControl<Vector2Control>("joystick");
            trigger = setup.GetControl<AxisControl>("trigger");
            grip = setup.GetControl<AxisControl>("grip");
            indexNearTouch = setup.GetControl<AxisControl>("indexNearTouch");
            thumbNearTouch = setup.GetControl<AxisControl>("thumbNearTouch");

            primaryButton = setup.GetControl<ButtonControl>("primaryButton");
            secondaryButton = setup.GetControl<ButtonControl>("secondaryButton");
            start = setup.GetControl<ButtonControl>("start");
            thumbstickClick = setup.GetControl<ButtonControl>("thumbstickClick");
            primaryTouch = setup.GetControl<ButtonControl>("primaryTouch");
            secondaryTouch = setup.GetControl<ButtonControl>("secondaryTouch");
            indexTouch = setup.GetControl<ButtonControl>("indexTouch");
            thumbstickTouch = setup.GetControl<ButtonControl>("thumbstickTouch");
            thumbrestTouch = setup.GetControl<ButtonControl>("thumbrestTouch");

            trackingState = setup.GetControl<IntegerControl>("trackingState");
            isTracked = setup.GetControl<ButtonControl>("isTracked");
            devicePosition = setup.GetControl<Vector3Control>("devicePosition");
            deviceRotation = setup.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = setup.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = setup.GetControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = setup.GetControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = setup.GetControl<Vector3Control>("deviceAngularAcceleration");
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 33)]
    public struct OculusTrackingReferenceState : IInputStateTypeInfo
    {
        [InputControl(template = "Integer")]
        [FieldOffset(0)]
        public int trackingState;
        [InputControl(template = "Button")]
        [FieldOffset(4)]
        public bool isTracked;
        [InputControl(template = "Vector3")]
        [FieldOffset(5)]
        public Vector3 devicePosition;
        [InputControl(template = "Quaternion")]
        [FieldOffset(17)]
        public Quaternion deviceRotation;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputTemplate(stateType = typeof(OculusTouchControllerState))]
    public class OculusTrackingReference : InputDevice
    {
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            base.FinishSetup(setup);

            trackingState = setup.GetControl<IntegerControl>("trackingState");
            isTracked = setup.GetControl<ButtonControl>("isTracked");
            devicePosition = setup.GetControl<Vector3Control>("devicePosition");
            deviceRotation = setup.GetControl<QuaternionControl>("deviceRotation");
        }
    }
}