namespace UnityEngine.InputSystem.Experimental
{
    public struct Smooth
    {
        // TODO Implement a generic smoothening filter, e.g. 1-euro filter with reasonable defaults
    }

    public static class SmoothExtensions
    {
        /*public static TSource Smooth<TSource>(this TSource source, float alpha = 0.3f, float beta = 0.5f)
            where TSource : IObservableInput<float>
        {
            // TODO Implement
            return source;
        }*/
        
        // TODO This is a pity, without C# INumeric or similar we cannot put proper constraints on the type
        //      For now we need to use a workaround.
        
        public static TSource Smooth<TSource>(this TSource source, float alpha = 0.3f, float beta = 0.5f)
            where TSource : IObservableInput<Vector2>
        {
            // TODO Implement
            return source;
        }
        
        /*public static TSource Smooth<TSource>(this TSource source, float alpha = 0.3f, float beta = 0.5f)
            where TSource : IObservableInput<Vector3>
        {
            // TODO Implement
            return source;
        }*/
    }
}