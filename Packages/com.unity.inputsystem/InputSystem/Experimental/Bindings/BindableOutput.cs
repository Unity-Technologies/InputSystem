using System;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Revisit this when inputs are well defined, this should basically be the inverse of input, so basically we would offer observable sources to e.g. a mux on the host side. In its simplest form this is just a function calling into a mux.
    //
    // E.g.
    // Input:  Gamepad.leftStick.Pressed().Subscribe(...);
    //
    // RumbleHaptic haptic;
    // haptic.amplitude = 0.5f;
    // Output: HapticRumble.rumble.Offer(haptic);     // all devices
    //         HapticRumble.devices[0].Offer(haptic); // specific device
    public class BindableOutput<T> : IObserver<T>, IDisposable where T : struct
    {
        public BindableOutput(OutputBindingTarget<T> binding)
        {
        }

        public void Dispose()
        {
        }

        public void Offer(T item)
        {
            // TODO Apply to output bindings
        }

        public void Offer(ref T item)
        {
            // TODO Apply to output bindings
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(T value)
        {
            throw new NotImplementedException();
        }
    }
}
