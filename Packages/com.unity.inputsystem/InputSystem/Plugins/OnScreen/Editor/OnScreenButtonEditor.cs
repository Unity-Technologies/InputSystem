#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen.Editor
{
    [CustomEditor(typeof(OnScreenButton))]
    public class OnScreenButtonEditor : UnityEditor.Editor
    {
    }
}
#endif // UNITY_EDITOR
