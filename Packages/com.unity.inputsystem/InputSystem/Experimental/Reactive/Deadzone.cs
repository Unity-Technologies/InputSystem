namespace UnityEngine.InputSystem.Experimental
{
    public static class DeadzoneExtensions
    {
        public static TSource Deadzone<TSource>(this TSource source, float radius = 0.05f) 
            where TSource : IObservableInput
        {
            return source;
        }
    }
}