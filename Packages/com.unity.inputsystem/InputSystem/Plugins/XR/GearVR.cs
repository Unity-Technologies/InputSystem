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

    [InputTemplate()]
    public class GearVRHMD : XRHMD
    {
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

    [InputTemplate(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class GearVRTrackedController : XRController
    {
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
