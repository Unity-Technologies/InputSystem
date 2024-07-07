using System;

namespace UnityEngine.InputSystem.Experimental
{
    public interface IObservableInputFactory<out T>
        where T : struct
    {
        public IObservableInput<T> Create();
    }
}