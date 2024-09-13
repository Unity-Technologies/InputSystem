#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UnityEngine.InputSystem.Experimental.Editor
{
    // Note that non-existing caching here is intentional since icon selected might be theme dependent.
    // There is no reason to cache icons unless there is a significant performance impact on the editor.

    /// <summary>
    /// Access package resources.
    /// </summary>
    internal static class Resources
    {
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

#endif // #if UNITY_EDITOR