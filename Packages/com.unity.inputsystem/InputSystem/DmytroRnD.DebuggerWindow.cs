using System;
using System.Text;
using Unity.Collections;
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
            if (!Core.s_IsInitialized)
                return;

            _instance = this;

            GUILayout.BeginVertical();
            
            GUILayout.Label($"Active step functions");

            var sb = new StringBuilder("", 1000);

            var dataset = Core.s_IngressPipeline.dataset;
            for (var valuesAxisIndex = 0; valuesAxisIndex < dataset.valueAxisIndexToOffset.Length; ++valuesAxisIndex)
            {
                var timestampsAxisIndex = dataset.valueAxisIndexToTimestampIndex[valuesAxisIndex];
                var timestampOffset = dataset.timestampAxisIndexToOffset[timestampsAxisIndex];
                var valuesOffset = dataset.valueAxisIndexToOffset[valuesAxisIndex];
                var length = dataset.timestampAxisIndexToLength[timestampsAxisIndex];
                //if (length == 0)
                //    continue;
                
                var doPartial = length > 10;
                if (doPartial)
                    length = 10;

                var prevTimestamp = dataset.timestampAxisIndexToPreviousRunValue[timestampsAxisIndex];
                var prevValue = dataset.valueAxisIndexToPreviousRunValue[valuesAxisIndex];
                
                var timestamps = dataset.timestamps.ToNativeSlice().Slice(timestampOffset, length);
                var values = dataset.values.ToNativeSlice().Slice(valuesOffset, length);

                sb.Clear();
                
                sb.Append($"{prevTimestamp}/{prevValue}");

                for (var i = 0; i < length; ++i)
                    sb.Append($", {timestamps[i]}/{values[i]}");
                
                if (doPartial)
                    sb.Append(", ...");

                GUILayout.Label($"{Core.s_ValueIndexToName[valuesAxisIndex]} => {sb}");
            }
            
            // for (var i = 0; i < Core.Devices.Length; ++i)
            // {
            //     if (Core.Devices[i].IsInitialized())
            //         GUILayout.Label($"device {i} {Core.Devices[i].LatestStateForDebug()}");
            // }
            
            //for (var i = 0; i < device.ChangedBits.Length; ++i)
            //    Debug.Log($"changed[{i}] = 0b{Convert.ToString((long)device.ChangedBits[i], 2).PadLeft(64, '0')}");

            
            // for (var i = 0; i < Core.Graph.StepFunctions.Length; ++i)
            //     GUILayout.Label($"sf{i} {Core.Graph.StepFunctions[i].ToString()}");
            
            //GUILayout.BeginHorizontal();
            //GUILayout.Box();
            //GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            
            // GUILayout.Label($"buttonLeft wasPressedInThisFrame {Core.Graph.DebugMouseLeftWasPressedThisFrame()}");
            // GUILayout.Label($"buttonLeft wasReleasedInThisFrame {Core.Graph.DebugMouseLeftWasReleasedThisFrame()}");
        }
    }
}