namespace UnityEngine.InputSystem.Experimental
{
    public unsafe struct NativeObservable<T>
    {
        public delegate*<T, T, T> func; // may call static function
        
    }
}