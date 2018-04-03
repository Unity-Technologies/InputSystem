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
                if ((deviceDescriptor.deviceName == "Oculus Rift" || String.IsNullOrEmpty(deviceDescriptor.deviceName)) && deviceDescriptor.deviceRole == DeviceRole.Generic)
                {
                    return "OculusHMD";
                }
                else if (deviceDescriptor.deviceName.StartsWith("Oculus Touch Controller") && (deviceDescriptor.deviceRole == DeviceRole.LeftHanded || deviceDescriptor.deviceRole == DeviceRole.RightHanded))
                {
                    return "OculusTouchController";
                }
            }

            return null;
        }
    }

    [InputTemplate()]
    public class OculusHMD : XRHMD
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
    public class OculusTouchController : XRControllerWithRumble
    {
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

    [InputTemplate()]
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
