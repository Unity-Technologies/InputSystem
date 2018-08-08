#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    // We want an empty editor in the inspector. Editing happens in a dedicated window.
    [CustomEditor(typeof(InputActionAsset))]
    public class InputActionAssetEditor : UnityEditor.Editor
    {
        protected override void OnHeaderGUI()
        {
        }

        public override void OnInspectorGUI()
        {
        }
    }
}
#endif // UNITY_EDITOR
