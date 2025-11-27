#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    // Note that non-existing caching here is intentional since icon selected might be theme dependent.
    // There is no reason to cache icons unless there is a significant performance impact on the editor.

    /// <summary>
    /// Provides access to icons associated with <see cref="InputActionAsset"/> and <see cref="InputActionReference"/>.
    /// </summary>
    internal static class InputActionAssetIconLoader
    {
        private const string kActionIcon = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/InputAction.png";
        private const string kAssetIcon = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/InputActionAsset.png";

        /// <summary>
        /// Attempts to load the icon associated with an <see cref="InputActionAsset"/>.
        /// </summary>
        /// <returns>Icon resource reference or <code>null</code> if the resource could not be loaded.</returns>
        internal static Texture2D LoadAssetIcon()
        {
            return (Texture2D)EditorGUIUtility.Load(kAssetIcon);
        }

        /// <summary>
        /// Attempts to load the icon associated with an <see cref="InputActionReference"/> sub-asset of an
        /// <see cref="InputActionAsset"/>.
        /// </summary>
        /// <returns>Icon resource reference or <code>null</code> if the resource could not be loaded.</returns>
        internal static Texture2D LoadActionIcon()
        {
            return (Texture2D)EditorGUIUtility.Load(kActionIcon);
        }
    }
}

#endif // #if UNITY_EDITOR
