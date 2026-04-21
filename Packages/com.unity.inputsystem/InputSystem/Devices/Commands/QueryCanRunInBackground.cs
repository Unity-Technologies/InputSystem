using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Queries to see if this device is able to continue to send updates and state changes when the application is not if focus.
    /// </summary>
    /// <seealso cref="InputDevice.canRunInBackground"/>
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize + sizeof(bool))]
    public struct QueryCanRunInBackground : IInputDeviceCommandInfo
    {
        /// <summary>The FourCC type identifier for this command.</summary>
        public static FourCC Type => new FourCC('Q', 'R', 'I', 'B');

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(bool);

        /// <summary>The base <see cref="InputDeviceCommand"/> header.</summary>
        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>Output field set to true if the device can generate input while the app runs in the background.</summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public bool canRunInBackground;

        /// <summary>Static FourCC type code used to identify this command.</summary>
        public FourCC typeStatic => Type;

        /// <summary>Creates a query can-run-in-background command.</summary>
        public static QueryCanRunInBackground Create()
        {
            return new QueryCanRunInBackground
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                canRunInBackground = false
            };
        }
    }
}
