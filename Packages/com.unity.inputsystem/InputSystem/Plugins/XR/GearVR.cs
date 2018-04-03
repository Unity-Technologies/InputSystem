using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    internal static class GearVRSupport
    {
        internal static string FilterTemplate(XRDeviceDescriptor deviceDescriptor)
        {
            if (deviceDescriptor.manufacturer == "__Samsung__" || deviceDescriptor.manufacturer == "Samsung")
            {
                if (deviceDescriptor.deviceName == "Oculus HMD" && deviceDescriptor.deviceRole == DeviceRole.Generic)
                {
                    return "GearVRHMD";
                }
                else if (deviceDescriptor.deviceName.StartsWith("Oculus Tracked Remote") && (deviceDescriptor.deviceRole == DeviceRole.LeftHanded || deviceDescriptor.deviceRole == DeviceRole.RightHanded))
                {
                    return "GearVRTrackedController";
                }
            }

            return null;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 320)]
    public struct GearVRHMDState : IInputStateTypeInfo
    {
        [InputControl(template = "Vector2")]
        [FieldOffset(0)]
        public Vector2 touchpad;

        [InputControl(template = "Integer")]
        [FieldOffset(8)]
        public int trackingState;

        [InputControl(template = "Button")]
        [FieldOffset(12)]
        public bool isTracked;

        [InputControl(template = "Vector3")]
        [FieldOffset(16)]
        public Vector3 devicePosition;

        [InputControl(template = "Quaternion")]
        [FieldOffset(28)]
        public Quaternion deviceRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(44)]
        public Vector3 deviceVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(56)]
        public Vector3 deviceAngularVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(68)]
        public Vector3 deviceAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(80)]
        public Vector3 deviceAngularAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(92)]
        public Vector3 leftEyePosition;

        [InputControl(template = "Quaternion")]
        [FieldOffset(104)]
        public Quaternion leftEyeRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(120)]
        public Vector3 leftEyeVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(132)]
        public Vector3 leftEyeAngularVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(144)]
        public Vector3 leftEyeAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(156)]
        public Vector3 leftEyeAngularAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(168)]
        public Vector3 rightEyePosition;

        [InputControl(template = "Quaternion")]
        [FieldOffset(180)]
        public Quaternion rightEyeRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(196)]
        public Vector3 rightEyeVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(208)]
        public Vector3 rightEyeAngularVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(220)]
        public Vector3 rightEyeAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(232)]
        public Vector3 rightEyeAngularAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(244)]
        public Vector3 centerEyePosition;

        [InputControl(template = "Quaternion")]
        [FieldOffset(256)]
        public Quaternion centerEyeRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(272)]
        public Vector3 centerEyeVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(284)]
        public Vector3 centerEyeAngularVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(296)]
        public Vector3 centerEyeAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(308)]
        public Vector3 centerEyeAngularAcceleration;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputTemplate(stateType = typeof(GearVRHMDState))]
    public class GearVRHMD : XRHMD
    {
        new public GearVRHMD active { get; private set; }

        public Vector2Control touchpad { get; private set; }
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceVelocity { get; private set; }
        public Vector3Control deviceAngularVelocity { get; private set; }
        public Vector3Control deviceAcceleration { get; private set; }
        public Vector3Control deviceAngularAcceleration { get; private set; }
        public Vector3Control leftEyePosition { get; private set; }
        public QuaternionControl leftEyeRotation { get; private set; }
        public Vector3Control leftEyeVelocity { get; private set; }
        public Vector3Control leftEyeAngularVelocity { get; private set; }
        public Vector3Control leftEyeAcceleration { get; private set; }
        public Vector3Control leftEyeAngularAcceleration { get; private set; }
        public Vector3Control rightEyePosition { get; private set; }
        public QuaternionControl rightEyeRotation { get; private set; }
        public Vector3Control rightEyeVelocity { get; private set; }
        public Vector3Control rightEyeAngularVelocity { get; private set; }
        public Vector3Control rightEyeAcceleration { get; private set; }
        public Vector3Control rightEyeAngularAcceleration { get; private set; }
        public Vector3Control centerEyePosition { get; private set; }
        public QuaternionControl centerEyeRotation { get; private set; }
        public Vector3Control centerEyeVelocity { get; private set; }
        public Vector3Control centerEyeAngularVelocity { get; private set; }
        public Vector3Control centerEyeAcceleration { get; private set; }
        public Vector3Control centerEyeAngularAcceleration { get; private set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            base.FinishSetup(setup);
            active = this;

            touchpad = setup.GetControl<Vector2Control>("touchpad");
            trackingState = setup.GetControl<IntegerControl>("trackingState");
            isTracked = setup.GetControl<ButtonControl>("isTracked");
            devicePosition = setup.GetControl<Vector3Control>("devicePosition");
            deviceRotation = setup.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = setup.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = setup.GetControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = setup.GetControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = setup.GetControl<Vector3Control>("deviceAngularAcceleration");
            leftEyePosition = setup.GetControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = setup.GetControl<QuaternionControl>("leftEyeRotation");
            leftEyeVelocity = setup.GetControl<Vector3Control>("leftEyeVelocity");
            leftEyeAngularVelocity = setup.GetControl<Vector3Control>("leftEyeAngularVelocity");
            leftEyeAcceleration = setup.GetControl<Vector3Control>("leftEyeAcceleration");
            leftEyeAngularAcceleration = setup.GetControl<Vector3Control>("leftEyeAngularAcceleration");
            rightEyePosition = setup.GetControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = setup.GetControl<QuaternionControl>("rightEyeRotation");
            rightEyeVelocity = setup.GetControl<Vector3Control>("rightEyeVelocity");
            rightEyeAngularVelocity = setup.GetControl<Vector3Control>("rightEyeAngularVelocity");
            rightEyeAcceleration = setup.GetControl<Vector3Control>("rightEyeAcceleration");
            rightEyeAngularAcceleration = setup.GetControl<Vector3Control>("rightEyeAngularAcceleration");
            centerEyePosition = setup.GetControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = setup.GetControl<QuaternionControl>("centerEyeRotation");
            centerEyeVelocity = setup.GetControl<Vector3Control>("centerEyeVelocity");
            centerEyeAngularVelocity = setup.GetControl<Vector3Control>("centerEyeAngularVelocity");
            centerEyeAcceleration = setup.GetControl<Vector3Control>("centerEyeAcceleration");
            centerEyeAngularAcceleration = setup.GetControl<Vector3Control>("centerEyeAngularAcceleration");
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 112)]
    public struct GearVRTrackedControllerState : IInputStateTypeInfo
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

        [InputControl(template = "Button")]
        [FieldOffset(16)]
        public bool back;

        [InputControl(template = "Button")]
        [FieldOffset(20)]
        public bool touchpadClick;

        [InputControl(template = "Button")]
        [FieldOffset(24)]
        public bool touchpadTouch;

        [InputControl(template = "Integer")]
        [FieldOffset(28)]
        public int trackingState;

        [InputControl(template = "Button")]
        [FieldOffset(32)]
        public bool isTracked;

        [InputControl(template = "Vector3")]
        [FieldOffset(36)]
        public Vector3 devicePosition;

        [InputControl(template = "Quaternion")]
        [FieldOffset(48)]
        public Quaternion deviceRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(64)]
        public Vector3 deviceVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(76)]
        public Vector3 deviceAngularVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(88)]
        public Vector3 deviceAcceleration;

        [InputControl(template = "Vector3")]
        [FieldOffset(100)]
        public Vector3 deviceAngularAcceleration;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputTemplate(stateType = typeof(GearVRTrackedControllerState), commonUsages = new[] { "LeftHand", "RightHand" })]
    public class GearVRTrackedController : XRController
    {
        new public static GearVRTrackedController leftHand { get; private set; }
        new public static GearVRTrackedController rightHand { get; private set; }

        public AxisControl combinedTrigger { get; private set; }
        public Vector2Control joystick { get; private set; }
        public AxisControl trigger { get; private set; }
        public ButtonControl back { get; private set; }
        public ButtonControl touchpadClick { get; private set; }
        public ButtonControl touchpadTouch { get; private set; }
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

            var deviceDescriptor = XRDeviceDescriptor.FromJson(description.capabilities);

            switch (deviceDescriptor.deviceRole)
            {
                case DeviceRole.LeftHanded:
                {
                    InputSystem.SetUsage(this, CommonUsages.LeftHand);
                    leftHand = this;
                    break;
                }
                case DeviceRole.RightHanded:
                {
                    InputSystem.SetUsage(this, CommonUsages.RightHand);
                    rightHand = this;
                    break;
                }
                default:
                    break;
            }

            combinedTrigger = setup.GetControl<AxisControl>("combinedTrigger");
            joystick = setup.GetControl<Vector2Control>("joystick");
            trigger = setup.GetControl<AxisControl>("trigger");
            back = setup.GetControl<ButtonControl>("back");
            touchpadClick = setup.GetControl<ButtonControl>("touchpadClick");
            touchpadTouch = setup.GetControl<ButtonControl>("touchpadTouch");

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
}
