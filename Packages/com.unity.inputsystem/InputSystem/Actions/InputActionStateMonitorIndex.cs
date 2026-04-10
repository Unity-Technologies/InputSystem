namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Bit-packed id registered with <see cref="InputManager"/> for <see cref="InputActionState"/> state change monitors.
    /// Layout must match <c>TriggerState.kMaxNum*</c> limits on <see cref="InputActionState"/>.
    /// </summary>
    internal readonly struct InputActionStateMonitorIndex
    {
        readonly long m_Packed;

        public InputActionStateMonitorIndex(long packed)
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
            result |= (long)priority << 48;
            return new InputActionStateMonitorIndex(result);
        }

        public int ControlIndex => (int)(m_Packed & 0x00ffffff);

        public int BindingIndex => (int)((m_Packed >> 24) & 0xffff);

        public int MapIndex => (int)((m_Packed >> 40) & 0xff);

        /// <summary>
        /// Only the low 8 bits are stored; larger <see cref="InputAction.Priority"/> values truncate when packed.
        /// </summary>
        public int Priority => (int)((m_Packed >> 48) & 0xff);
    }
}
