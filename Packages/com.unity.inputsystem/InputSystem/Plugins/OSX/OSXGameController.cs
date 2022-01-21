#if UNITY_EDITOR || UNITY_STANDALONE_OSX
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.OSX.LowLevel;

namespace UnityEngine.InputSystem.OSX.LowLevel
{
    /// <summary>
    /// Structure of HID input reports for SteelSeries Nimbus+ controllers.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal struct NimbusControllerHIDInputState : IInputStateTypeInfo
    {
        [InputControl(name = "leftStick", layout = "Stick", format = "VC2B")]
        [InputControl(name = "leftStick/x", offset = 0, format = "SBYT", parameters = "")]
        [InputControl(name = "leftStick/left", offset = 0, format = "SBYT", parameters = "clamp=1,clampMin=-1,clampMax=0,invert")]
        [InputControl(name = "leftStick/right", offset = 0, format = "SBYT", parameters = "clamp=1,clampMin=0,clampMax=1")]
        [InputControl(name = "leftStick/y", offset = 1, format = "SBYT", parameters = "")]
        [InputControl(name = "leftStick/up", offset = 1, format = "SBYT", parameters = "clamp=1,clampMin=0,clampMax=1")]
        [InputControl(name = "leftStick/down", offset = 1, format = "SBYT", parameters = "clamp=1,clampMin=-1,clampMax=0,invert")]
        [FieldOffset(0)] public sbyte leftStickX;
        [FieldOffset(1)] public sbyte leftStickY;

        [InputControl(name = "rightStick", layout = "Stick", format = "VC2B")]
        [InputControl(name = "rightStick/x", offset = 0, format = "SBYT")]
        [InputControl(name = "rightStick/left", offset = 0, format = "SBYT", parameters = "clamp=1,clampMin=-1,clampMax=0,invert")]
        [InputControl(name = "rightStick/right", offset = 0, format = "SBYT", parameters = "clamp=1,clampMin=0,clampMax=1")]
        [InputControl(name = "rightStick/y", offset = 1, format = "SBYT")]
        [InputControl(name = "rightStick/up", offset = 1, format = "SBYT", parameters = "clamp=1,clampMin=0,clampMax=1")]
        [InputControl(name = "rightStick/down", offset = 1, format = "SBYT", parameters = "clamp=1,clampMin=-1,clampMax=0,invert")]
        [FieldOffset(2)] public sbyte rightStickX;
        [FieldOffset(3)] public sbyte rightStickY;

        [InputControl(name = "leftTrigger", format = "BYTE")]
        [FieldOffset(4)] public byte leftTrigger;
        [InputControl(name = "rightTrigger", format = "BYTE")]
        [FieldOffset(5)] public byte rightTrigger;

        [InputControl(name = "dpad", format = "BIT", layout = "Dpad", sizeInBits = 4)]
        [InputControl(name = "dpad/up", format = "BIT", bit = 0)]
        [InputControl(name = "dpad/right", format = "BIT", bit = 1)]
        [InputControl(name = "dpad/down", format = "BIT", bit = 2)]
        [InputControl(name = "dpad/left", format = "BIT", bit = 3)]
        [InputControl(name = "buttonSouth", displayName = "A", bit = 4)]
        [InputControl(name = "buttonEast", displayName = "B", bit = 5)]
        [InputControl(name = "buttonWest", displayName = "X", bit = 6)]
        [InputControl(name = "buttonNorth", displayName = "Y", bit = 7)]
        [FieldOffset(6)] public byte buttons1;
        [InputControl(name = "leftShoulder", bit = 0)]
        [InputControl(name = "rightShoulder", bit = 1)]
        [InputControl(name = "leftStickPress", bit = 2)]
        [InputControl(name = "rightStickPress", bit = 3)]
        [InputControl(name = "menuButton", layout = "Button", bit = 4)]
        [InputControl(name = "select", bit = 5)]
        [InputControl(name = "start", bit = 6)]
        [FieldOffset(7)] public byte buttons2;

        public FourCC format => new FourCC('H', 'I', 'D');
    }
}

namespace UnityEngine.InputSystem.OSX
{
    /// <summary>
    /// Steel Series Nimbus+ uses iOSGameController MFI when on iOS but
    /// is just a standard HID on osx.
    /// </summary>
    [InputControlLayout(stateType = typeof(NimbusControllerHIDInputState), displayName = "nimbus+ Gamepad")]
    [Scripting.Preserve]
    public class NimbusGameController : Gamepad
    {
    }
}
#endif // UNITY_EDITOR || UNITY_STANDALONE_OSX
