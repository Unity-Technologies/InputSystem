#if UNITY_EDITOR || UNITY_ANDROID

using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Android
{
    /// <summary>
    /// Command to get a custom value from the runtime
    /// For example, on Android, this command can be used to query whether the back button
    /// leaves the app or not.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    internal struct GetCustomCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('G', 'C', 'C'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(uint) * 2;

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public uint code;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(uint))]
        public uint value;

        public FourCC typeStatic
        {
            get { return Type; }
        }

        public static GetCustomCommand Create()
        {
            return new GetCustomCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize)
            };
        }
    }
}

#endif // UNITY_EDITOR || UNITY_ANDROID