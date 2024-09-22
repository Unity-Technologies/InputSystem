using System;

namespace UnityEngine.InputSystem.Experimental
{
    [Serializable]
    public class Dummy : IObservableInput<Vector2>
    {
        [SerializeField] private int m_SomeNumber;

        public Dummy()
        {
            
        }
        
        public Dummy(int number)
        {
            m_SomeNumber = number;
        }
        
        public IDisposable Subscribe<TObserver>(Context context, TObserver observer) where TObserver : IObserver<Vector2>
        {
            throw new NotImplementedException();
        }
    }
}