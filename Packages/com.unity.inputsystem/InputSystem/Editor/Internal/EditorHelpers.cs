#if UNITY_EDITOR
using System;
using System.Diagnostics.CodeAnalysis;
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

        // Attempts to retrieve the asset GUID associated with the given asset. If asset is null or the asset
        // is not associated with a GUID or the operation fails for any other reason the return value will be null.
        public static string GetAssetGUID(Object asset)
        {
            return !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var assetGuid, out long _)
                ? null : assetGuid;
        }

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

        public static string GetHyperlink(string text, string path)
        {
            return "<a href=\"" + path + $"\">{text}</a>";
        }

        public static string GetHyperlink(string path)
        {
            return GetHyperlink(path, path);
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

        /// <summary>
        /// Attempts to checkout an asset for editing at the given path and overwrite its file content with
        /// the given asset text content.
        /// </summary>
        /// <param name="assetPath">Path to asset to be checkout out and overwritten.</param>
        /// <param name="text">The new file content.</param>
        /// <returns>true if the file was successfully checkout for editing and the file was written.
        /// This function may return false if unable to checkout the file for editing in the underlying
        /// version control system.</returns>
        internal static bool WriteAsset(string assetPath, string text)
        {
            // Attempt to checkout the file path for editing and inform the user if this fails.
            if (!CheckOut(assetPath))
                return false;

            // (Over)write file text content.
            File.WriteAllText(GetPhysicalPath(assetPath), text);

            // Reimport the asset (indirectly triggers ADB notification callbacks)
            AssetDatabase.ImportAsset(assetPath);

            return true;
        }

        /// <summary>
        /// Saves an asset to the given <c>assetPath</c> with file content corresponding to <c>text</c>
        /// if the current content of the asset given by <c>assetPath</c> is different or the asset do not exist.
        /// </summary>
        /// <param name="assetPath">Destination asset path.</param>
        /// <param name="text">The new desired text content to be written to the asset.</param>
        /// <returns><c>true</c> if the asset was successfully modified or created, else <c>false</c>.</returns>
        internal static bool SaveAsset(string assetPath, string text)
        {
            var existingJson = File.Exists(assetPath) ? File.ReadAllText(assetPath) : string.Empty;

            // Return immediately if file content has not changed, i.e. touching the file would not yield a difference.
            if (text == existingJson)
                return false;

            // Attempt to write asset to disc (including checkout the file) and inform the user if this fails.
            if (WriteAsset(assetPath, text))
                return true;

            Debug.LogError($"Unable save asset to \"{assetPath}\" since the asset-path could not be checked-out as editable in the underlying version-control system.");
            return false;
        }

        // Maps path into a physical path.
        public static string GetPhysicalPath(string path)
        {
            return FileUtil.GetPhysicalPath(path);
        }
    }
}
#endif // UNITY_EDITOR
