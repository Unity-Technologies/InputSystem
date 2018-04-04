using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    internal static class GearVRSupport
    {
        internal static string FilterLayout(XRDeviceDescriptor deviceDescriptor)
        {
            if (deviceDescriptor.manufacturer == "__Samsung__" || deviceDescriptor.manufacturer == "Samsung")
            {
                if (deviceDescriptor.deviceName == "Oculus HMD" && deviceDescriptor.deviceRole == EDeviceRole.Generic)
                {
                    return "GearVRHMD";
                }
                else if (deviceDescriptor.deviceName.StartsWith("Oculus Tracked Remote") && (deviceDescriptor.deviceRole == EDeviceRole.LeftHanded || deviceDescriptor.deviceRole == EDeviceRole.RightHanded))
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
        [InputControl(layout = "Vector2")]
        [FieldOffset(0)]
        public Vector2 touchpad;

        [InputControl(layout = "Integer")]
        [FieldOffset(8)]
        public int trackingState;

        [InputControl(layout = "Button")]
        [FieldOffset(12)]
        public bool isTracked;

        [InputControl(layout = "Vector3")]
        [FieldOffset(16)]
        public Vector3 devicePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(28)]
        public Quaternion deviceRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(44)]
        public Vector3 deviceVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(56)]
        public Vector3 deviceAngularVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(68)]
        public Vector3 deviceAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(80)]
        public Vector3 deviceAngularAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(92)]
        public Vector3 leftEyePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(104)]
        public Quaternion leftEyeRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(120)]
        public Vector3 leftEyeVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(132)]
        public Vector3 leftEyeAngularVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(144)]
        public Vector3 leftEyeAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(156)]
        public Vector3 leftEyeAngularAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(168)]
        public Vector3 rightEyePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(180)]
        public Quaternion rightEyeRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(196)]
        public Vector3 rightEyeVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(208)]
        public Vector3 rightEyeAngularVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(220)]
        public Vector3 rightEyeAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(232)]
        public Vector3 rightEyeAngularAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(244)]
        public Vector3 centerEyePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(256)]
        public Quaternion centerEyeRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(272)]
        public Vector3 centerEyeVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(284)]
        public Vector3 centerEyeAngularVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(296)]
        public Vector3 centerEyeAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(308)]
        public Vector3 centerEyeAngularAcceleration;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputControlLayout(stateType = typeof(GearVRHMDState))]
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

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);
            active = this;

            touchpad = builder.GetControl<Vector2Control>("touchpad");
            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = builder.GetControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = builder.GetControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = builder.GetControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = builder.GetControl<Vector3Control>("deviceAngularAcceleration");
            leftEyePosition = builder.GetControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = builder.GetControl<QuaternionControl>("leftEyeRotation");
            leftEyeVelocity = builder.GetControl<Vector3Control>("leftEyeVelocity");
            leftEyeAngularVelocity = builder.GetControl<Vector3Control>("leftEyeAngularVelocity");
            leftEyeAcceleration = builder.GetControl<Vector3Control>("leftEyeAcceleration");
            leftEyeAngularAcceleration = builder.GetControl<Vector3Control>("leftEyeAngularAcceleration");
            rightEyePosition = builder.GetControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = builder.GetControl<QuaternionControl>("rightEyeRotation");
            rightEyeVelocity = builder.GetControl<Vector3Control>("rightEyeVelocity");
            rightEyeAngularVelocity = builder.GetControl<Vector3Control>("rightEyeAngularVelocity");
            rightEyeAcceleration = builder.GetControl<Vector3Control>("rightEyeAcceleration");
            rightEyeAngularAcceleration = builder.GetControl<Vector3Control>("rightEyeAngularAcceleration");
            centerEyePosition = builder.GetControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = builder.GetControl<QuaternionControl>("centerEyeRotation");
            centerEyeVelocity = builder.GetControl<Vector3Control>("centerEyeVelocity");
            centerEyeAngularVelocity = builder.GetControl<Vector3Control>("centerEyeAngularVelocity");
            centerEyeAcceleration = builder.GetControl<Vector3Control>("centerEyeAcceleration");
            centerEyeAngularAcceleration = builder.GetControl<Vector3Control>("centerEyeAngularAcceleration");
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 112)]
    public struct GearVRTrackedControllerState : IInputStateTypeInfo
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

        [InputControl(layout = "Button")]
        [FieldOffset(16)]
        public bool back;

        [InputControl(layout = "Button")]
        [FieldOffset(20)]
        public bool touchpadClick;

        [InputControl(layout = "Button")]
        [FieldOffset(24)]
        public bool touchpadTouch;

        [InputControl(layout = "Integer")]
        [FieldOffset(28)]
        public int trackingState;

        [InputControl(layout = "Button")]
        [FieldOffset(32)]
        public bool isTracked;

        [InputControl(layout = "Vector3")]
        [FieldOffset(36)]
        public Vector3 devicePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(48)]
        public Quaternion deviceRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(64)]
        public Vector3 deviceVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(76)]
        public Vector3 deviceAngularVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(88)]
        public Vector3 deviceAcceleration;

        [InputControl(layout = "Vector3")]
        [FieldOffset(100)]
        public Vector3 deviceAngularAcceleration;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputControlLayout(stateType = typeof(GearVRTrackedControllerState), commonUsages = new[] { "LeftHand", "RightHand" })]
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
            back = builder.GetControl<ButtonControl>("back");
            touchpadClick = builder.GetControl<ButtonControl>("touchpadClick");
            touchpadTouch = builder.GetControl<ButtonControl>("touchpadTouch");

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
}
