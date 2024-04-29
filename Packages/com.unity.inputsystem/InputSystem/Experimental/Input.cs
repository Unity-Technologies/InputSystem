using System;

namespace UnityEngine.InputSystem.Experimental
{
    class Input<T> : IDisposable
    {
        private void ReleaseUnmanagedResources()
        {
            // TODO 
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Input()
        {
            ReleaseUnmanagedResources();
        }
    }
}