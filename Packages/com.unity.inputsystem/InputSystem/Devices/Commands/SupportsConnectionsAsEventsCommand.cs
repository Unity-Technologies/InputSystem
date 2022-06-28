using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    // Check if native code supports sending connections as events.
    // Send it to deviceId 0 as it's a special "global" IOCTL that gets routed internally.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal struct SupportsConnectionsAsEventsCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('C', 'N', 'E', 'V'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        public FourCC typeStatic
        {
            get { return Type; }
        }

        public static SupportsConnectionsAsEventsCommand Create()
        {
            return new SupportsConnectionsAsEventsCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
}
