using UnityEngine.InputSystem.XR.Haptics;
using UnityEngine.InputSystem.Haptics;
using UnityEngine.InputSystem.Layouts;
#if UNITY_INPUT_SYSTEM_ENABLE_XR
using UnityEngine.XR;
#endif

namespace UnityEngine.InputSystem.XR
{
    /// <summary>
    /// The base type of all XR head mounted displays.  This can help organize shared behaviour across all HMDs.
    /// </summary>
    [InputControlLayout(isGenericTypeOfDevice = true, displayName = "XR HMD")]
    [Scripting.Preserve]
    public class XRHMD : InputDevice
    {
    }

    /// <summary>
    /// The base type for all XR handed controllers.
    /// </summary>
    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" }, isGenericTypeOfDevice = true)]
    [Scripting.Preserve]
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

        protected override void FinishSetup()
        {
            base.FinishSetup();

#if UNITY_INPUT_SYSTEM_ENABLE_XR
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
#endif //UNITY_INPUT_SYSTEM_ENABLE_XR
        }
    }

    /// <summary>
    /// Identifies a controller that is capable of rumble or haptics.
    /// </summary>
    [Scripting.Preserve]
    public class XRControllerWithRumble : XRController
    {
        public void SendImpulse(float amplitude, float duration)
        {
            var command = SendHapticImpulseCommand.Create(0, amplitude, duration);
            ExecuteCommand(ref command);
        }
    }
}
