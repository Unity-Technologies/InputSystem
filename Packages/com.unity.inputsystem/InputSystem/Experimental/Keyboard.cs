namespace UnityEngine.InputSystem.Experimental.Devices
{
    public struct Keyboard
    {
        public static InputBindingSource<bool> w = new();
        public static InputBindingSource<bool> a = new();
        public static InputBindingSource<bool> s = new();
        public static InputBindingSource<bool> d = new();
        public static InputBindingSource<bool> space = new();  // TODO Extract context
    }
}
