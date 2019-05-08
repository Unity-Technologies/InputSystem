#if UNITY_EDITOR || UNITY_WSA
using System;
using UnityEngine;
#if ENABLE_WINMD_SUPPORT
using Windows.UI.ViewManagement;
using Windows.UI.Text.Core;
#endif

namespace UnityEngine.InputSystem.Plugins.WSA
{
    public class WSAScreenKeyboard : ScreenKeyboard
    {
#if ENABLE_WINMD_SUPPORT
        InputPane m_InputPane;
        CoreTextEditContext m_CoreTextEditContext;
#endif
        public WSAScreenKeyboard()
        {
            UnityEngine.WSA.Application.InvokeOnUIThread(new UnityEngine.WSA.AppCallbackItem(() =>
            {
#if ENABLE_WINMD_SUPPORT
                if (m_InputPane == null)
                    m_InputPane = InputPane.GetForCurrentView();

                if (m_CoreTextEditContext == null)
                {
                    m_CoreTextEditContext = CoreTextServicesManager.GetForCurrentView().CreateEditContext();
                    m_CoreTextEditContext.InputPaneDisplayPolicy = CoreTextInputPaneDisplayPolicy.Manual;
                }
#endif
            }), true);
        }

#if ENABLE_WINMD_SUPPORT
        private CoreTextInputScope GetInputScope(ScreenKeyboardType type)
        {
            switch (type)
            {
                case ScreenKeyboardType.URL:
                    return CoreTextInputScope.Url;
                case ScreenKeyboardType.Search:
                    return CoreTextInputScope.Search;
                case ScreenKeyboardType.NumbersAndPunctuation:
                case ScreenKeyboardType.NumberPad:
                    return CoreTextInputScope.Number;
                case ScreenKeyboardType.PhonePad:
                    return CoreTextInputScope.TelephoneNumber;
                case ScreenKeyboardType.NamePhonePad:
                    return CoreTextInputScope.NameOrPhoneNumber;
                case ScreenKeyboardType.EmailAddress:
                case ScreenKeyboardType.Social:
                    return CoreTextInputScope.EmailAddress;
                case ScreenKeyboardType.Default:
                case ScreenKeyboardType.ASCIICapable:
                default:
                    return CoreTextInputScope.Text;
            }
        }

#endif
        public override void Show(ScreenKeyboardShowParams showParams)
        {
#if ENABLE_WINMD_SUPPORT
            m_CoreTextEditContext.InputScope = GetInputScope(showParams.type);
            m_CoreTextEditContext.NotifyFocusEnter();
            m_InputPane.TryShow();
#endif
            ChangeStatus(ScreenKeyboardStatus.Visible);
        }

        public override void Hide()
        {
            if (m_Status != ScreenKeyboardStatus.Visible)
                return;
#if ENABLE_WINMD_SUPPORT
            m_InputPane.TryHide();
#endif
            ChangeStatus(ScreenKeyboardStatus.Done);
        }

        public override string inputFieldText
        {
            get
            {
                // There's no input field on WSA keyboard
                return "";
            }

            set
            {
            }
        }

        public override Rect occludingArea
        {
            get
            {
#if ENABLE_WINMD_SUPPORT
                var r = m_InputPane.OccludedRect;
                return new Rect((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height);
#else
                return Rect.zero;
#endif
            }
        }
    }
}

#endif
