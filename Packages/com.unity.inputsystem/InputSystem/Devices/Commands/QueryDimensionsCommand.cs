using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Query dimensions of a device.
    /// </summary>
    /// <remarks>
    /// This is usually used to query screen dimensions from pointer devices.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct QueryDimensionsCommand : IInputDeviceCommandInfo
    {
        /// <summary>The FourCC type identifier for this command.</summary>
        public static FourCC Type { get { return new FourCC('D', 'I', 'M', 'S'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float) * 2;

        /// <summary>The base <see cref="InputDeviceCommand"/> header.</summary>
        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>Output field filled in with the device dimensions after the command executes.</summary>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public Vector2 outDimensions;

        /// <summary>Static FourCC type code used to identify this command.</summary>
        public FourCC typeStatic
        {
            get { return Type; }
        }

        /// <summary>Creates a query dimensions command.</summary>
        public static QueryDimensionsCommand Create()
        {
            return new QueryDimensionsCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}
