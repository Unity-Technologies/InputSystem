#if UNITY_EDITOR
using UnityEngine.Experimental.Input.LowLevel;

////REVIEW: rename to IInputDiagnostics and "Diagnostics Mode"?

namespace UnityEngine.Experimental.Input
{
    // Internal interface that allows monitoring the system for problems.
    // This is primarily meant to make it easier to diagnose problems in the event stream.
    internal interface IInputDiagnostics
    {
        void OnCannotFindDeviceForEvent(InputEventPtr eventPtr);
        void OnEventTimestampOutdated(InputEventPtr eventPtr, InputDevice device);
        void OnEventFormatMismatch(InputEventPtr eventPtr, InputDevice device);
        void OnEventForDisabledDevice(InputEventPtr eventPtr, InputDevice device);
    }
}
#endif
