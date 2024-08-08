using System;

namespace UnityEngine.InputSystem.Experimental
{
    public interface IObservableInputFactory<out T>
        where T : struct
    {
        public IObservableInputNode<T> Create();
    }
}