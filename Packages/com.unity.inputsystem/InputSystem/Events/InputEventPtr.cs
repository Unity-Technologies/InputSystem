using System;
using UnityEngine.InputSystem.Utilities;

////REVIEW: nuke this and force raw pointers on all code using events?

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Pointer to an <see cref="InputEvent"/>. Makes it easier to work with InputEvents and hides
    /// the unsafe operations necessary to work with them.
    /// </summary>
    /// <remarks>
    /// Note that event pointers generally refer to event buffers that are continually reused. This means
    /// that event pointers should not be held on to. Instead, to hold onto event data, manually copy
    /// an event to a buffer.
    /// </remarks>
    public unsafe struct InputEventPtr : IEquatable<InputEventPtr>
    {
        // C# does not allow us to have pointers to structs that have managed data members. Since
        // this can't be guaranteed for generic type parameters, they can't be used with pointers.
        // This is why we cannot make InputEventPtr generic or have a generic method that returns
        // a pointer to a specific type of event.
        private readonly InputEvent* m_EventPtr;

        /// <summary>
        /// Initialize the pointer to refer to the given event.
        /// </summary>
        /// <param name="eventPtr">Pointer to an event. Can be <c>null</c>.</param>
        public InputEventPtr(InputEvent* eventPtr)
        {
            m_EventPtr = eventPtr;
        }

        /// <summary>
        /// Whether the pointer is not <c>null</c>.
        /// </summary>
        /// <value>True if the struct refers to an event.</value>
        public bool valid => m_EventPtr != null;

        /// <summary>
        ///
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public bool handled
        {
            get
            {
                if (!valid)
                    return false;
                return m_EventPtr->handled;
            }
            set
            {
                if (!valid)
                    throw new InvalidOperationException("The InputEventPtr is not valid.");
                m_EventPtr->handled = value;
            }
        }

        public int id
        {
            get
            {
                if (!valid)
                    return 0;
                return m_EventPtr->eventId;
            }
            set
            {
                if (!valid)
                    throw new InvalidOperationException("The InputEventPtr is not valid.");
                m_EventPtr->eventId = value;
            }
        }

        public FourCC type
        {
            get
            {
                if (!valid)
                    return new FourCC();
                return m_EventPtr->type;
            }
        }

        public uint sizeInBytes
        {
            get
            {
                if (!valid)
                    return 0;
                return m_EventPtr->sizeInBytes;
            }
        }

        public int deviceId
        {
            get
            {
                if (!valid)
                    return InputDevice.InvalidDeviceId;
                return m_EventPtr->deviceId;
            }
            set
            {
                if (!valid)
                    throw new InvalidOperationException("The InputEventPtr is not valid.");
                m_EventPtr->deviceId = value;
            }
        }

        public double time
        {
            get => valid ? m_EventPtr->time : 0.0;
            set
            {
                if (!valid)
                    throw new InvalidOperationException("The InputEventPtr is not valid.");
                m_EventPtr->time = value;
            }
        }

        internal double internalTime
        {
            get => valid ? m_EventPtr->internalTime : 0.0;
            set
            {
                if (!valid)
                    throw new InvalidOperationException("The InputEventPtr is not valid.");
                m_EventPtr->internalTime = value;
            }
        }

        public InputEvent* data => m_EventPtr;

        // The stateFormat, stateSizeInBytes, and stateOffset properties are very
        // useful for debugging.

        internal FourCC stateFormat
        {
            get
            {
                var eventType = type;
                if (eventType == StateEvent.Type)
                    return StateEvent.FromUnchecked(this)->stateFormat;
                if (eventType == DeltaStateEvent.Type)
                    return DeltaStateEvent.FromUnchecked(this)->stateFormat;
                throw new InvalidOperationException("Event must be a StateEvent or DeltaStateEvent but is " + this);
            }
        }

        internal uint stateSizeInBytes
        {
            get
            {
                if (IsA<StateEvent>())
                    return StateEvent.From(this)->stateSizeInBytes;
                if (IsA<DeltaStateEvent>())
                    return DeltaStateEvent.From(this)->deltaStateSizeInBytes;
                throw new InvalidOperationException("Event must be a StateEvent or DeltaStateEvent but is " + this);
            }
        }

        internal uint stateOffset
        {
            get
            {
                if (IsA<DeltaStateEvent>())
                    return DeltaStateEvent.From(this)->stateOffset;
                throw new InvalidOperationException("Event must be a DeltaStateEvent but is " + this);
            }
        }

        public bool IsA<TOtherEvent>()
            where TOtherEvent : struct, IInputEventTypeInfo
        {
            if (m_EventPtr == null)
                return false;

            // NOTE: Important to say `default` instead of `new TOtherEvent()` here. The latter will result in a call to
            //       `Activator.CreateInstance` on Mono and thus allocate GC memory.
            TOtherEvent otherEvent = default;
            return m_EventPtr->type == otherEvent.typeStatic;
        }

        // NOTE: It is your responsibility to know *if* there actually another event following this one in memory.
        public InputEventPtr Next()
        {
            if (!valid)
                return new InputEventPtr();

            return new InputEventPtr(InputEvent.GetNextInMemory(m_EventPtr));
        }

        public override string ToString()
        {
            if (!valid)
                return "null";

            // il2cpp has a bug which makes builds fail if this is written as 'return m_EventPtr->ToString()'.
            // Gives an error about "trying to constrain an invalid type".
            // Writing it as a two-step operation like here makes it build cleanly.
            var eventPtr = *m_EventPtr;
            return eventPtr.ToString();
        }

        /// <summary>
        /// Return the plain pointer wrapped around by the struct.
        /// </summary>
        /// <returns>A plain pointer. Can be <c>null</c>.</returns>
        public InputEvent* ToPointer()
        {
            return this;
        }

        public bool Equals(InputEventPtr other)
        {
            return m_EventPtr == other.m_EventPtr || InputEvent.Equals(m_EventPtr, other.m_EventPtr);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is InputEventPtr ptr && Equals(ptr);
        }

        public override int GetHashCode()
        {
            return unchecked((int)(long)m_EventPtr);
        }

        public static bool operator==(InputEventPtr left, InputEventPtr right)
        {
            return left.m_EventPtr == right.m_EventPtr;
        }

        public static bool operator!=(InputEventPtr left, InputEventPtr right)
        {
            return left.m_EventPtr != right.m_EventPtr;
        }

        public static implicit operator InputEventPtr(InputEvent* eventPtr)
        {
            return new InputEventPtr(eventPtr);
        }

        public static InputEventPtr From(InputEvent* eventPtr)
        {
            return new InputEventPtr(eventPtr);
        }

        public static implicit operator InputEvent*(InputEventPtr eventPtr)
        {
            return eventPtr.data;
        }

        // Make annoying Microsoft code analyzer happy.
        public static InputEvent* FromInputEventPtr(InputEventPtr eventPtr)
        {
            return eventPtr.data;
        }
    }
}
