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
#if !DISABLE_SCREEN_KEYBOARD
                case RuntimePlatform.Android:
                    return new Android.AndroidScreenKeyboard();
                case RuntimePlatform.IPhonePlayer:
                    return new iOS.iOSScreenKeyboard();
#endif
                default:
                    return new EmulatedScreenKeyboard();
            }
        }
    }

    [Preserve]
    public class DefaultScreenKeyboardFactory2 : IScreenKeyboardFactory
    {
        public ScreenKeyboard Create()
        {
            // Note: Not using defines, since it hides compile errors
            switch (Application.platform)
            {
#if !DISABLE_SCREEN_KEYBOARD
                case RuntimePlatform.Android:
                    return new Android.AndroidScreenKeyboard();
                case RuntimePlatform.IPhonePlayer:
                    return new iOS.iOSScreenKeyboard();
#endif
                default:
                    return new EmulatedScreenKeyboard();
            }
        }
    }
}
