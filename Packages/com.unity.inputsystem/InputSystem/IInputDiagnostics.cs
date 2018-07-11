#if UNITY_EDITOR
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Internal interface that allows monitoring the system for problems.
    /// </summary>
    /// <remarks>
    /// This is primarily meant to make it easier to diagnose problems in the event stream.
    ///
    /// Note that while the diagnostics hook is only enabled in the editor, when using
    /// the input debugger connected to a player it will also diagnose problems in the
    /// event stream of the player.
    /// </remarks>
    internal interface IInputDiagnostics
    {
        void OnCannotFindDeviceForEvent(InputEventPtr eventPtr);
        void OnEventTimestampOutdated(InputEventPtr eventPtr, InputDevice device);
        void OnEventFormatMismatch(InputEventPtr eventPtr, InputDevice device);
        void OnEventForDisabledDevice(InputEventPtr eventPtr, InputDevice device);
    }
}
#endif
