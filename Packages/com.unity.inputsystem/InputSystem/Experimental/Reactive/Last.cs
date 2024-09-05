namespace UnityEngine.InputSystem.Experimental
{
    // TODO To implement last, we need to track last object in stream given current model.
    // TODO If all underlying sources are absolute, we can skip all samples directly to last
    // TODO If underlying sources are relative we need to aggregate all samples.
    // TODO We need to register ourself to be poked when we have processed underlying.
    
    public static class LastExtensions
    {
        public static TSource Last<TSource>(this TSource source)
            where TSource : IObservableInput
        {
            return source;
        }
    }
}