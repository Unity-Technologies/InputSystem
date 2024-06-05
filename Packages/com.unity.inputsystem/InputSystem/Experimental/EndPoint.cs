using System;
using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct EndPoint : IEquatable<EndPoint>
    {
        private const uint deviceIdBits = 16;
        private const int deviceIdShift = 16;

        [FieldOffset(0)] public ulong value;

        public static EndPoint FromUsage(Usage usage)
        {
            return new EndPoint() { value = usage.Value };
        }

        public static EndPoint FromDeviceAndUsage(ushort deviceId, Usage usage)
        {
            return new EndPoint() { value = ((ulong)(deviceId & deviceIdBits) << deviceIdShift) | usage.Value };
        }

        public ushort deviceId => (ushort)((value >> deviceIdShift) & deviceIdBits);

        /// <summary>
        /// Returns the usage part of this end point address.
        /// </summary>
        public Usage usage => new Usage((uint)value);

        public bool Equals(EndPoint other)
        {
            return value == other.value;
        }

        public override bool Equals(object obj)
        {
            return obj is EndPoint other && Equals(other);
        }

        public static bool operator==(EndPoint first, EndPoint second)
        {
            return first.value == second.value;
        }

        public static bool operator!=(EndPoint first, EndPoint second)
        {
            return !(first == second);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
    }
}
