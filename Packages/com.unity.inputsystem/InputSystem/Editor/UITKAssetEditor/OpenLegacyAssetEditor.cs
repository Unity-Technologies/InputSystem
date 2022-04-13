#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal class OpenLegacyAssetEditor
    {
        [MenuItem("Assets/Input System/Open Legacy Editor")]
        public static void OpenEditor()
        {
            var selectedAsset = Selection.activeObject as InputActionAsset;
            if (selectedAsset == null)
                return;

            InputActionEditorWindow.OpenEditor(selectedAsset);
        }
    }
}

#endif
