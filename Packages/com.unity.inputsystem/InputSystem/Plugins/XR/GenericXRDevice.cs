using UnityEngine.Experimental.Input.Utilities;
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
        /// <summary>
        /// A quick accessor to grab the currently used HMD, regardless of type.
        /// </summary>
        /// <remarks>If no HMD is connected, this can be null.</remarks>
        public static XRHMD current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }
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
        public static XRController leftHand { get; private set; }

        //// <summary>
        /// A quick accessor for the currently active right handed device.  This is also tracked via usages on the device.
        /// </summary>
        /// <remarks>If there is no left hand connected, this will be null. This also matches any currently tracked device that contains the 'RightHand' device usage.</remarks>
        public static XRController rightHand { get; private set; }

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

        public override void MakeCurrent()
        {
            base.MakeCurrent();

            if (usages.Contains(CommonUsages.LeftHand))
            {
                leftHand = this;
            }
            else if (leftHand == this)
            {
                leftHand = null;
            }

            if (usages.Contains(CommonUsages.RightHand))
            {
                rightHand = this;
            }
            else if (rightHand == this)
            {
                rightHand = null;
            }
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (leftHand == this)
                leftHand = null;
            else if (rightHand == this)
                rightHand = null;
        }
    }

    /// <summary>
    /// Identifies a controller that is capable of rumble or haptics.
    /// </summary>
    public class XRControllerWithRumble : XRController, IHaptics
    {
        SimpleRumble m_Rumble;
        BufferedRumble m_BufferedRumble;

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);
            m_Rumble = new SimpleRumble(this);
        }

        protected override void OnAdded()
        {
            base.OnAdded();

            m_BufferedRumble = new BufferedRumble(this);
            HapticCapabilities capabilities = m_BufferedRumble.capabilities;
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
        /// Pauses haptics so that motorspeed on the device will be 0, regardless of the current intensity level.
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
