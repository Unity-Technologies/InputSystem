using UnityEditor;

namespace UnityEngine.InputSystem.Experimental
{
    public abstract class InputBinding : ScriptableObject
    {
        public abstract IObservableInput<T> GetObservableInput<T>();
    }
    
    // Defines output type but not storage type
    public class InputBinding<T> : InputBinding 
        where T : struct
    {
        private IObservableInputNode<T> m_Node; // TODO We cannot use an interface, so we need to use TObservableInput instead to make it concrete. This implies we need to build a concrete chain.

        public override IObservableInput<T> GetObservableInput<T>()
        {
            return m_Node as IObservableInput<T>;
        }

        internal void Set(IObservableInputNode<T> node)
        {
            m_Node = node;
        }
        
        // Support creating an empty binding asset
        [MenuItem("Assets/Create/Input/XXX")]
        public static void CreateInputAsset()
        {
            var asset = ScriptableObject.CreateInstance<InputBinding<Vector2>>();
            ProjectWindowUtil.CreateAsset(asset, "Assets/InputBindingTest.asset");
            /*ProjectWindowUtil.CreateAssetWithContent(
                filename: "Binding.asset",
                content: "{}",
                icon: null); // TODO We should set appropriate icon, otherwise no icon is displayed during renaming (prior to file getting imported).*/
        }    
    }
}