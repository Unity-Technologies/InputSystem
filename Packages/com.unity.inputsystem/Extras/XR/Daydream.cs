using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine;

namespace UnityEngine.Experimental.Input.XR
{
    internal static class DaydreamSupport
    {
        internal static string FilterTemplate(XRDeviceDescriptor deviceDescriptor)
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
        [InputControl(template = "Integer")]
        [FieldOffset(0)]
        public int trackingState;
        [InputControl(template = "Button")]
        [FieldOffset(4)]
        public bool isTracked;

        [InputControl(template = "Vector3")]
        [FieldOffset(8)]
        public Vector3 devicePosition;
        [InputControl(template = "Quaternion")]
        [FieldOffset(20)]
        public Quaternion deviceRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(36)]
        public Vector3 leftEyePosition;
        [InputControl(template = "Quaternion")]
        [FieldOffset(48)]
        public Quaternion leftEyeRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(64)]
        public Vector3 rightEyePosition;
        [InputControl(template = "Quaternion")]
        [FieldOffset(76)]
        public Quaternion rightEyeRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(92)]
        public Vector3 centerEyePosition;
        [InputControl(template = "Quaternion")]
        [FieldOffset(104)]
        public Quaternion centerEyeRotation;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputTemplate(stateType = typeof(DaydreamHMDState))]
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

    [StructLayout(LayoutKind.Explicit, Size = 100)]
    public struct DaydreamControllerState : IInputStateTypeInfo
    {
        [InputControl(template = "Vector2")]
        [FieldOffset(0)]
        public Vector2 touchpad;
        [InputControl(template = "Button")]
        [FieldOffset(8)]
        public bool volumeUp;
        [InputControl(template = "Button")]
        [FieldOffset(12)]
        public bool recentered;
        [InputControl(template = "Button")]
        [FieldOffset(16)]
        public bool volumeDown;
        [InputControl(template = "Button")]
        [FieldOffset(20)]
        public bool recentering;
        [InputControl(template = "Button")]
        [FieldOffset(24)]
        public bool app;
        [InputControl(template = "Button")]
        [FieldOffset(28)]
        public bool home;
        [InputControl(template = "Button")]
        [FieldOffset(32)]
        public bool touchpadClick;
        [InputControl(template = "Button")]
        [FieldOffset(36)]
        public bool touchpadTouch;


        [InputControl(template = "Integer")]
        [FieldOffset(40)]
        public int trackingState;

        [InputControl(template = "Button")]
        [FieldOffset(44)]
        public bool isTracked;

        [InputControl(template = "Vector3")]
        [FieldOffset(48)]
        public Vector3 devicePosition;

        [InputControl(template = "Quaternion")]
        [FieldOffset(60)]
        public Quaternion deviceRotation;

        [InputControl(template = "Vector3")]
        [FieldOffset(76)]
        public Vector3 deviceVelocity;

        [InputControl(template = "Vector3")]
        [FieldOffset(88)]
        public Vector3 deviceAcceleration;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputTemplate(stateType = typeof(DaydreamControllerState), commonUsages = new[] { "LeftHand", "RightHand" })]
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

            touchpad = setup.GetControl<Vector2Control>("touchpad");
            volumeUp = setup.GetControl<ButtonControl>("volumeUp");
            recentered = setup.GetControl<ButtonControl>("recentered");
            volumeDown = setup.GetControl<ButtonControl>("volumeDown");
            recentering = setup.GetControl<ButtonControl>("recentering");
            app = setup.GetControl<ButtonControl>("app");
            home = setup.GetControl<ButtonControl>("home");
            touchpadClick = setup.GetControl<ButtonControl>("touchpadClick");
            touchpadTouch = setup.GetControl<ButtonControl>("touchpadTouch");

            trackingState = setup.GetControl<IntegerControl>("trackingState");
            isTracked = setup.GetControl<ButtonControl>("isTracked");
            devicePosition = setup.GetControl<Vector3Control>("devicePosition");
            deviceRotation = setup.GetControl<QuaternionControl>("deviceRotation");
            deviceVelocity = setup.GetControl<Vector3Control>("deviceVelocity");
            deviceAcceleration = setup.GetControl<Vector3Control>("deviceAcceleration");
        }
    }
}