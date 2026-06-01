namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Bit-packed id registered with <see cref="InputManager"/> for <see cref="InputActionState"/> state change monitors.
    /// Layout must match <c>TriggerState.kMaxNum*</c> limits on <see cref="InputActionState"/>.
    /// </summary>
    internal readonly struct InputActionStateMonitorIndex
    {
        // Bit layout (64 bits total):
        //  [0–23]  controlIndex   (24 bits)
        //  [24–39] bindingIndex   (16 bits)
        //  [40–47] mapIndex       (8 bits)
        //  [48–63] priority or composite complexity (ushort, 16 bits)
        readonly long m_Packed;

        private InputActionStateMonitorIndex(long packed)
        {
            m_Packed = packed;
        }

        public long Packed => m_Packed;

        public static implicit operator long(InputActionStateMonitorIndex index) => index.m_Packed;

        public static InputActionStateMonitorIndex FromPacked(long packed) => new InputActionStateMonitorIndex(packed);

        public static InputActionStateMonitorIndex Create(int mapIndex, int controlIndex, int bindingIndex, int priority)
        {
            long result = controlIndex;
            result |= (long)bindingIndex << 24;
            result |= (long)mapIndex << 40;
            // Bits 48–63 hold priority or composite complexity (ushort); use ulong shift so values ≥32768
            // pack without corrupting the signed long layout.
            var priorityBits = (ulong)(ushort)(priority & 0xffff);
            result |= (long)(priorityBits << 48);
            return new InputActionStateMonitorIndex(result);
        }

        public int ControlIndex => (int)(m_Packed & 0x00ffffff);

        public int BindingIndex => (int)((m_Packed >> 24) & 0xffff);

        public int MapIndex => (int)((m_Packed >> 40) & 0xff);

        /// <summary>
        /// The high 16 bits of the packed index (matching the ushort priority/complexity slot in <see cref="InputActionState"/>).
        /// </summary>
        public int Priority => (int)(((ulong)m_Packed >> 48) & 0xffff);
    }
}
