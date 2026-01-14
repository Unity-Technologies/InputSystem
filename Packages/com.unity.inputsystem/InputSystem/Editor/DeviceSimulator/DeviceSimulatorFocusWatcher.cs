namespace UnityEngine.InputSystem.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System;
    using System.Reflection;

    [InitializeOnLoad]
    public static class DeviceSimulatorFocusWatcher
    {
        static EditorWindow simulatorWindow;
        static bool lastFocusState;

        static DeviceSimulatorFocusWatcher()
        {
            Debug.LogError("Called constructor!!");
            EditorApplication.update += Update;
        }

        static void Update()
        {
            if (simulatorWindow == null)
                simulatorWindow = FindDeviceSimulator();

            if (simulatorWindow == null)
                return;

            bool hasFocus = HasFocus(simulatorWindow);

            if (hasFocus != lastFocusState)
            {
                lastFocusState = hasFocus;
                if (hasFocus)
                    OnSimulatorFocused();
                else
                    OnSimulatorUnfocused();
            }
        }

        static EditorWindow FindDeviceSimulator()
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();

            foreach (var w in windows)
            {
                if (w.titleContent.text.Contains("Device Simulator"))
                    return w;
            }
            return null;
        }

        static bool HasFocus(EditorWindow window)
        {
            // internal: EditorWindow.m_HasFocus
            var field = typeof(EditorWindow)
                .GetField("m_HasFocus", BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
                return false;

            return (bool)field.GetValue(window);
        }

        static void OnSimulatorFocused()
        {
            Debug.Log("📱 Device Simulator focused");
            // Hook your logic here
        }

        static void OnSimulatorUnfocused()
        {
            Debug.Log("🖥️ Device Simulator lost focus");
            // Hook your logic here
        }
    }
}
