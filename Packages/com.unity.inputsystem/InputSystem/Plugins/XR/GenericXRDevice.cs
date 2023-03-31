// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !UNITY_FORCE_INPUTSYSTEM_XR_OFF || PACKAGE_DOCS_GENERATION
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR.Haptics;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.XR;

namespace UnityEngine.InputSystem.XR
{
    [InputControlLayout(isGenericTypeOfDevice = true, displayName = "XR HMD", canRunInBackground = true)]
    public class XRHMD : TrackedDevice
    {
        /// <summary>
        /// The base type of all XR head mounted displays.  This can help organize shared behaviour across all HMDs.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// To give your head tracking an extra update before rendering:
        /// First, enable before render updates on your Device.
        ///
        /// <sample>
        /// <code>
        ///     // JSON
        ///     {
        ///         "name" : "MyHMD",
        ///         "extend" : "HMD",
        ///         "beforeRender" : "Update"
        ///     }
        /// </code>
        /// </sample>
        ///
        /// Then, make sure you put extra `StateEvents` for your HMD on the queue right in time before rendering. Also, if your HMD is a combination of non-tracking and tracking controls, you can update just the tracking by sending a delta event instead of a full state event.
        ///
        /// </remarks>

        [InputControl(noisy = true)]
        public Vector3Control leftEyePosition { get; private set; }
        [InputControl(noisy = true)]
        public QuaternionControl leftEyeRotation { get; private set; }
        [InputControl(noisy = true)]
        public Vector3Control rightEyePosition { get; private set; }
        [InputControl(noisy = true)]
        public QuaternionControl rightEyeRotation { get; private set; }
        [InputControl(noisy = true)]
        public Vector3Control centerEyePosition { get; private set; }
        [InputControl(noisy = true)]
        public QuaternionControl centerEyeRotation { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            centerEyePosition = GetChildControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = GetChildControl<QuaternionControl>("centerEyeRotation");
            leftEyePosition = GetChildControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = GetChildControl<QuaternionControl>("leftEyeRotation");
            rightEyePosition = GetChildControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = GetChildControl<QuaternionControl>("rightEyeRotation");
        }
    }

    /// <summary>
    /// The base type for all XR handed controllers.
    /// </summary>
    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" }, isGenericTypeOfDevice = true, displayName = "XR Controller")]
    public class XRController : TrackedDevice
    {
        /// <summary>
        /// A quick accessor for the currently active left handed device.
        /// </summary>
        /// <remarks>
        /// If there is no left hand connected, this will be null.
        /// This also matches any currently tracked device that contains the 'LeftHand' device usage.
        /// <example>
        /// <code>
        /// // To set up an Action to specifically target
        /// // the left-hand XR controller:
        ///
        /// var action = new InputAction(binding: "/&lt;XRController&gt;{leftHand}/position");
        /// </code>
        /// </example>
        ///
        /// <example>
        /// <code>
        /// // To make the left-hand XR controller behave like the right-hand one
        /// var controller = XRController.leftHand;
        /// InputSystem.SetUsage(controller, CommonUsages.RightHand);
        /// </code>
        /// </example>
        /// </remarks>
        public static XRController leftHand => InputSystem.GetDevice<XRController>(CommonUsages.LeftHand);

        /// <summary>
        /// A quick accessor for the currently active right handed device.  This is also tracked via usages on the device.
        /// </summary>
        /// <remarks>If there is no left hand connected, this will be null. This also matches any currently tracked device that contains the 'RightHand' device usage.</remarks>
        public static XRController rightHand => InputSystem.GetDevice<XRController>(CommonUsages.RightHand);

        protected override void FinishSetup()
        {
            base.FinishSetup();

            var capabilities = description.capabilities;
            var deviceDescriptor = XRDeviceDescriptor.FromJson(capabilities);

            if (deviceDescriptor != null)
            {
                if ((deviceDescriptor.characteristics & InputDeviceCharacteristics.Left) != 0)
                    InputSystem.SetDeviceUsage(this, CommonUsages.LeftHand);
                else if ((deviceDescriptor.characteristics & InputDeviceCharacteristics.Right) != 0)
                    InputSystem.SetDeviceUsage(this, CommonUsages.RightHand);
            }
        }
    }

    /// <summary>
    /// Identifies a controller that is capable of rumble or haptics.
    /// </summary>
    public class XRControllerWithRumble : XRController
    {
        public void SendImpulse(float amplitude, float duration)
        {
            var command = SendHapticImpulseCommand.Create(0, amplitude, duration);
            ExecuteCommand(ref command);
        }
    }
}
#endif
