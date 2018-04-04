using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    internal static class DaydreamSupport
    {
        internal static string FilterLayout(XRDeviceDescriptor deviceDescriptor)
        {
            if (String.IsNullOrEmpty(deviceDescriptor.manufacturer))
            {
                if (deviceDescriptor.deviceName == "Daydream HMD" && deviceDescriptor.deviceRole == EDeviceRole.Generic)
                {
                    return "DaydreamHMD";
                }
                else if (deviceDescriptor.deviceName == "Daydream Controller" && (deviceDescriptor.deviceRole == EDeviceRole.LeftHanded || deviceDescriptor.deviceRole == EDeviceRole.RightHanded))
                {
                    return "DaydreamController";
                }
            }

            return null;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 120)]
    public struct DaydreamHMDState : IInputStateTypeInfo
    {
        [InputControl(layout = "Integer")]
        [FieldOffset(0)]
        public int trackingState;
        [InputControl(layout = "Button")]
        [FieldOffset(4)]
        public bool isTracked;

        [InputControl(layout = "Vector3")]
        [FieldOffset(8)]
        public Vector3 devicePosition;
        [InputControl(layout = "Quaternion")]
        [FieldOffset(20)]
        public Quaternion deviceRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(36)]
        public Vector3 leftEyePosition;
        [InputControl(layout = "Quaternion")]
        [FieldOffset(48)]
        public Quaternion leftEyeRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(64)]
        public Vector3 rightEyePosition;
        [InputControl(layout = "Quaternion")]
        [FieldOffset(76)]
        public Quaternion rightEyeRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(92)]
        public Vector3 centerEyePosition;
        [InputControl(layout = "Quaternion")]
        [FieldOffset(104)]
        public Quaternion centerEyeRotation;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputLayout(stateType = typeof(DaydreamHMDState))]
    public class DaydreamHMD : XRHMD
    {
        new public DaydreamHMD active { get; private set; }

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

    [StructLayout(LayoutKind.Explicit, Size = 100)]
    public struct DaydreamControllerState : IInputStateTypeInfo
    {
        [InputControl(layout = "Vector2")]
        [FieldOffset(0)]
        public Vector2 touchpad;
        [InputControl(layout = "Button")]
        [FieldOffset(8)]
        public bool volumeUp;
        [InputControl(layout = "Button")]
        [FieldOffset(12)]
        public bool recentered;
        [InputControl(layout = "Button")]
        [FieldOffset(16)]
        public bool volumeDown;
        [InputControl(layout = "Button")]
        [FieldOffset(20)]
        public bool recentering;
        [InputControl(layout = "Button")]
        [FieldOffset(24)]
        public bool app;
        [InputControl(layout = "Button")]
        [FieldOffset(28)]
        public bool home;
        [InputControl(layout = "Button")]
        [FieldOffset(32)]
        public bool touchpadClick;
        [InputControl(layout = "Button")]
        [FieldOffset(36)]
        public bool touchpadTouch;


        [InputControl(layout = "Integer")]
        [FieldOffset(40)]
        public int trackingState;

        [InputControl(layout = "Button")]
        [FieldOffset(44)]
        public bool isTracked;

        [InputControl(layout = "Vector3")]
        [FieldOffset(48)]
        public Vector3 devicePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(60)]
        public Quaternion deviceRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(76)]
        public Vector3 deviceVelocity;

        [InputControl(layout = "Vector3")]
        [FieldOffset(88)]
        public Vector3 deviceAcceleration;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputLayout(stateType = typeof(DaydreamControllerState), commonUsages = new[] { "LeftHand", "RightHand" })]
    public class DaydreamController : XRController
    {
        new public static DaydreamController leftHand { get; private set; }
        new public static DaydreamController rightHand { get; private set; }

        public Vector2Control touchpad { get; private set; }
        public ButtonControl volumeUp { get; private set; }
        public ButtonControl recentered { get; private set; }
        public ButtonControl volumeDown { get; private set; }
        public ButtonControl recentering { get; private set; }
        public ButtonControl app { get; private set; }
        public ButtonControl home { get; private set; }
        public ButtonControl touchpadClick { get; private set; }
        public ButtonControl touchpadTouch { get; private set; }

        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }
        public Vector3Control deviceVelocity { get; private set; }
        public Vector3Control deviceAcceleration { get; private set; }

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

            touchpad = builder.GetControl<Vector2Control>("touchpad");
            volumeUp = builder.GetControl<ButtonControl>("volumeUp");
            recentered = builder.GetControl<ButtonControl>("recentered");
            volumeDown = builder.GetControl<ButtonControl>("volumeDown");
            recentering = builder.GetControl<ButtonControl>("recentering");
            app = builder.GetControl<ButtonControl>("app");
            home = builder.GetControl<ButtonControl>("home");
            touchpadClick = builder.GetControl<ButtonControl>("touchpadClick");
            touchpadTouch = builder.GetControl<ButtonControl>("touchpadTouch");

            trackingState = builder.GetControl<IntegerControl>("trackingState");
            isTracked = builder.GetControl<ButtonControl>("isTracked");
            devicePosition = builder.GetControl<Vector3Control>("devicePosition");
            deviceRotation = builder.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = builder.GetControl<Vector3Control>("deviceVelocity");
            deviceAcceleration = builder.GetControl<Vector3Control>("deviceAcceleration");
        }
    }
}
