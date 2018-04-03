using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    internal static class WMRSupport
    {
        internal static string FilterTemplate(XRDeviceDescriptor deviceDescriptor)
        {
            if (deviceDescriptor.manufacturer == "Microsoft")
            {
                if (deviceDescriptor.deviceName == "Windows Mixed Reality HMD" && deviceDescriptor.deviceRole == DeviceRole.Generic)
                {
                    return "WMRHMD";
                }
                else if (deviceDescriptor.deviceName == "Spatial Controller" && (deviceDescriptor.deviceRole == DeviceRole.LeftHanded || deviceDescriptor.deviceRole == DeviceRole.RightHanded))
                {
                    return "WMRSpatialController";
                }
            }

            return null;
        }
    }

    [InputTemplate()]
    public class WMRHMD : XRHMD
    {
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

    [InputTemplate(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class WMRSpatialController : XRControllerWithRumble
    {
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
