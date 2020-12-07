using UnityEngine.Scripting;

namespace UnityEngine.InputSystem
{
    [Preserve]
    public class DefaultScreenKeyboardFactory : IScreenKeyboardFactory
    {
        public ScreenKeyboard Create()
        {
            return new EmulatedScreenKeyboard();
        }
    }
}
