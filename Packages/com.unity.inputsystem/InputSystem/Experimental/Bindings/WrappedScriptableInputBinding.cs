using System;

namespace UnityEngine.InputSystem.Experimental
{
    public class WrappedScriptableInputBinding<T> : ScriptableInputBinding<T> 
        where T : struct
    {
        // Note that we store a type erased observer m_Value to avoid issues with Unity serialization of generics.
        // Since we only allow Set to be called when the type constraints are fulfilled we will succeed with casts.
        
        // How do we solve this, will lead to boxing of 1 element, one solution may be to wrap at root level via class to do an allocation rather than constant boxing of temporary?
        //[SerializeField] private IObservableInput<T> m_Value; // Seems to be problem
        [SerializeReference] private IObservableInput m_Value;

        public void Set(in IObservableInput<T> observable)
        {
            m_Value = observable; // TODO Do we need to use SerializedObject here?!   
        }

        public IObservableInput<T> value => m_Value as IObservableInput<T>;
        
        public override IDisposable Subscribe<TObserver>(Context context, TObserver observer) => value.Subscribe(context, observer);
    }
}