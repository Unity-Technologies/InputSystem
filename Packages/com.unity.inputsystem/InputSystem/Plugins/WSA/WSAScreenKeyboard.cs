#if UNITY_EDITOR || UNITY_WSA
using System;
using UnityEngine;
#if ENABLE_WINMD_SUPPORT
using Windows.UI.ViewManagement;
using Windows.UI.Text.Core;
#endif

namespace UnityEngine.Experimental.Input.Plugins.WSA
{
    public class WSAScreenKeyboard : ScreenKeyboard
    {
        public override void Show(ScreenKeyboardShowParams showParams)
        {
            UnityEngine.WSA.Application.InvokeOnUIThread(new UnityEngine.WSA.AppCallbackItem(() =>
            {
#if ENABLE_WINMD_SUPPORT
            var inputPane = InputPane.GetForCurrentView();
            var context = CoreTextServicesManager.GetForCurrentView().CreateEditContext();
            context.InputPaneDisplayPolicy = CoreTextInputPaneDisplayPolicy.Manual;
            context.InputScope = CoreTextInputScope.Default;
            context.NotifyFocusEnter();
            inputPane.TryShow();
#endif
            }), false);
        }

        public override void Hide()
        {

        }

        public override bool visible
        {
            get
            {
                return false;
            }
        }
    }
}

#endif