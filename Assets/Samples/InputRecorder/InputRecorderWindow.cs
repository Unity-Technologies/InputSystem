#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

////TODO: this is unfinished

namespace UnityEngine.InputSystem.Editor
{
    internal class InputRecorderWindow : EditorWindow
    {
        public static void OpenOrShow(InputRecorder recorder)
        {
            var window = s_OpenWindows.FirstOrDefault(x => x.recorder == recorder);
            if (window == null)
            {
                window = CreateInstance<InputRecorderWindow>();
                window.InitializeWith(recorder);
                s_OpenWindows.Add(window);
            }

            window.Show();
            window.Focus();
        }

        private void InitializeWith(InputRecorder recorder)
        {
            titleContent = new GUIContent("Input Trace");
            this.recorder = recorder;
        }

        public InputRecorder recorder { get; private set; }

        private static List<InputRecorderWindow> s_OpenWindows = new List<InputRecorderWindow>();
    }
}
#endif
