using System;

namespace UnityEngine.InputSystem.Experimental
{
    public class BooleanScriptableInputBinding : ScriptableInputBinding<bool>
    {
        public override IDisposable Subscribe<TObserver>(Context context, TObserver observer)
        {
            // TODO Should rather have an internal generic 
            return Devices.Keyboard.Space.Subscribe(context, observer);
        }
    }
    
    /**
     * This of course works since concrete
     */
    public class Vector2ScriptableInputBinding : ScriptableInputBinding<Vector2>
    {
        public override IDisposable Subscribe<TObserver>(Context context, TObserver observer)
        {
            return Combine.Composite(Devices.Keyboard.A, Devices.Keyboard.D, Devices.Keyboard.S, Devices.Keyboard.W)
                .Subscribe(context, observer);
        }
    }
    
    //
    //  "Composite": {
    //      "negativeX": "Keyboard.A"
    //      "positiveX": "Keyboard.B"
    //  }
    //
    //

    public struct Temp<T>
    {
        public T value;
    }
}