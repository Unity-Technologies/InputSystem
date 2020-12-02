namespace UnityEngine.InputSystem
{
    class EmulatedScreenKeyboardFactory : IScreenKeyboardFactory
    {
        public ScreenKeyboard Create()
        {
            return new EmulatedScreenKeyboard();
        }
    }
}
