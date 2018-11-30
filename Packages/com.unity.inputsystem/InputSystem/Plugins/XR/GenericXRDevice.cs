using UnityEngine.Experimental.Input.Plugins.XR.Haptics;
using UnityEngine.Experimental.Input.Haptics;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    /// <summary>
    /// The base type of all XR head mounted displays.  This can help organize shared behaviour accross all HMDs.
    /// </summary>
    [InputControlLayout]
    public class XRHMD : InputDevice
    {
    }

    /// <summary>
    /// The base type for all XR handed controllers.
    /// </summary>
    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class XRController : InputDevice
    {
        /// <summary>
        /// A quick accessor for the currently active left handed device.
        /// </summary>
        /// <remarks>If there is no left hand connected, this will be null. This also matches any currently tracked device that contains the 'LeftHand' device usage.</remarks>
        public static XRController leftHand
        {
            get { return InputSystem.GetDevice<XRController>(CommonUsages.LeftHand); }
        }

        //// <summary>
        /// A quick accessor for the currently active right handed device.  This is also tracked via usages on the device.
        /// </summary>
        /// <remarks>If there is no left hand connected, this will be null. This also matches any currently tracked device that contains the 'RightHand' device usage.</remarks>
        public static XRController rightHand
        {
            get { return InputSystem.GetDevice<XRController>(CommonUsages.RightHand); }
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            var capabilities = description.capabilities;
            var deviceDescriptor = XRDeviceDescriptor.FromJson(capabilities);

            if (deviceDescriptor != null)
            {
                if (deviceDescriptor.deviceRole == DeviceRole.LeftHanded)
                {
                    InputSystem.SetDeviceUsage(this, CommonUsages.LeftHand);
                }
                else if (deviceDescriptor.deviceRole == DeviceRole.RightHanded)
                {
                    InputSystem.SetDeviceUsage(this, CommonUsages.RightHand);
                }
            }
        }
    }

    /// <summary>
    /// Identifies a controller that is capable of rumble or haptics.
    /// </summary>
    public class XRControllerWithRumble : XRController, IHaptics
    {
        SimpleRumble m_Rumble;

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);
            m_Rumble = new SimpleRumble(this);
        }

        /// <summary>
        /// Set's this device's motor intensity.
        /// </summary>
        /// <param name="intensity">The intensity of [0-1] you'd like to set device's haptic rumbling to.</param>
        /// <remarks>Intensities are updated immediately, and all values outside of the [0-1] range will be clamped.</remarks>
        public void SetIntensity(float intensity)
        {
            m_Rumble.intensity = intensity;
        }

        /// <summary>
        /// Used to check if the haptics for this device is currently paused.
        /// </summary>
        public bool isHapticsPaused
        {
            get
            {
                return m_Rumble.isPaused;
            }
        }

        /// <summary>
        /// Pauses haptics so that motor speed on the device will be 0, regardless of the current intensity level.
        /// </summary>
        public void PauseHaptics()
        {
            m_Rumble.isPaused = true;
        }

        /// <summary>
        /// Resumes haptics so that motor intensity is again forwarded onto the actual device.
        /// </summary>
        public void ResumeHaptics()
        {
            m_Rumble.isPaused = false;
        }

        /// <summary>
        /// Resets the haptics for this device to defaults.  Defaults are an intensity of 0 and unpaused.
        /// </summary>
        public void ResetHaptics()
        {
            m_Rumble.Reset();
        }
    }
}
