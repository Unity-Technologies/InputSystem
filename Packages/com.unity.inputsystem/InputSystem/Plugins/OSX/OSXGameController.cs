#if UNITY_EDITOR || UNITY_STANDALONE_OSX || PACKAGE_DOCS_GENERATION
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.OSX.LowLevel;
using UnityEngine.InputSystem.Controls;

namespace UnityEngine.InputSystem.OSX.LowLevel
{
    /// <summary>
    /// Structure of HID input reports for SteelSeries Nimbus+ controllers supported
    /// via HID on OSX.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal struct NimbusPlusHIDInputReport : IInputStateTypeInfo
    {
        /// <summary>
        /// A dummy vendor ID made available by OSX when supporting Nimbus+ via HID.
        /// This is exposed by OSX instead of the true SteelSeries vendor ID 0x1038.
        /// </summary>
        public const int OSXVendorId = 0xd;

        /// <summary>
        /// A dummy product ID made available by OSX when supporting Nimbus+ via HID.
        /// This is exposed by OSX instead of the true Nimbus+ product ID 0x1422.
        /// </summary>
        public const int OSXProductId = 0x0;

        public FourCC format => new FourCC('H', 'I', 'D');

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
        [InputControl(name = "homeButton", layout = "Button", bit = 4)]
        [InputControl(name = "select", bit = 5)]
        [InputControl(name = "start", bit = 6)]
        [FieldOffset(7)] public byte buttons2;
    }
}

namespace UnityEngine.InputSystem.OSX
{
    /// <summary>
    /// Steel Series Nimbus+ uses iOSGameController MFI when on iOS but
    /// is just a standard HID on OSX. Note that the gamepad is made available
    /// with incorrect VID/PID by OSX instead of the true VID/PID registred with
    /// USB.org for this device.
    /// </summary>
    [InputControlLayout(stateType = typeof(NimbusPlusHIDInputReport), displayName = "Nimbus+ Gamepad")]
    [Scripting.Preserve]
    public class NimbusGamepadHid : Gamepad
    {
        /// <summary>
        /// The center button in the middle section of the controller.
        /// </summary>
        /// <remarks>
        /// Note that this button is also picked up by OS.
        /// </remarks>
        [InputControl(name = "homeButton", displayName = "Home", shortDisplayName = "Home")]
        public ButtonControl homeButton { get; protected set; }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            homeButton = GetChildControl<ButtonControl>("homeButton");
            Debug.Assert(homeButton != null);

            base.FinishSetup();
        }
    }
}
#endif // UNITY_EDITOR || UNITY_STANDALONE_OSX
