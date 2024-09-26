using System;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Provide a way to enumerate interfaces of device.
    /// <summary>
    /// Represents an human-computer-interface device.
    /// </summary>
    public readonly struct Device : IEquatable<Device>
    {
        private readonly ushort m_Id;

        internal Device(ushort deviceId)
        {
            m_Id = deviceId;
        }

        public ushort id => m_Id;

        public bool Equals(Device other)
        {
            return m_Id == other.m_Id;
        }

        public override bool Equals(object obj)
        {
            return obj is Device other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Id.GetHashCode();
        }
    }
}
