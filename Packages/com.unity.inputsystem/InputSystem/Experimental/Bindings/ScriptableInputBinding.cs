using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// An opaque base class for scriptable input bindings.
    /// </summary>
    public abstract class ScriptableInputBinding : ScriptableObject, IObservableInput
    {
        /// <summary>
        /// Returns the associated binding type.
        /// </summary>
        /// <returns>Value type of the binding.</returns>
        public abstract Type GetBindingType(); // TODO Remove if not needed
        
        #region Static factory interface

        private static readonly Dictionary<Type, Type> Factories;

        static ScriptableInputBinding()
        {
            // TODO Use registration via code generated registration for custom input types

            Factories = new Dictionary<Type, Type>
            {
                { typeof(InputEvent), typeof(InputEventInputBinding) },
                { typeof(bool), typeof(BooleanInputBinding) },
                { typeof(Vector2), typeof(Vector2InputBinding) }
            };
            
            // TODO Assert all bindings are inheriting ScriptableInputBinding with inner type matching key type
        }
        
        private static ScriptableInputBinding Create(System.Type type)
        {
            if (Factories.TryGetValue(type, out var bindingType))
                return (ScriptableInputBinding)CreateInstance(bindingType);
            
            throw new ArgumentException($"Type \"{type}\" is not a supported input value type. Custom types " + 
                                        $"need to be marked with {nameof(InputValueTypeAttribute)} to be used " +
                                        "as input value types in asset-based workflows.");
        }
        
        // TODO Consider type erasure
        private static WrappedScriptableInputBinding<T> Create<T>() where T : struct
        {
            return (WrappedScriptableInputBinding<T>)Create(typeof(T));
        }
        
        // TODO Consider type erasure
        public static WrappedScriptableInputBinding<T> Create<T>(IObservableInput<T> source) 
            where T : struct
        {
            var opaque = Create(typeof(T));
            var binding = (WrappedScriptableInputBinding<T>)opaque;
            binding.value = source;
            return binding;
        }
        
        #endregion
    }

    /// <summary>
    /// Represents an abstract scriptable input binding that is also an observable input generating a specific
    /// observable value type.
    /// </summary>
    /// <typeparam name="T">The observable value type.</typeparam>
    public abstract class ScriptableInputBinding<T> : ScriptableInputBinding, IObservableInput<T>, IObservable<T> 
        where T : struct
    {
        public override Type GetBindingType() => typeof(T);
        public IDisposable Subscribe(IObserver<T> observer) => Subscribe(Context.instance, observer);
        public abstract IDisposable Subscribe<TObserver>(Context context, TObserver observer) 
            where TObserver : IObserver<T>;
    }
}