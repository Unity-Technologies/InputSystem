using System;
using System.Runtime.InteropServices;

namespace UnityEngine.InputSystem.Experimental
{
    public enum SourceType 
    {
        Device,
        Derived
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct Endpoint : IEquatable<Endpoint>
    {
        public const ushort kAnySource = 0;
        
        private const int kUsageBits = 32;
        private const int kUsageShift = 0;
        private const uint kUsageMask = 0xffffffffu >> (32 - kUsageBits);
        
        private const int kUsageProtocolBits = 4;
        private const int kUsageProtocolBitsShift = kUsageBits;
        private const uint kUsageProtocolMask = 0xffffffffu >> (32 - kUsageProtocolBits);
        
        private const int kSourceIdBits = 16;
        private const int kSourceIdShift = kUsageProtocolBitsShift + kUsageProtocolBits;
        private const uint kSourceIdMask = 0xffffffffu >> (32 - kSourceIdBits);

        private const int kSourceTypeBits = 4;
        private const int kSourceTypeShift = kSourceIdShift + kSourceIdBits;
        private const uint kSourceTypeMask = 0xffffffffu >> (32 - kSourceTypeBits);
        
        private const int kInputFlag = 62;

        private const int kReservedFlag = 63;

        [FieldOffset(0)] public readonly ulong value;
        
        private Endpoint(Usage usage, byte usageProtocol = 0, ushort sourceId = kAnySource, SourceType sourceType = SourceType.Device)
        {
            value = ((ulong)((uint)sourceType & kSourceTypeMask) << kSourceTypeShift) |
                    ((ulong)(sourceId & kSourceIdMask) << kSourceIdShift) |
                    ((ulong)(usageProtocol & kUsageProtocolMask) << kUsageProtocolBitsShift) |
                    ((ulong)(usage.Value & kUsageMask) << kUsageShift);
        }
        
        /// <summary>
        /// Returns a multi-source endpoint mapping to a specific usage regardless of source identifier.
        /// </summary>
        /// <param name="usage">The associated usage.</param>
        /// <returns><c>Endpoint</c> instance.</returns>
        public static Endpoint FromUsage(Usage usage)
        {
            return new Endpoint(usage: usage, usageProtocol: 0, sourceId: 0, SourceType.Device);
        }

        /// <summary>
        /// Constructs an endpoint for a specific device and usage.
        /// </summary>
        /// <param name="deviceId">The unique device id of the addressed device.</param>
        /// <param name="usage">The associated usage.</param>
        /// <returns><c>Endpoint</c> instance.</returns>
        public static Endpoint FromDeviceAndUsage(ushort deviceId, Usage usage)
        {
            return new Endpoint(usage: usage, usageProtocol: 0, sourceId: deviceId, sourceType: SourceType.Device);
        }

        /// <summary>
        /// Returns the unique identifier of the specific device addressed by this endpoint.
        /// </summary>
        public ushort sourceId => (ushort)((value >> kSourceIdShift) & kSourceIdMask);

        public SourceType sourceType => (SourceType)((value >> kSourceTypeShift) & kSourceTypeMask);

        /// <summary>
        /// Returns the usage part of this endpoint address.
        /// </summary>
        public Usage usage => new Usage((uint)value);

        /// <inheritdoc cref="IEquatable{T}"/>
        public bool Equals(Endpoint other)
        {
            return value == other.value;
        }

        /// <inheritdoc cref="IEquatable{T}"/>
        public override bool Equals(object obj)
        {
            return obj is Endpoint other && Equals(other);
        }
        
        /// <inheritdoc cref="IEquatable{T}"/>
        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        /// <summary>
        /// Compares two endpoints for value equality.
        /// </summary>
        /// <param name="first">The first endpoint.</param>
        /// <param name="second">The second endpoint</param>
        /// <returns><c>true</c> if <paramref name="first"/> and <paramref name="second"/> refer to the same endpoint, else false.</returns>
        public static bool operator==(Endpoint first, Endpoint second)
        {
            return first.value == second.value;
        }

        /// <summary>
        /// Compares two endpoints for inequality.
        /// </summary>
        /// <param name="first">The first endpoint.</param>
        /// <param name="second">The second endpoint.</param>
        /// <returns><c>true</c> if <paramref name="first"/> and <paramref name="second"/> refer to different endpoints, else false.</returns>
        public static bool operator!=(Endpoint first, Endpoint second)
        {
            return !(first == second);
        }
    }
}
