using System;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen
{
    public class OnScreenKeyboard : Keyboard
    {
        public enum Status
        {
        }

        ////REVIEW: Type is a bad name due to ambiguity with System.Type
        public enum Type
        {
        }

        public Status status
        {
            get { throw new NotImplementedException(); }
        }

        public Type type
        {
            get { throw new NotImplementedException(); }
        }

        public Rect screenArea
        {
            get { throw new NotImplementedException(); }
        }

        public void Show()
        {
            throw new NotImplementedException();
        }

        public void Hide()
        {
            throw new NotImplementedException();
        }
    }
}
