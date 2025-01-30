#if UNITY_EDITOR || PACKAGE_DOCS_GENERATION
using System;

////REVIEW: should this be a PopupWindowContent?

namespace UnityEngine.InputSystem.Editor
{
    public sealed class InputControlPicker
    {
        public enum Mode
        {
            PickControl,
            PickDevice,
        }
    }
}
#endif // UNITY_EDITOR
