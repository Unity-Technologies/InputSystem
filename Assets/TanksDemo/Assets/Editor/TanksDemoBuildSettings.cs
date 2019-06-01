#if UNITY_EDITOR
using UnityEditor;

namespace TanksDemo.Assets.Editor
{
    public static class TanksDemoBuildSettings
    {
        [MenuItem("Build Settings/Tanks Demo")]
        public static void Set()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/TanksDemo/Assets/Scenes/NewInput.unity", true),
            };
        }
    }
}
#endif
