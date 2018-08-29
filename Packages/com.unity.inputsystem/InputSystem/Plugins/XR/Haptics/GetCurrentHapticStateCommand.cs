using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input.Plugins.XR.Haptics
{
    public struct HapticState
    {
        public HapticState(uint samplesQueued, uint samplesAvailable)
        {
            this.samplesQueued = samplesQueued;
            this.samplesAvailable = samplesAvailable;
        }

        public uint samplesQueued { get; private set; }
        public uint samplesAvailable { get; private set; }
    }

    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public struct GetCurrentHapticStateCommand : IInputDeviceCommandInfo
    {
        static FourCC Type { get { return new FourCC('X', 'H', 'S', '0'); } }

        const int kSize = InputDeviceCommand.kBaseCommandSize + (sizeof(uint) * 2);

        public FourCC GetTypeStatic()
        {
            return Type;
        }

        [FieldOffset(0)]
        InputDeviceCommand baseCommand;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public uint samplesQueued;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + sizeof(int))]
        public uint samplesAvailable;

        public HapticState currentState
        {
            get
            {
                return new HapticState(samplesQueued, samplesAvailable);
            }
        }

        public static GetCurrentHapticStateCommand Create()
        {
            return new GetCurrentHapticStateCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
}
