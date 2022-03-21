using System;
using System.Runtime.InteropServices;

namespace Unity.InputSystem.Runtime
{
    internal unsafe class PALCallbacksContainer : IDisposable
    {
        public delegate void Log(sbyte* ptr);

        public delegate void DebugBreak();

        private Log _log;
        private DebugBreak _debugBreak;
        private bool _disposed = false;

        public PALCallbacksContainer(Log log, DebugBreak debugBreak)
        {
            _log = log;
            _debugBreak = debugBreak;

            Native.InputSetPALCallbacks(new InputPALCallbacks
            {
                Log = (delegate* unmanaged[Cdecl]<sbyte*, void>) Marshal.GetFunctionPointerForDelegate(_log),
                DebugTrap = (delegate* unmanaged[Cdecl]<void>) Marshal.GetFunctionPointerForDelegate(_debugBreak)
                //Log = Marshal.GetFunctionPointerForDelegate(_log),
                //DebugTrap = Marshal.GetFunctionPointerForDelegate(_debugBreak)
            });
        }

        public void Dispose()
        {
            Dispose(iAmBeingCalledFromDisposeAndNotFinalize: true);
            GC.SuppressFinalize(this);
        }

        ~PALCallbacksContainer()
        {
            Dispose(iAmBeingCalledFromDisposeAndNotFinalize: false);
        }

        protected void Dispose(bool iAmBeingCalledFromDisposeAndNotFinalize)
        {
            if (_disposed)
                return;
            Native.InputSetPALCallbacks(new InputPALCallbacks { });
            _disposed = true;
        }
    }
}