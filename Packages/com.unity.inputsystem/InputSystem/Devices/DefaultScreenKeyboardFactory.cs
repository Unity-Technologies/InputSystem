using UnityEngine.Scripting;

namespace UnityEngine.InputSystem
{
    [Preserve]
    public class DefaultScreenKeyboardFactory : IScreenKeyboardFactory
    {
        public ScreenKeyboard Create()
        {
            // Note: Not using defines, since it hides compile errors
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return new Android.AndroidScreenKeyboard();
                case RuntimePlatform.IPhonePlayer:
                    return new iOS.iOSScreenKeyboard();
                default:
                    return new EmulatedScreenKeyboard();
            }
        }
    }
}
