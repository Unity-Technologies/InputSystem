using System;
using System.Collections.Generic;
using InputSystem.Experimental;

namespace UnityEngine.InputSystem.Experimental
{
    public class Context : IDisposable
    {
        private static Context _instance;

        private Device[] _devices;
        private Dictionary<Type, int> _types;
        private int _typeId;
        //private unsagNativeContext* _nativeContext;

        public Context()
        {
            //_nativeContext = new NativeContext();

            _devices = new Device[1];
            _devices[0] = new Device();
        }
        
        public static Context instance
        {
            get { return _instance ??= new Context(); }
        }

        public int GetDeviceCount()
        {
            return _devices?.Length ?? 0;
        }
        
        public Device GetDevice(int index)
        {
            if (_devices == null || index < 0 || index >= _devices.Length)
                throw new ArgumentOutOfRangeException($"Attempting to access device at index: {index} which is outside valid range.");
            
            return _devices[index];   
        }

        public int RegisterType(Type type)
        {
            return Host.instance.RegisterType(type);
        }

        internal void RegisterCallback(Callback callback)
        {
            
        }
        
        internal void UnregisterCallback(Callback callback)
        {
            
        }

        internal void Update()
        {
            // TODO Get upper bound from host m_UpperBound = ... 
            m_LowerBound = m_UpperBound;
        }

        private uint m_LowerBound;
        private uint m_UpperBound;

        private void ReleaseUnmanagedResources()
        {
            // TODO Destroy native context
        }

        public void Dispose()
        {
            /*if (_devices != null)
            {
                for (var i = 0; i < _devices.Length; ++i)
                {
                    _devices[0].Dispose();
                }
                _devices = null;
            }*/
            
            
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Context()
        {
            ReleaseUnmanagedResources();
        }
    }
}