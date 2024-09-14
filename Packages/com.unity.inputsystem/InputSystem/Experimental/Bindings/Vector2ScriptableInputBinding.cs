using System;

namespace UnityEngine.InputSystem.Experimental
{
    // In this case its a fixed binding constructed via code
    /*public class Vector2ScriptableInputBinding : ScriptableInputBinding<Vector2>
    {
        public override IDisposable Subscribe<TObserver>(Context context, TObserver observer)
        {
            return Combine.Composite(Devices.Keyboard.A, Devices.Keyboard.D, Devices.Keyboard.S, Devices.Keyboard.W)
                .Subscribe(context, observer);
        }
    }*/

    // TODO We want a binding constructed containing object
}