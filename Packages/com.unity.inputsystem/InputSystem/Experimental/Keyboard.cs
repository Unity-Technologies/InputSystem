namespace UnityEngine.InputSystem.Experimental.Device
{
    public struct Keyboard
    {
        public static InputBindingSource<Button> w = new();
        public static InputBindingSource<Button> a = new();
        public static InputBindingSource<Button> s = new();
        public static InputBindingSource<Button> d = new();
        public static InputBindingSource<Button> space = new (); // TODO Extract context
    }
}