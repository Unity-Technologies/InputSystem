#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Plugins.iOS.LowLevel
{
    [StructLayout(LayoutKind.Explicit, Size = 52)]
    public struct MotionDeviceState : IInputStateTypeInfo
    {
        public static FourCC kFormat = new FourCC('I', 'M', 'T', 'N');

        [InputControl][FieldOffset(0)] public Vector3 gravity;
        [InputControl][FieldOffset(12)] public Quaternion attitude;
        [InputControl][FieldOffset(28)] public Vector3 acceleration;
        [InputControl][FieldOffset(40)] public Vector3 rotationRateUnbiased;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}
#endif // UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
