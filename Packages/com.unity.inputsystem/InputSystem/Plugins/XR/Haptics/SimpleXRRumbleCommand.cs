using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.XR.Haptics
{
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct SimpleXRRumbleCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type { get { return new FourCC('X', 'R', 'R', '0'); } }

        public const int kSize = InputDeviceCommand.kBaseCommandSize + sizeof(float);

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public float intensity;


        public FourCC GetTypeStatic()
        {
            return Type;
        }

        public static SimpleXRRumbleCommand Create(float motorIntensity)
        {
            return new SimpleXRRumbleCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
                intensity = motorIntensity
            };
        }
    }
}
