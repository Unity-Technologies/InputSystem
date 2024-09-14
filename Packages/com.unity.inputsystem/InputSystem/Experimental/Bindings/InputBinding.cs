using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental
{
    public static class InputBinding
    {
        private static ScriptableInputBinding Create(System.Type type)
        {
            // TODO Use registration via code generated registration for custom input types
            
            if (type == typeof(InputEvent))
                return ScriptableObject.CreateInstance<InputEventInputBinding>();
            if (type == typeof(bool))
                return ScriptableObject.CreateInstance<BooleanInputBinding>();
            if (type == typeof(Vector2))
                return ScriptableObject.CreateInstance<Vector2InputBinding>();
            
            throw new ArgumentException($"Type \"{type}\" is not a supported input value type. Custom types " + 
                                        $"need to be marked with {nameof(InputValueTypeAttribute)} to be used " +
                                        "as input value types in asset-based workflows.");
        }
        
        private static WrappedScriptableInputBinding<T> Create<T>() where T : struct
        {
            return (WrappedScriptableInputBinding<T>)Create(typeof(T));
        }
        
        public static WrappedScriptableInputBinding<T> Create<T>(IObservableInput<T> source) 
            where T : struct 
        {
            var binding = (WrappedScriptableInputBinding<T>)Create(typeof(T));
            // TODO Set name?!
            binding.Set(source);
            return binding;
        }
    }
}