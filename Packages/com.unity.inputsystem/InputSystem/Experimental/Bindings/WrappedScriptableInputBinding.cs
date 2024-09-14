using System;
using UnityEditor;

namespace UnityEngine.InputSystem.Experimental
{
    // We probably need ScriptableInputBinding for all built in types we want to support.
    // Then we could have attribute [InputType] on custom types to extend it by generating WrappedScriptableInputBinding instances for those types?

    // We want to convert all fields marked [SerializeField] to JSON properties
    
    public static class JsonExtensions
    {
        public static string ToJson<TSource>(this TSource source) 
        {
            return "{ }";
        }
    }
    
    // TODO Key question for comparison should be, will it generate the exact same output. Hence type specialization should not be relevant.
    
    public static class BindingPresets
    {
        private static void CreateAsset(string fileNameWithoutExtension, string content)
        {
            ProjectWindowUtil.CreateAssetWithContent(
                filename: $"{fileNameWithoutExtension}.{InputBindingImporter.Extension}",
                content: content, 
                icon: null);
        }
        
        // Support creating an empty binding asset
        [MenuItem("Assets/Create/Input/Default Move Input Binding")]
        public static void CreateMove()
        {
            var binding = Combine.Composite(
                Devices.Keyboard.A, Devices.Keyboard.D, Devices.Keyboard.S, Devices.Keyboard.W);
            var json = binding.ToJson();
            CreateAsset("Move", binding.ToJson());
        }
    }
    
    // TODO It would/could be powerful with binding asset from code snippet. 
    
    /*public class ScriptableInputBindingDelegate<T> : ScriptableInputBinding<T> where T : struct
    {
        public override IDisposable Subscribe<TObserver>(Context context, TObserver observer)
        {
            return node.Subscribe(context, observer);
        }
    }*/

    public interface IWrappedScriptableInputBinding<in T> where T : struct
    {
        public void Set<TValue>(in TValue val) where TValue : IObservableInput<T>;
    }
    
    // Interesting take on this problem: https://github.com/Thundernerd/Unity3D-SerializableInterface/blob/main/Runtime/SerializableInterface.cs
    
    public class WrappedScriptableInputBinding<T> : ScriptableInputBinding<T> 
        where T : struct
    {
        // Note that we store a type erased observer m_Value to avoid issues with Unity serialization of generics.
        // Since we only allow Set to be called when the type constraints are fulfilled we will succeed with casts.
        
        // How do we solve this, will lead to boxing of 1 element, one solution may be to wrap at root level via class to do an allocation rather than constant boxing of temporary?
        //[SerializeField] private IObservableInput<T> m_Value; // Seems to be problem
        [SerializeField] private IObservableInput m_Value;

        public void Set(in IObservableInput<T> observable) => m_Value = observable;

        //public IObservableInput<T> value => m_Value;
        public IObservableInput<T> value => m_Value as IObservableInput<T>;
        
        public override IDisposable Subscribe<TObserver>(Context context, TObserver observer) => value.Subscribe(context, observer);
    }

    /*public static class WrappedScriptableInputBinding
    {
        public static ScriptableObject Wrap<TSource, T>(this TSource source) where TSource : IObservableInput<T>
        {
            return null;
        }
    }*/
    
    // TODO Consider this, if we have a single asset type, its by definition not type-safe. 
    
    // TODO This gives us the assigningment validation using standard tools, there might be a better way by using indirection where the ScriptableObject would be an inner part of the root?

    // TODO Should just mark [InputType]
    
    /* A LITTLE BIT BETTER
     public class WrappedScriptableInputBinding : ScriptableInputBinding<bool>
    {
        // How do we solve this, will lead to boxing of 1 element, one solution may be to wrap at root level via class to do an allocation rather than constant boxing of temporary?
        [SerializeField] private IObservableInput<bool> m_Value;
        
        public void Set<TValue>(in TValue val) where TValue : IObservableInput<bool>
        {
            m_Value = val;
        }

        public IObservableInput<bool> value => m_Value;
        
        public override IDisposable Subscribe<TObserver>(Context context, TObserver observer) => m_Value.Subscribe(context, observer);
    }*/
    
    /* WORKS BUT FIXED TYPE
     public class WrappedScriptableInputBinding : ScriptableInputBinding<bool>
    {
        // How do we solve this, Unity doesnt support doing this?
        [SerializeField] private ObservableInput<bool> m_Value;
        
        public void Initialize<TValue>(ObservableInput<bool> val) // TODO Temp
        {
            m_Value = val;
        }

        public ObservableInput<bool> value => m_Value;
        
        public override IDisposable Subscribe<TObserver>(Context context, TObserver observer)
        {
            return m_Value.Subscribe(context, observer);
        }
    }*/
}