#if UNITY_EDITOR || UNITY_ANDROID

using System.Runtime.InteropServices;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Android
{
    /// <summary>
    /// Command to set a custom value in the runtime
    /// For example, on Android, this command can set wether the back button
    /// leaves the app or not.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct SetCustomCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('S', 'C', 'C'); } }

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

        public static SetCustomCommand Create(uint code, uint value)
        {
            return new SetCustomCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                code = code,
                value = value,
            };
        }
    }
}

#endif // UNITY_EDITOR || UNITY_ANDROID