using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Device Command that enables IME Composition within the application.  Primarily handled by Keyboard devices.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.kBaseCommandSize + sizeof(byte))]
    public unsafe struct EnableIMECompositionCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('I', 'M', 'E', 'M'); } }

        internal const int kSize = InputDeviceCommand.kBaseCommandSize + +sizeof(uint);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>
        /// Set to true, and if true, Input Method Editors will be used while typing.
        /// </summary>
        public bool imeEnabled
        {
            get { return m_ImeEnabled != 0; }
        }

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        byte m_ImeEnabled;

        public FourCC typeStatic
        {
            get { return Type; }
        }

        public static EnableIMECompositionCommand Create(bool enabled)
        {
            return new EnableIMECompositionCommand
            {
                baseCommand = new InputDeviceCommand(Type, InputDeviceCommand.kBaseCommandSize + sizeof(byte)),
                m_ImeEnabled = enabled ? byte.MaxValue : (byte)0
            };
        }
    }
}
