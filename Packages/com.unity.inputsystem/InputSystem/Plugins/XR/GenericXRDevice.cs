#if UNITY_INPUT_SYSTEM_ENABLE_XR || PACKAGE_DOCS_GENERATION
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR.Haptics;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;
using UnityEngine.XR;

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// The base type of all XR head mounted displays.  This can help organize shared behaviour across all HMDs.
    /// </summary>
    [InputControlLayout(isGenericTypeOfDevice = true, displayName = "XR HMD")]
    [Preserve]
    public class XRHMD : TrackedDevice
    {
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control leftEyePosition { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public QuaternionControl leftEyeRotation { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control rightEyePosition { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public QuaternionControl rightEyeRotation { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
        public Vector3Control centerEyePosition { get; private set; }
        [InputControl(noisy = true)]
        [Preserve]
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
    [Preserve]
    public class XRController : TrackedDevice
    {
        /// <summary>
        /// A quick accessor for the currently active left handed device.
        /// </summary>
        /// <remarks>If there is no left hand connected, this will be null. This also matches any currently tracked device that contains the 'LeftHand' device usage.</remarks>
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
#if UNITY_2019_3_OR_NEWER
                if ((deviceDescriptor.characteristics & InputDeviceCharacteristics.Left) != 0)
                    InputSystem.SetDeviceUsage(this, CommonUsages.LeftHand);
                else if ((deviceDescriptor.characteristics & InputDeviceCharacteristics.Right) != 0)
                    InputSystem.SetDeviceUsage(this, CommonUsages.RightHand);
#else
                if (deviceDescriptor.deviceRole == InputDeviceRole.LeftHanded)
                    InputSystem.SetDeviceUsage(this, CommonUsages.LeftHand);
                else if (deviceDescriptor.deviceRole == InputDeviceRole.RightHanded)
                    InputSystem.SetDeviceUsage(this, CommonUsages.RightHand);
#endif //UNITY_2019_3_OR_NEWER
            }
        }
    }

    /// <summary>
    /// This interfaces is used by various <cref="InputDevice"> instances that can process haptic impulses.
    /// </summary>
    public interface IXRRumble
    {
        /// <summary>
        /// True when the underlying system will be able to react to <cref="IXRRumble.SendImpulse"> commands.  In order to see what channels are available see ,<cref="IXRRumble.numChannels">
        /// </summary>
        bool supportsImpulse { get; }
        /// <summary>
        /// The number of channels available to receive a <cref="IXRRumble.SendImpulse"> command to.  Each channel can represent a motor, or diffrent reaction from the underlying device.
        /// </summary>
        int numChannels { get; }

        /// <summary>
        /// Sends an impulse of a set <cref="amplitude"> and <cref="duration">.
        /// </summary>
        /// <remarks>This will always send the impulse over channel 0, see <cref="bool SendImpulse(int, float, float)"> for information on channels</remarks>
        /// <param name="amplitude">The overall power of the impulse from 0.0 to 1.0, where 1.0 is the strongest impulse available to the device.</param>
        /// <param name="duration">How long, in seconds, the impulse should last for.</param>
        /// <returns>True if the impulse was successfully received, false otherwise.</returns>
        bool SendImpulse(float amplitude, float duration);

        /// <summary>
        /// Sends an impulse of a set <cref="amplitude"> and <cref="duration">.
        /// </summary>
        /// <param name="channel">A channel represents an individual motor or other output that can react to impulses being sent to the same source <cref="InputDevice"></param>
        /// <param name="amplitude">The overall power of the impulse from 0.0 to 1.0, where 1.0 is the strongest impulse available to the device.</param>
        /// <param name="duration">How long, in seconds, the impulse should last for.</param>
        /// <returns>True if the impulse was successfully received, false otherwise.</returns>
        bool SendImpulse(int channel, float amplitude, float duration);
    }

    /// <summary>
    /// A specialized sub group of XRController that supports basic rumble commands.
    /// </summary>
    [Preserve]
    public class XRControllerWithRumble : XRController, IXRRumble
    {
        /// <summary>
        /// True when the underlying system will be able to react to <cref="IXRRumble.SendImpulse"> commands.  In order to see what channels are available see ,<cref="IXRRumble.numChannels">
        /// </summary>
        public bool supportsImpulse
        {
            get
            {
                var command = GetHapticCapabilitiesCommand.Create();
                ExecuteCommand(ref command);
                return command.capabilities.supportsImpulse;
            }
        }

        /// <summary>
        /// The number of channels available to receive a <cref="IXRRumble.SendImpulse"> command to.  Each channel can represent a motor, or diffrent reaction from the underlying device.
        /// </summary>
        public int numChannels
        {
            get
            {
                var command = GetHapticCapabilitiesCommand.Create();
                ExecuteCommand(ref command);
                return command.capabilities.numChannels;
            }
        }

        /// <summary>
        /// Sends an impulse of a set <cref="amplitude"> and <cref="duration">.
        /// </summary>
        /// <remarks>This will always send the impulse over channel 0, see <cref="bool SendImpulse(int, float, float)"> for information on channels</remarks>
        /// <param name="amplitude">The overall power of the impulse from 0.0 to 1.0, where 1.0 is the strongest impulse available to the device.</param>
        /// <param name="duration">How long, in seconds, the impulse should last for.</param>
        /// <returns>True if the impulse was successfully received, false otherwise.</returns>
        public bool SendImpulse(float amplitude, float duration)
        {
            return SendImpulse(0, amplitude, duration);
        }

        /// <summary>
        /// Sends an impulse of a set <cref="amplitude"> and <cref="duration">.
        /// </summary>
        /// <param name="channel">A channel represents an individual motor or other output that can react to impulses being sent to the same source <cref="InputDevice"></param>
        /// <param name="amplitude">The overall power of the impulse from 0.0 to 1.0, where 1.0 is the strongest impulse available to the device.</param>
        /// <param name="duration">How long, in seconds, the impulse should last for.</param>
        /// <returns>True if the impulse was successfully received, false otherwise.</returns>
        public bool SendImpulse(int channel, float amplitude, float duration)
        {
            var command = SendHapticImpulseCommand.Create(channel, amplitude, duration);
            return ExecuteCommand(ref command) > 0;
        }
    }
}
#endif
