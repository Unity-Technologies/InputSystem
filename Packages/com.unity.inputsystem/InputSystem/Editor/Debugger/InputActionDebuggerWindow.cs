#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

////TODO: survive domain reload properly

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionDebuggerWindow : EditorWindow
    {
        [NonSerialized] private InputAction m_Action = null;

        public static void CreateOrShowExisting(InputAction action)
        {
            if (action == null)
                throw new System.ArgumentNullException(nameof(action));

            // See if we have an existing window for the action and if so pop it in front.
            if (s_OpenDebuggerWindows != null)
            {
                for (var i = 0; i < s_OpenDebuggerWindows.Count; ++i)
                {
                    var existingWindow = s_OpenDebuggerWindows[i];
                    if (ReferenceEquals(existingWindow.m_Action, action))
                    {
                        existingWindow.Show();
                        existingWindow.Focus();
                        return;
                    }
                }
            }

            // No, so create a new one.
            var window = CreateInstance<InputActionDebuggerWindow>();
            window.Show();
            window.titleContent = new GUIContent(action.name);
            window.AddToList();
        }

        public void OnGUI()
        {
        }

        private static List<InputActionDebuggerWindow> s_OpenDebuggerWindows;

        private void AddToList()
        {
            if (s_OpenDebuggerWindows == null)
                s_OpenDebuggerWindows = new List<InputActionDebuggerWindow>();
            if (!s_OpenDebuggerWindows.Contains(this))
                s_OpenDebuggerWindows.Add(this);
        }

        private void RemoveFromList()
        {
            if (s_OpenDebuggerWindows != null)
                s_OpenDebuggerWindows.Remove(this);
        }
    }
}
#endif // UNITY_EDITOR
