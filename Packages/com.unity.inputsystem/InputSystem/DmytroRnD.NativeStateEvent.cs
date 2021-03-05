using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.DmytroRnD
{
    internal enum NativeStateEventType
    {
        Mouse = 0x4d4f5553, // 'MOUS'
        Keyboard = 0x4b455953, // 'KEYS'
        Pen = 0x50454e20, // 'PEN '
        Touch = 0x544f5543, // 'TOUC'
        Touchscreen = 0x54534352, // 'TSCR'
        Tracking = 0x504f5345, // 'POSE'
        Gamepad = 0x47504144, // 'GPAD'
        HID = 0x48494420, // 'HID '
        Accelerometer = 0x4143434c, // 'ACCL'
        Gyroscope = 0x4759524f, // 'GYRO'
        Gravity = 0x47525620, // 'GRV '
        Attitude = 0x41545444, // 'ATTD'
        LinearAcceleration = 0x4c414343, // 'LACC'
        LinuxJoystick = 0x4c4a4f59, // 'LJOY'
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    internal struct NativeStateEvent
    {
        [FieldOffset(0)]
        public NativeStateEventType Type;
    }
}