using System;
using System.Runtime.InteropServices;

// TODO Make Source and Usage their own types and convert existing Usage into UnityUsage?

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Represents what kind of end-point is represented.
    /// </summary>
    public enum EndpointKind 
    {
        /// <summary>
        /// The end-point represents a device or device control.
        /// </summary>
        Device,
        
        /// <summary>
        /// The end-point represents an end-point that is derived from one or multiple other end-points.
        /// </summary>
        Derived
    }

    /// <summary>
    /// Represents a stream end-point input or output that includes a usage of an associated protocol.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct Endpoint : IEquatable<Endpoint>
    {
        public static readonly Endpoint Invalid = default;
        
        public const ushort AnySource = 0;
        
        /// <summary>
        /// Returns an interleaved multi-source endpoint mapping to a specific usage regardless of source identifier.
        /// </summary>
        /// <param name="usage">The associated usage.</param>
        /// <returns><c>Endpoint</c> instance.</returns>
        public static Endpoint FromUsage(Usage usage) =>
            new Endpoint(usage: usage, usageProtocol: 0, sourceId: 0, EndpointKind.Device);

        /// <summary>
        /// Constructs an endpoint for a specific device and usage.
        /// </summary>
        /// <param name="deviceId">The unique device id of the addressed device.</param>
        /// <param name="usage">The associated usage.</param>
        /// <returns><c>Endpoint</c> instance.</returns>
        public static Endpoint FromDeviceAndUsage(ushort deviceId, Usage usage) =>
            new (usage: usage, usageProtocol: 0, sourceId: deviceId, endpointKind: EndpointKind.Device);

        /// <summary>
        /// Returns the unique identifier of the specific device addressed by this endpoint.
        /// </summary>
        public ushort sourceId => (ushort)((value >> kSourceIdShift) & kSourceIdMask);

        /// <summary>
        /// Returns the associated source type.
        /// </summary>
        /// <see cref="EndpointKind"/>
        public EndpointKind endpointKind => (EndpointKind)((value >> kSourceTypeShift) & kSourceTypeMask);

        /// <summary>
        /// Returns whether this end-point represents an aggregate of all instances of a specific end-point.
        /// </summary>
        public bool isAggregate => sourceId == AnySource;

        /// <summary>
        /// Returns the usage part of this endpoint address.
        /// </summary>
        public Usage usage => new Usage((uint)value);

        /// <inheritdoc cref="IEquatable{T}"/>
        public bool Equals(Endpoint other) => value == other.value;

        /// <inheritdoc cref="IEquatable{T}"/>
        public override bool Equals(object obj) => obj is Endpoint other && Equals(other);
        
        /// <inheritdoc cref="IEquatable{T}"/>
        public override int GetHashCode() => value.GetHashCode();

        /// <summary>
        /// Compares two endpoints for value equality.
        /// </summary>
        /// <param name="first">The first endpoint.</param>
        /// <param name="second">The second endpoint</param>
        /// <returns><c>true</c> if <paramref name="first"/> and <paramref name="second"/> refer to the same endpoint,
        /// else false.</returns>
        public static bool operator==(Endpoint first, Endpoint second) => first.value == second.value;

        /// <summary>
        /// Compares two endpoints for inequality.
        /// </summary>
        /// <param name="first">The first endpoint.</param>
        /// <param name="second">The second endpoint.</param>
        /// <returns><c>true</c> if <paramref name="first"/> and <paramref name="second"/> refer to different endpoints,
        /// else false.</returns>
        public static bool operator!=(Endpoint first, Endpoint second) => !(first == second);
        
        #region Internals
        
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

        [SerializeField, FieldOffset(0)] public ulong value;
        
        private Endpoint(Usage usage, byte usageProtocol = 0, ushort sourceId = AnySource, 
            EndpointKind endpointKind = EndpointKind.Device)
        {
            value = ((ulong)((uint)endpointKind & kSourceTypeMask) << kSourceTypeShift) |
                    ((ulong)(sourceId & kSourceIdMask) << kSourceIdShift) |
                    ((ulong)(usageProtocol & kUsageProtocolMask) << kUsageProtocolBitsShift) |
                    ((ulong)(usage.value & kUsageMask) << kUsageShift);
        }
        
        #endregion
    }
}