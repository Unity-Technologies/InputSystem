using System;
using UnityEditor;

namespace UnityEngine.InputSystem.DmytroRnD
{
    internal class DebuggerWindow : EditorWindow
    {
        private static DebuggerWindow _instance;

        [MenuItem("Window/Analysis/Dmytro RnD Input Debugger", false, 2200)]
        public static void CreateOrShow()
        {
            if (_instance == null)
            {
                _instance = GetWindow<DebuggerWindow>();
                _instance.Show();
                _instance.titleContent = new GUIContent("Dmytro RnD Input Debug");
            }
            else
            {
                _instance.Show();
                _instance.Focus();
            }
        }

        public static void RefreshCurrent()
        {
            if (_instance == null)
                return;
            _instance.Repaint();
        }
        
        private void OnGUI()
        {
            if (!Core.IsInitialized)
                return;

            _instance = this;

            GUILayout.BeginVertical();
            for (var i = 0; i < Core.Devices.Length; ++i)
            {
                if (Core.Devices[i].IsInitialized())
                    GUILayout.Label($"device {i} {Core.Devices[i].LatestStateForDebug()}");
            }
            
            //for (var i = 0; i < device.ChangedBits.Length; ++i)
            //    Debug.Log($"changed[{i}] = 0b{Convert.ToString((long)device.ChangedBits[i], 2).PadLeft(64, '0')}");

            
            for (var i = 0; i < Core.Graph.StepFunctions.Length; ++i)
                GUILayout.Label($"sf{i} {Core.Graph.StepFunctions[i].ToString()}");
            
            //GUILayout.BeginHorizontal();
            //GUILayout.Box();
            //GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            
            GUILayout.Label($"buttonLeft wasPressedInThisFrame {Core.Graph.DebugMouseLeftWasPressedThisFrame()}");
            GUILayout.Label($"buttonLeft wasReleasedInThisFrame {Core.Graph.DebugMouseLeftWasReleasedThisFrame()}");
        }
    }
}