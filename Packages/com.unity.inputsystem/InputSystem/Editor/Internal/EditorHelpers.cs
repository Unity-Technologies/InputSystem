#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal static class EditorHelpers
    {
        public static Action<string> SetSystemCopyBufferContents = s => EditorGUIUtility.systemCopyBuffer = s;
        public static Func<string> GetSystemCopyBufferContents = () => EditorGUIUtility.systemCopyBuffer;

        // SerializedProperty.tooltip *should* give us the tooltip as per [Tooltip] attribute. Alas, for some
        // reason, it's not happening.
        public static string GetTooltip(this SerializedProperty property)
        {
            if (!string.IsNullOrEmpty(property.tooltip))
                return property.tooltip;

            var field = property.GetField();
            if (field != null)
            {
                var tooltipAttribute = field.GetCustomAttribute<TooltipAttribute>();
                if (tooltipAttribute != null)
                    return tooltipAttribute.tooltip;
            }

            return string.Empty;
        }

        public static void RestartEditorAndRecompileScripts(bool dryRun = false)
        {
            // The API here are not public. Use reflection to get to them.
            var editorApplicationType = typeof(EditorApplication);
            var restartEditorAndRecompileScripts =
                editorApplicationType.GetMethod("RestartEditorAndRecompileScripts",
                    BindingFlags.NonPublic | BindingFlags.Static);
            if (!dryRun)
                restartEditorAndRecompileScripts.Invoke(null, null);
            else if (restartEditorAndRecompileScripts == null)
                throw new MissingMethodException(editorApplicationType.FullName, "RestartEditorAndRecompileScripts");
        }

        public static void CheckOut(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            // Make path relative to project folder.
            var projectPath = Application.dataPath;
            if (path.StartsWith(projectPath) && path.Length > projectPath.Length &&
                (path[projectPath.Length] == '/' || path[projectPath.Length] == '\\'))
                path = path.Substring(0, projectPath.Length + 1);

            AssetDatabase.MakeEditable(path);
        }

        public static void CheckOut(Object asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            var path = AssetDatabase.GetAssetPath(asset);
            CheckOut(path);
        }

        // It seems we're getting instabilities on the farm from using EditorGUIUtility.systemCopyBuffer directly in tests.
        // Ideally, we'd have a mocking library to just work around that but well, we don't. So this provides a solution
        // locally to tests.
        public class FakeSystemCopyBuffer : IDisposable
        {
            private string m_Contents;
            private readonly Action<string> m_OldSet;
            private readonly Func<string> m_OldGet;

            public FakeSystemCopyBuffer()
            {
                m_OldGet = GetSystemCopyBufferContents;
                m_OldSet = SetSystemCopyBufferContents;
                SetSystemCopyBufferContents = s => m_Contents = s;
                GetSystemCopyBufferContents = () => m_Contents;
            }

            public void Dispose()
            {
                SetSystemCopyBufferContents = m_OldSet;
                GetSystemCopyBufferContents = m_OldGet;
            }
        }
    }
}
#endif // UNITY_EDITOR
