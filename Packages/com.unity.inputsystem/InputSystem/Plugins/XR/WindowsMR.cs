using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    internal static class WMRSupport
    {
        internal static string FilterLayout(XRDeviceDescriptor deviceDescriptor)
        {
            if (deviceDescriptor.manufacturer == "Microsoft")
            {
                if (deviceDescriptor.deviceName == "Windows Mixed Reality HMD" && deviceDescriptor.deviceRole == EDeviceRole.Generic)
                {
                    return "WMRHMD";
                }
                else if (deviceDescriptor.deviceName == "Spatial Controller" && (deviceDescriptor.deviceRole == EDeviceRole.LeftHanded || deviceDescriptor.deviceRole == EDeviceRole.RightHanded))
                {
                    return "WMRSpatialController";
                }
            }

            return null;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 117)]
    public struct WMRHMDState : IInputStateTypeInfo
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
        public Vector3 deviceRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(33)]
        public Vector3 leftEyePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(45)]
        public Vector3 leftEyeRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(61)]
        public Vector3 rightEyePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(73)]
        public Vector3 rightEyeRotation;

        [InputControl(layout = "Vector3")]
        [FieldOffset(89)]
        public Vector3 centerEyePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(101)]
        public Vector3 centerEyeRotation;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputLayout(stateType = typeof(WMRHMDState))]
    public class WMRHMD : XRHMD
    {
        new public WMRHMD active { get; private set; }

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

    [StructLayout(LayoutKind.Explicit, Size = 67)]
    public struct WMRSpatialControllerState : IInputStateTypeInfo
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

        [InputControl(layout = "Vector2")]
        [FieldOffset(20)]
        public Vector2 touchpad;

        [InputControl(layout = "Button")]
        [FieldOffset(28)]
        public bool gripPressed;

        [InputControl(layout = "Button")]
        [FieldOffset(29)]
        public bool menu;

        [InputControl(layout = "Button")]
        [FieldOffset(30)]
        public bool joystickClick;

        [InputControl(layout = "Button")]
        [FieldOffset(31)]
        public bool triggerPressed;

        [InputControl(layout = "Button")]
        [FieldOffset(32)]
        public bool touchpadClick;

        [InputControl(layout = "Button")]
        [FieldOffset(33)]
        public bool touchpadTouch;

        [InputControl(layout = "Integer")]
        [FieldOffset(34)]
        public int trackingState;

        [InputControl(layout = "Button")]
        [FieldOffset(38)]
        public bool isTracked;

        [InputControl(layout = "Vector3")]
        [FieldOffset(39)]
        public Vector3 devicePosition;

        [InputControl(layout = "Quaternion")]
        [FieldOffset(51)]
        public Quaternion deviceRotation;

        public FourCC GetFormat()
        {
            return new FourCC('X', 'R', 'S', '0');
        }
    }

    [InputLayout(stateType = typeof(WMRSpatialControllerState), commonUsages = new[] { "LeftHand", "RightHand" })]
    public class WMRSpatialController : XRController
    {
        new public static WMRSpatialController leftHand { get; private set; }
        new public static WMRSpatialController rightHand { get; private set; }

        public AxisControl combinedTrigger { get; private set; }
        public Vector2Control joystick { get; private set; }
        public AxisControl trigger { get; private set; }
        public AxisControl grip { get; private set; }
        public Vector2Control touchpad { get; private set; }
        public ButtonControl gripPressed { get; private set; }
        public ButtonControl menu { get; private set; }
        public ButtonControl joystickClick { get; private set; }
        public ButtonControl triggerPressed { get; private set; }
        public ButtonControl touchpadClicked { get; private set; }
        public ButtonControl touchPadTouched { get; private set; }
        public IntegerControl trackingState { get; private set; }
        public ButtonControl isTracked { get; private set; }
        public Vector3Control devicePosition { get; private set; }
        public QuaternionControl deviceRotation { get; private set; }

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
            {}

            combinedTrigger = setup.GetControl<AxisControl>("combinedTrigger");
            joystick = setup.GetControl<Vector2Control>("joystick");
            trigger = setup.GetControl<AxisControl>("trigger");
            grip = setup.GetControl<AxisControl>("grip");
            touchpad = setup.GetControl<Vector2Control>("touchpad");
            gripPressed = setup.GetControl<ButtonControl>("gripPressed");
            menu = setup.GetControl<ButtonControl>("menu");
            joystickClick = setup.GetControl<ButtonControl>("joystickClick");
            triggerPressed = setup.GetControl<ButtonControl>("triggerPressed");
            touchpadClicked = setup.GetControl<ButtonControl>("touchpadClicked");
            touchPadTouched = setup.GetControl<ButtonControl>("touchPadTouched");
            trackingState = setup.GetControl<IntegerControl>("trackingState");
            isTracked = setup.GetControl<ButtonControl>("isTracked");
            devicePosition = setup.GetControl<Vector3Control>("devicePosition");
            deviceRotation = setup.GetControl<QuaternionControl>("deviceRotation");
        }
    }
}
