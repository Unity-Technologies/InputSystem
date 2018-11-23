using System;

namespace UnityEngine.Experimental.Input
{
    public enum ScreenKeyboardType
    {
        Default = 0,
        ASCIICapable = 1,
        NumbersAndPunctuation = 2,
        URL = 3,
        NumberPad = 4,
        PhonePad = 5,
        NamePhonePad = 6,
        EmailAddress = 7,
        NintendoNetworkAccount = 8,
        Social = 9,
        Search = 10
    }


    public class ScreenKeyboardShowParams
    {
        public ScreenKeyboardType type;
        public string initialText;
        public bool autocorrection;
        public bool multiline;
        public bool secure;
        public bool alert;

        ////TODO: no characterLimit here, because the logic for characterLimit is too complex when IME composition occurs, instead let user manage the text from OnTextChanged callback

        public ScreenKeyboardShowParams()
        {
            type = ScreenKeyboardType.Default;
            initialText = string.Empty;
            autocorrection = false;
            multiline = false;
            secure = false;
            alert = false;
        }
    }


    ////TODO: probably need a better name, so not to collide with com.unity.inputsystem\InputSystem\Plugins\OnScreen\OnScreenKeyboard.cs
    public abstract class ScreenKeyboard : Keyboard
    {
        public abstract void Show(ScreenKeyboardShowParams showParams);

        public void Show()
        {
            Show(new ScreenKeyboardShowParams());
        }

        public abstract void Hide();
    }
}
