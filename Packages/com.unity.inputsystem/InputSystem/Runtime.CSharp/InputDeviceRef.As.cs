using System;
using System.Runtime.CompilerServices;

namespace Unity.InputSystem.Runtime
{
    public partial struct InputDeviceRef
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref T As<T>() where T : unmanaged, IInputDeviceTrait
        {
            var metaInstance = new T();
            var ptr = Native.InputGetDeviceTrait(this, metaInstance.traitRef);
            if (ptr == null)
                // TODO get the names
                throw new InvalidCastException(
                    $"Device with ref '{_opaque}' doesn't implement trait '{metaInstance.traitRef.transparent}'");
            return ref *(T*) ptr;
        }
    }
}