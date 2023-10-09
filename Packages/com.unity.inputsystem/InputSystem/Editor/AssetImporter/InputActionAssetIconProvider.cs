#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Provides access to icons associated with <code>InputActionAsset</code>.
    /// </summary>
    internal static class InputActionAssetIconProvider
    {
        private const string kActionIcon = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/InputAction.png";
        private const string kAssetIcon = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/InputActionAsset.png";

        /// <summary>
        /// Attempts to load the icon associated with an <code>InputActionAsset</code> (.inputactions) asset.
        /// </summary>
        /// <returns>Icon resource reference or <code>null</code> if the resource could not be loaded.</returns>
        public static Texture2D LoadAssetIcon()
        {
            return (Texture2D)EditorGUIUtility.Load(kAssetIcon);
        }

        /// <summary>
        /// Attempts to load the icon associated with an <code>InputActionReference</code> sub-asset of an
        /// <code>InputActionAsset</code> (.inputactions) asset.
        /// </summary>
        /// <returns>Icon resource reference or <code>null</code> if the resource could not be loaded.</returns>
        public static Texture2D LoadActionIcon()
        {
            return (Texture2D)EditorGUIUtility.Load(kActionIcon);
        }
    }
}

#endif // #if UNITY_EDITOR
