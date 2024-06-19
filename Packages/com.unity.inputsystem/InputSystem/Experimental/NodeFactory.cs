namespace UnityEngine.InputSystem.Experimental
{
    public interface INodeFactory<out T>
    {
        public T Create();
    }
    
    // Observation: If needed to instantiate type we could use a factory, but do we have to?
    //              We also loose type information using this pattern.
    /*
    public class NodeFactory : INodeFactory<Press<IObservableInput<bool>>>
    {
        public Press<IObservableInput<bool>> Create()
        {
            throw new System.NotImplementedException();
        }
    }*/
}