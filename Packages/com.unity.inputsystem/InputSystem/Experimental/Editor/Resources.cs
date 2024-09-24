using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.InputSystem.Experimental
{
    /// <summary>
    /// Provides access to editor-only Input System package associated resources.
    /// </summary>
    internal static class Resources
    {
        public const string PackagePath = "Packages/com.unity.inputsystem/InputSystem/";
        public const string PackageEditorPath = PackagePath + "Experimental/Editor";
        public const string InputBindingAssetExtension = ".asset";
        public const string InputBindingAssetPresetMenu = "Assets/Create/Input Binding Presets/";
        
        /// <summary>
        /// Supported package-specific icons.
        /// </summary>
        public enum Icon
        {
            Asset,
            Action,
            InteractiveBinding
        }

        /// <summary>
        /// Attempts to load a package-specific icon.
        /// </summary>
        /// <returns>Icon resource reference or <code>null</code> if the resource could not be loaded.</returns>
        internal static Texture2D LoadIcon(Icon icon)
        {
            const string path = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/";

            switch (icon)
            {
                case Icon.Action: return LoadIcon(path + "InputAction.png");
                case Icon.Asset: return LoadIcon(path + "InputActionAsset.png");
                case Icon.InteractiveBinding: return LoadIcon(path + "InteractiveBinding.png");
                default:
                    throw new ArgumentOutOfRangeException(nameof(icon));
            }
        }

        private static Texture2D LoadIcon(string path)
        {
            return (Texture2D)EditorGUIUtility.Load(path);
        }
    }
}