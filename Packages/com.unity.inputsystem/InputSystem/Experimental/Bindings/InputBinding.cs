using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.InputSystem.Experimental
{
    internal interface IInputBinding
    {
        
    }
    
    [Serializable]
    internal enum InputBindingMode
    {
        Undefined,
        UnityObject,
        Object
    }
 
    [Serializable]
    public class InputBinding<T> // TODO IEquatable<InputBinding<T>> 
        where T : struct
    {
        [SerializeField] private Object m_Object;
        [SerializeField] private InputBindingMode m_Mode;
        [SerializeReference] private object m_Value;
        
        public InputBinding() { }

        public InputBinding(IObservableInput<T> value)
        {
            this.value = value;
        }
        
        public IObservableInput<T> value
        {
            get
            {
                switch (m_Mode)
                {
                    // The binding is currently associated with a UnityEngine.Object
                    case InputBindingMode.UnityObject:
                        return m_Object as IObservableInput<T>;
                    
                    // The binding is currently associated with a C# object
                    case InputBindingMode.Object:
                        return m_Value as IObservableInput<T>;
                    
                    // The binding is currently not set and hence is null.
                    case InputBindingMode.Undefined:
                    default:
                        return null;
                }
            }
            set
            {
                if (value is Object unityObject)
                {
                    m_Value = null;
                    m_Object = unityObject;
                    m_Mode = InputBindingMode.UnityObject;
                }
                else 
                {
                    m_Object = null;
                    m_Value = value;
                    m_Mode = InputBindingMode.Object;
                }
            }
        }
    }
}