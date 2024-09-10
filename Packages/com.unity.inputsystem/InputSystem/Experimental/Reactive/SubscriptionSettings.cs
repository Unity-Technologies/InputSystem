using System;

namespace UnityEngine.InputSystem.Experimental
{
    public struct SubscriptionSettings : IEquatable<SubscriptionSettings>
    {
        public const byte DefaultEventGroupPriority = 0;
        public const byte DefaultEventGroup = 0;

        private int m_Settings;

        private const int EventGroupPriorityBitsOffset = 0;
        private const int EventGroupPriorityBits = 8;
        private const int EventGroupPriorityBitsMask = (EventGroupPriorityBits - 1) << EventGroupPriorityBitsOffset;

        private const int EventGroupIdBitsOffset = 0;
        private const int EventGroupIdBits = 8;
        private const int EventGroupIdBitsMask = (EventGroupIdBits - 1) << EventGroupIdBitsOffset;

        /// <summary>
        /// The associated event group priority within the associated event group.
        /// </summary>
        /// <remarks>
        /// Events emitted within an event group (with ID different than zero) will be prioritized based on
        /// this value if multiple events would be emitted based on the same underlying trigger.
        /// If such a situation occurs, only events with the highest priority within that group would be allowed
        /// to propagate. 
        /// </remarks>
        public byte eventGroupPriority
        {
            get => (byte)((m_Settings >> EventGroupPriorityBitsOffset) & EventGroupPriorityBits);
            set
            {
                m_Settings &= ~EventGroupPriorityBitsMask;
                m_Settings |= ((value & EventGroupPriorityBits) << EventGroupPriorityBitsOffset);
            }
        }

        /// <summary>
        /// The associated event group id.
        /// </summary>
        /// <remarks>
        /// Events emitted from a source which is associated with an event group ID different than zero
        /// will be subject for event conflict resolution within that group based on there priority.
        /// For event group zero (default) no conflict resolution occur.
        /// </remarks>
        public byte eventGroupId
        {
            get => (byte)((m_Settings >> EventGroupIdBitsOffset) & EventGroupIdBits);
            set
            {
                m_Settings &= ~EventGroupIdBitsMask;
                m_Settings |= ((value & EventGroupIdBits) << EventGroupIdBitsOffset);
            }
        }

        public bool Equals(SubscriptionSettings other)
        {
            return m_Settings == other.m_Settings;
        }

        public override bool Equals(object obj)
        {
            return obj is SubscriptionSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Settings;
        }

        public static bool operator ==(SubscriptionSettings left, SubscriptionSettings right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SubscriptionSettings left, SubscriptionSettings right)
        {
            return !left.Equals(right);
        }
    }
}