using System;
using System.Security;
using InputSystem.Experimental;

namespace UnityEngine.InputSystem.Experimental
{
    // This is conceptual and should be replaced by native counterpart and just call into native

    public delegate void Callback();

    internal interface IResolveDeviceInterface<T>
    {
        public static T Query() => default;
    }

    /*public sealed class DeviceInterface
    {
        public ushort typeId;
        public Func<>
    }*/
    
    public sealed class Device
    {
        internal struct CastHelper<T>
        {
            public T t;
            public IntPtr onePointerFurtherThanT;
        }

        // Similar to Component
        private extern void GetComponentFastPath(System.Type type, IntPtr oneFurtherThanResultValue);

        private IntPtr m_Ptr; 
        
        public static implicit operator bool(Device device)
        {
            return !object.ReferenceEquals(device, null) && device.m_Ptr != null;
        }
        
        public static int count { get; }

        private struct Stream
        {
            private ushort type;
            private unsafe Stream* stream;
        }

        private unsafe Stream* _streams; // TODO Likely needs something else, e.g. some type which may allow cast to T
        private int _streamsCount;
        
        [SecuritySafeCritical]
        public unsafe T GetDeviceInterface<T>() where T : class, IResolveDeviceInterface<T>
        {
            // TODO Need to map type T to interface usage type.
            //      For a built-in type this may be predefined. For custom type this needs to be registered to
            //      get a unique interface id for the type.

            var typeId = Host.instance.GetType(typeof(T));
            if (typeId == 0)
                return null;

            var streamsEnd = _streams + _streamsCount;
            for (var s = _streams; s != streamsEnd; ++s)
            {
                if (s->type == typeId)
                {
                    // TODO We want to construct interface from stream here
                }
            }
                
                    
            //CastHelper<T> castHelper = new CastHelper<T>();
            //GetComponentFastPath(typeof (T), new IntPtr((void*) &castHelper.onePointerFurtherThanT));
            //return castHelper.t;
        }

        public unsafe T FindDeviceWithInterface<T>()
        {
            return GetDeviceInterface<T>();
        }
    }
}