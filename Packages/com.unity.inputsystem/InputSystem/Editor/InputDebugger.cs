#if UNITY_EDITOR
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class InputDebugger : IInputDebugger
    {
        public void OnCannotFindDeviceForEvent(InputEventPtr eventPtr)
        {
            Debug.LogError("Cannot find device for input event: " + eventPtr);
        }

        public void OnEventTimestampOutdated(InputEventPtr eventPtr, InputDevice device)
        {
            Debug.LogError(string.Format("'{0}' input event for device '{1}' is outdated (event time: {2}, device time: {3})", eventPtr.type, device, eventPtr.time, device.lastUpdateTime));
        }

        public void OnEventFormatMismatch(InputEventPtr eventPtr, InputDevice device)
        {
            Debug.LogError(string.Format("'{0}' input event for device '{1}' has incorrect format (event format: '{2}', device format: '{3}')",
                    eventPtr.type, device, eventPtr.type, device.stateBlock.format));
        }
    }
}
#endif // UNITY_EDITOR
