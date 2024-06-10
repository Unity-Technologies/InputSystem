namespace UnityEngine.InputSystem.Experimental.Devices
{
    public enum Key
    {
        
    }
    
    public struct Keyboard
    {
        public static ObservableInput<bool> w = new();
        public static ObservableInput<bool> a = new();
        public static ObservableInput<bool> s = new();
        public static ObservableInput<bool> d = new();
        public static ObservableInput<bool> space = new();  // TODO Extract context
        
        
    }
}
