using System;

namespace UnityEngine.InputSystem.Experimental
{
    public readonly struct Device : IEquatable<Device>
    {
        public readonly ushort m_Id;

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
