#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal static class InputAssetEditorUtils
    {
        /// <summary>
        /// Represents a dialog result.
        /// </summary>
        internal enum DialogResult
        {
            /// <summary>
            /// The dialog was closed with an invalid path.
            /// </summary>
            InvalidPath,

            /// <summary>
            /// The dialog was cancelled by the user and the path is invalid.
            /// </summary>
            Cancelled,

            /// <summary>
            /// The dialog was accepted by the user and the associated path is valid.
            /// </summary>
            Valid
        }

        internal struct PromptResult
        {
            public PromptResult(DialogResult result, string path)
            {
                this.result = result;
                this.relativePath = path;
            }

            public readonly DialogResult result;
            public readonly string relativePath;
        }

        internal static string MakeProjectFileName(string projectNameSuffixNoExtension)
        {
            return PlayerSettings.productName + "." + projectNameSuffixNoExtension;
        }

        internal static PromptResult PromptUserForAsset(string friendlyName, string suggestedAssetFilePathWithoutExtension, string assetFileExtension)
        {
            // Prompt user for a file name.
            var fullAssetFileExtension = "." + assetFileExtension;
            var path = EditorUtility.SaveFilePanel(
                title: $"Create {friendlyName} File",
                directory: "Assets",
                defaultName: suggestedAssetFilePathWithoutExtension + "." + assetFileExtension,
                extension: assetFileExtension);
            if (string.IsNullOrEmpty(path))
                return new PromptResult(DialogResult.Cancelled, null);

            // Make sure the path is in the Assets/ folder.
            path = path.Replace("\\", "/"); // Make sure we only get '/' separators.
            var dataPath = Application.dataPath + "/";
            if (!path.StartsWith(dataPath, StringComparison.CurrentCultureIgnoreCase))
            {
                Debug.LogError($"{friendlyName} must be stored in Assets folder of the project (got: '{path}')");
                return new PromptResult(DialogResult.InvalidPath, null);
            }

            // Make sure path ends with expected extension
            var extension = Path.GetExtension(path);
            if (string.Compare(extension, fullAssetFileExtension, StringComparison.InvariantCultureIgnoreCase) != 0)
                path += fullAssetFileExtension;

            return new PromptResult(DialogResult.Valid, "Assets/" + path.Substring(dataPath.Length));
        }

        internal static T CreateAsset<T>(T asset, string relativePath) where T : ScriptableObject
        {
            AssetDatabase.CreateAsset(asset, relativePath);
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        public static void DrawMakeActiveGui<T>(T current, T target, string targetName, string entity, Action<T> apply)
            where T : ScriptableObject
        {
            if (current == target)
            {
                EditorGUILayout.HelpBox($"This asset contains the currently active {entity} for the Input System.", MessageType.Info);
                return;
            }

            string currentlyActiveAssetsPath = null;
            if (current != null)
                currentlyActiveAssetsPath = AssetDatabase.GetAssetPath(current);
            if (!string.IsNullOrEmpty(currentlyActiveAssetsPath))
                currentlyActiveAssetsPath = $"The currently active {entity} are stored in {currentlyActiveAssetsPath}. ";
            EditorGUILayout.HelpBox($"Note that this asset does not contain the currently active {entity} for the Input System. {currentlyActiveAssetsPath??""}Click \"Make Active\" below to make \"{targetName}\" the active one.", MessageType.Warning);
            if (GUILayout.Button($"Make active", EditorStyles.miniButton))
                apply(target);
        }

        public static bool IsValidFileExtension(string path)
        {
            return path != null && path.EndsWith("." + InputActionAsset.Extension, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}

#endif // UNITY_EDITOR
