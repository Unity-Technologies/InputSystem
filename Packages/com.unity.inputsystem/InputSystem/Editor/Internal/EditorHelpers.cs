#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal static class EditorHelpers
    {
        // Provides an abstraction layer on top of EditorGUIUtility to allow replacing the underlying buffer.
        public static Action<string> SetSystemCopyBufferContents = s => EditorGUIUtility.systemCopyBuffer = s;

        // Provides an abstraction layer on top of EditorGUIUtility to allow replacing the underlying buffer.
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

        // Attempts to make an asset editable in the underlying version control system and returns true if successful.
        public static bool CheckOut(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            // Make path relative to project folder.
            var projectPath = Application.dataPath;
            if (path.StartsWith(projectPath) && path.Length > projectPath.Length &&
                (path[projectPath.Length] == '/' || path[projectPath.Length] == '\\'))
                path = path.Substring(0, projectPath.Length + 1);

            return AssetDatabase.MakeEditable(path);
        }

        public static void CheckOut(Object asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            var path = AssetDatabase.GetAssetPath(asset);
            CheckOut(path);
        }

        public static string ReadAllText(string path)
        {
            // Note that FileUtil.GetPhysicalPath(string) is only available in 2021.2 or newer
#if UNITY_2021_2_OR_NEWER
            return File.ReadAllText(FileUtil.GetPhysicalPath(path));
#else
            return File.ReadAllText(path);
#endif
        }

        public static void WriteAllText(string path, string contents)
        {
            // Note that FileUtil.GetPhysicalPath(string) is only available in 2021.2 or newer
#if UNITY_2021_2_OR_NEWER
            File.WriteAllText(path: FileUtil.GetPhysicalPath(path), contents: contents);
#else
            File.WriteAllText(path: path, contents: contents);
#endif
        }
    }
}
#endif // UNITY_EDITOR
