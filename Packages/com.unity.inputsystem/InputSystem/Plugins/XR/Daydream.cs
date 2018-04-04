using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    [InputTemplate()]
    public class DaydreamHMD : XRHMD
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
    public class DaydreamController : XRController
    {
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
