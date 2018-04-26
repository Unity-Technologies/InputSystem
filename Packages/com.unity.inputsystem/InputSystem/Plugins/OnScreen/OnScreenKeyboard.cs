using System;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen
{
    public class OnScreenKeyboard : Keyboard
    {
        public enum Status
        {
        }

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
