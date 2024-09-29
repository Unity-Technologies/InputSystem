using System;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Provide a way to enumerate interfaces of device.
    // TODO Provide a way to enumerate controls
    /// <summary>
    /// Represents an human-computer-interface device.
    /// </summary>
    public readonly struct Device : IEquatable<Device>
    {
        private readonly ushort m_Id;
        private readonly ushort m_ParentId;
        
        /// <summary>
        /// Constructs a new device.
        /// </summary>
        /// <param name="deviceId">A unique non-zero session ID uniquely identifying the device for the current
        /// application instance.</param>
        /// <param name="parentId">An optional non-zero session ID uniquely identifying the parent device.
        /// May be zero (default) in which case the host system is the parent of the device.</param>
        internal Device(ushort deviceId, ushort parentId = 0)
        {
            m_Id = deviceId;
            m_ParentId = parentId;
        }

        /// <summary>
        /// Returns a session specific identifier of the device.
        /// </summary>
        /// <remarks>This identifier only uniquely identifies the device during the current run-time session
        /// and may be different if the application is restarted.</remarks>
        public ushort id => m_Id;

        /// <summary>
        /// Returns a session specific identifier of the parent device of this device (if any).
        /// </summary>
        public ushort parentId => m_ParentId;

        public bool Equals(Device other) => m_Id == other.m_Id;

        public override bool Equals(object obj) => obj is Device other && Equals(other);

        public override int GetHashCode() => m_Id.GetHashCode();
    }
}
