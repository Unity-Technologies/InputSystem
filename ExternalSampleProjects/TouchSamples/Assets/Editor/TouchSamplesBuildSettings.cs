using UnityEditor;

namespace TouchSamples.Editor
{
    public static class TouchSamplesBuildSettings
    {
        [MenuItem("Build Settings/Touch Samples")]
        public static void Set()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/TouchSamples/Demo/Scenes/Menu.unity", true),
                new EditorBuildSettingsScene("Assets/TouchSamples/Demo/Scenes/Drawing.unity", true),
                new EditorBuildSettingsScene("Assets/TouchSamples/Demo/Scenes/Rolling.unity", true),
                new EditorBuildSettingsScene("Assets/TouchSamples/Demo/Scenes/Swiping.unity", true),
                new EditorBuildSettingsScene("Assets/TouchSamples/Demo/Scenes/Tapping.unity", true),
            };
        }
    }
}
