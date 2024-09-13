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
}