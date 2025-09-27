using System;
using System.Text;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// A gesture event.
    /// </summary>
    public readonly struct GestureEvent
    {
        /// <summary>
        /// Gesture event flags.
        /// </summary>
        [Flags]
        public enum Flags
        {
            /// <summary>
            /// No gesture flags, indicates an invalid or default constructed gesture event.
            /// </summary>
            None = 0,

            /// <summary>
            /// The event is a tap gesture.
            /// </summary>
            TapGesture = 1 << 0,

            /// <summary>
            /// The event is a press-and-hold (also known as long tap) gesture.
            /// </summary>
            PressAndHoldGesture = 1 << 1,

            /// <summary>
            /// The event is a drag gesture.
            /// </summary>
            DragGesture = 1 << 2,

            /// <summary>
            /// The event is an active gesture.
            /// </summary>
            Active = 1 << 3,

            PhaseStart = 1 << 28,
            PhaseChange = 1 << 29,
            PhaseEnd = 1 << 30,
            PhaseCancel = 1 << 31,
        }

        public readonly Flags flags;
        public readonly Vector2 start;
        public readonly Vector2 delta;
        public readonly double duration;

        public GestureEvent(Vector2 delta, double duration, Flags flags, Vector2 start)
        {
            this.delta = delta;
            this.duration = duration;
            this.flags = flags;
            this.start = start;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (flags.HasFlag(Flags.TapGesture))
                Append(sb, "Tap");
            if (flags.HasFlag(Flags.PressAndHoldGesture))
                Append(sb, "PressAndHold");
            if (flags.HasFlag(Flags.DragGesture))
                Append(sb, "Drag");
            if (flags.HasFlag(Flags.PhaseCancel))
                Append(sb, "PhaseCancel");
            if (flags.HasFlag(Flags.PhaseStart))
                Append(sb, "PhaseStart");
            if (flags.HasFlag(Flags.PhaseChange))
                Append(sb, "PhaseChange");
            if (flags.HasFlag(Flags.PhaseEnd))
                Append(sb, "PhaseEnd");
            return $"GestureEvent{{delta: {delta}, duration: {duration}, start: {start}, flags: {sb}}}";
        }

        private static void Append(StringBuilder sb, string s)
        {
            if (sb.Length != 0)
                sb.Append(", ");
            sb.Append(s);
        }

        // TODO Consider including a transform for direct manipulation
    }
}
