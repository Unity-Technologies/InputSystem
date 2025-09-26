using System;
using System.Text;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    [Flags]
    public enum GestureFlags
    {
        None = 0,

        TapGesture = 1 << 0,
        PressAndHoldGesture = 1 << 1,
        DragGesture = 1 << 2,
        Active = 1 << 3,

        PhaseCancel = 1 << 28,
        PhaseStart = 1 << 29,
        PhaseChange = 1 << 30,
        PhaseEnd = 1 << 31,
    }

    public readonly struct GestureEvent
    {
        public readonly GestureFlags flags;
        public readonly Vector2 start;
        public readonly Vector2 delta;
        public readonly double duration;

        public GestureEvent(Vector2 delta, double duration, GestureFlags flags, Vector2 start)
        {
            this.delta = delta;
            this.duration = duration;
            this.flags = flags;
            this.start = start;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (flags.HasFlag(GestureFlags.TapGesture))
                Append(sb, "Tap");
            if (flags.HasFlag(GestureFlags.PressAndHoldGesture))
                Append(sb, "PressAndHold");
            if (flags.HasFlag(GestureFlags.DragGesture))
                Append(sb, "Drag");
            if (flags.HasFlag(GestureFlags.PhaseCancel))
                Append(sb, "PhaseCancel");
            if (flags.HasFlag(GestureFlags.PhaseStart))
                Append(sb, "PhaseStart");
            if (flags.HasFlag(GestureFlags.PhaseChange))
                Append(sb, "PhaseChange");
            if (flags.HasFlag(GestureFlags.PhaseEnd))
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
