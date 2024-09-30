using System;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Experimental.Internal;

// TODO using Unity.Mathematics;

namespace UnityEngine.InputSystem.Experimental
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
    //[InputInterface(Experimental.Usages.Devices.Mouse)]
    public struct MouseState
    {
        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct Buttons
        {
            public Buttons(uint value)
            {
                this.value = value;
            }
            
            public bool this[int index] => Bits.GetBit(value, index);

            public uint value;
        }
        
        [FieldOffset(0), RelativeControl] public float deltaX;
        [FieldOffset(4), RelativeControl] public float deltaY;
        [FieldOffset(0), RelativeControl] public Vector2 delta;
        [FieldOffset(8), RelativeControl] public float scrollX;
        [FieldOffset(12), RelativeControl] public float scrollY;
        [FieldOffset(8), RelativeControl] public Vector2 scroll;
        [FieldOffset((16)), ButtonsControl] public Buttons buttons;
    }
    
    /// <summary>
    /// Represents a standard model interface of a Mouse device.
    /// </summary>
    /// <remarks>
    /// All exposed controls may not be present depending on the capabilities of the underlying hardware, OS or drivers.
    /// </remarks>
    [Serializable]
    public struct Mouse : IObservableInputNode<MouseState> // TODO Would make sense to use another interface than IDependencyGraphNode since it cannot have children
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
                    return new(
                        Endpoint.FromDeviceAndUsage(m_DeviceId, m_Usage + index),
                        Field.Bit(16, 0));
                }
            }
            
            // TODO Have mouse button enum
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
        public static ReadOnlySpan<Mouse> devices => GetDevices(Context.instance); // TODO Should be observable collection
        
        /// <summary>
        /// Returns all currently connected <c>Mouse</c> devices on the system from the perspective of the
        /// given context.
        /// </summary>
        /// <param name="context">The context for which to retrieve devices.</param>
        /// <returns>ReadOnlySpan&lt;Mouse&gt; containing all available devices.</returns>
        public static ReadOnlySpan<Mouse> GetDevices(Context context) => context.GetDevices<Mouse>();  // TODO Should be observable collection
        
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
        
        public IDisposable Subscribe<TObserver>(Context context, TObserver observer) 
            where TObserver : IObserver<MouseState>
        {
            // Subscribe directly to end-point stream context
            var ctx = context.GetOrCreateStreamContext<MouseState>(
                Endpoint.FromDeviceAndUsage(m_DeviceId, Experimental.Usages.Devices.Mouse));
            return ctx.Subscribe(observer);
        }
        public IDisposable Subscribe(IObserver<MouseState> observer) => Subscribe(Context.instance, observer);
        public bool Equals(IDependencyGraphNode other) => other is Mouse node && Equals(node);
        public bool Equals(Mouse other) => m_DeviceId.Equals(other.m_DeviceId);
        public string displayName => "Standard Mouse";
        public int childCount => 0;
        public IDependencyGraphNode GetChild(int index) => throw new ArgumentOutOfRangeException(nameof(index));
    }
}