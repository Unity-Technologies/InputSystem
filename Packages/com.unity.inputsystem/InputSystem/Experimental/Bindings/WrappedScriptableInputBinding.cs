using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// A <see cref="ScriptableInputBinding"/> that wraps an abstract <see cref="IObservableInput{T}"/>.
    /// </summary>
    /// <typeparam name="T">The input value type.</typeparam>
    /// <remarks>Internally the observable input is serialized with <see cref="UnityEngine.SerializeReference"/>
    /// which implies that the associated binding needs to be boxed if its a struct type that implements
    /// <see cref="IObservableInput{T}"/>.</remarks>
    public class WrappedScriptableInputBinding<T> : ScriptableInputBinding<T> 
        where T : struct
    {
        [SerializeReference] private IObservableInput<T> m_Value;
        
        /// <summary>
        /// Sets or gets the wrapped <see cref="IObservableInput{T}"/> instance. 
        /// </summary>
        public IObservableInput<T> value
        {
            get => m_Value;
            set => m_Value = value;
        }
        

        #region IObservableInput
        
        public override IDisposable Subscribe<TObserver>(Context context, TObserver observer) => 
            m_Value.Subscribe(context, observer);
        
        #endregion
    }
}