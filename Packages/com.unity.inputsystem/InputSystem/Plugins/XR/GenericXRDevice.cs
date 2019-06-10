using UnityEngine.InputSystem.Plugins.XR.Haptics;
using UnityEngine.InputSystem.Haptics;
using UnityEngine.InputSystem.Layouts;

namespace UnityEngine.InputSystem.Plugins.XR
{
    /// <summary>
    /// The base type of all XR head mounted displays.  This can help organize shared behaviour across all HMDs.
    /// </summary>
    [InputControlLayout(isGenericTypeOfDevice = true, displayName = "XR HMD")]
    public class XRHMD : InputDevice
    {
    }

    /// <summary>
    /// The base type for all XR handed controllers.
    /// </summary>
    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" }, isGenericTypeOfDevice = true)]
    public class XRController : InputDevice
    {
        /// <summary>
        /// A quick accessor for the currently active left handed device.
        /// </summary>
        /// <remarks>If there is no left hand connected, this will be null. This also matches any currently tracked device that contains the 'LeftHand' device usage.</remarks>
        public static XRController leftHand => InputSystem.GetDevice<XRController>(CommonUsages.LeftHand);

        //// <summary>
        /// A quick accessor for the currently active right handed device.  This is also tracked via usages on the device.
        /// </summary>
        /// <remarks>If there is no left hand connected, this will be null. This also matches any currently tracked device that contains the 'RightHand' device usage.</remarks>
        public static XRController rightHand => InputSystem.GetDevice<XRController>(CommonUsages.RightHand);

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
    public class XRControllerWithRumble : XRController
    {
        public void SendImpulse(float amplitude, float duration)
        {
            var command = SendHapticImpulseCommand.Create(0, amplitude, duration);
            ExecuteCommand(ref command);
        }
    }
}
