namespace UnityEngine.InputSystem.Experimental
{
    public struct Released : IUnaryFunc<bool, InputEvent>
    {
        private bool m_PreviousValue;
        
        public bool Process(bool arg0, ref InputEvent result)
        {
            if (m_PreviousValue == arg0) 
                return false;
            
            m_PreviousValue = arg0;
            if (!arg0)
            {
                result = new InputEvent();
                return true;    
            }
            return false;
        }
    }
        
    public static class ReleasedExtensions
    {
        private const string kReleaseDisplayName = "Release";
        
        public static Unary<bool, TSource, InputEvent, Released> Released<TSource>(this TSource source)
            where TSource : IObservableInput<bool>, IDependencyGraphNode
        {
            return new Unary<bool, TSource, InputEvent, Released>(kReleaseDisplayName, source, new Released());
        }
    }
}