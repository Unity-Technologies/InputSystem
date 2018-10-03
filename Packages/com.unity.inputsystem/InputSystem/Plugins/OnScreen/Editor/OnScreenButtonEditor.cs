#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.Experimental.Input.Plugins.OnScreen.Editor
{
    [CustomEditor(typeof(OnScreenButton))]
    public class OnScreenButtonEditor : OnScreenControlEditor
    {
    }
}
#endif // UNITY_EDITOR
