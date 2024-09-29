using System;
using System.Runtime.InteropServices;

// TODO using Unity.Mathematics;

namespace UnityEngine.InputSystem.Experimental.Devices
{
    public static partial class Usages
    {
        public static partial class MouseUsages
        {
            public static readonly Usage MouseButton = new(2342344);
            public static readonly Usage MouseButton0 = MouseButton + 0;
            public static readonly Usage MouseButton1 = MouseButton + 1;
            public static readonly Usage MouseButton2 = MouseButton + 2;
            public static readonly Usage MouseButton3 = MouseButton + 3;
            public static readonly Usage MouseButton4 = MouseButton + 4;
            public static readonly Usage Delta = new(9518473);
            public static readonly Usage Scroll = new(1231230921);
        }
    }

    /// <summary>
    /// Represents a reading of mouse device state.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct MouseState
    {
        [FieldOffset(0), RelativeControl] public float deltaX;
        [FieldOffset(4), RelativeControl] public float deltaY;
        [FieldOffset(0), RelativeControl] public Vector2 delta;
        [FieldOffset(8), RelativeControl] public float scrollX;
        [FieldOffset(12), RelativeControl] public float scrollY;
        [FieldOffset(8), ButtonsControl] public Vector2 scroll;
        [FieldOffset((16)), ButtonsControl] public uint buttons;
    }
    
    /// <summary>
    /// Represents a standard model interface of a Mouse device.
    /// </summary>
    /// <remarks>
    /// All exposed controls may not be present depending on the capabilities of the underlying hardware, OS or drivers.
    /// </remarks>
    [Serializable]
    public struct Mouse
    {
        public readonly struct Buttons
        {
            private readonly ushort m_DeviceId;
            private readonly Usage m_Usage;
        
            public Buttons(Usage usage, ushort deviceId)
            {
                m_Usage = usage;
                m_DeviceId = deviceId;
            }

            public ObservableControl<bool> this[int index]
            {
                get
                {
                    if (index < 0 || index > 4)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    return Get(index);
                }
            }
    
            private ObservableControl<bool> Get(int index) => new (
                Endpoint.FromDeviceAndUsage(m_DeviceId, m_Usage + index), 
                Field.Bit(16, 0));
        }
        
        #region Device access
        
        /// <summary>
        /// Returns an aggregate <see cref="Mouse"/> device representing the aggregated result of all such devices
        /// on the system (if any).
        /// </summary>
        public static Mouse any => new (0);
        
        /// <summary>
        /// Returns all currently connected <c>Mouse</c> devices on the system (if any).
        /// </summary>
        /// <remarks>
        /// The return value is never <c>null</c>.
        /// </remarks>
        public static ReadOnlySpan<Mouse> devices => GetDevices(Context.instance);
        
        /// <summary>
        /// Returns all currently connected <c>Mouse</c> devices on the system from the perspective of the
        /// given context.
        /// </summary>
        /// <param name="context">The context for which to retrieve devices.</param>
        /// <returns>ReadOnlySpan&lt;Mouse&gt; containing all available devices.</returns>
        public static ReadOnlySpan<Mouse> GetDevices(Context context) => context.GetDevices<Mouse>(); 
        
        #endregion
        
        #region Control access
        
        /// <summary>
        /// Provides an observable representation of the devices delta control that provides relative movement.
        /// </summary>
        public ObservableInput<Vector2> delta => new(Endpoint.FromUsage(Usages.MouseUsages.Delta));
        
        /// <summary>
        /// Provides an observable representation of the devices delta scroll control that provides relative movement.
        /// </summary>
        public ObservableInput<Vector2> scroll => new(Endpoint.FromUsage(Usages.MouseUsages.Scroll));
        
        /// <summary>
        /// Provides an observable representation of the devices button controls.
        /// </summary>
        public Buttons buttons => new (Usages.MouseUsages.MouseButton, m_DeviceId);
        
        #endregion
        
        internal Mouse(ushort deviceId) => m_DeviceId = deviceId;
        
        private ushort m_DeviceId;
    }
}