namespace UnityEngine.InputSystem.LowLevel
{

    public interface IScreenKeyboardStateReceiver
    {
        void OnScreenKeyboardStateChanged(ScreenKeyboardInformation keyboardInformation);
    }
}
