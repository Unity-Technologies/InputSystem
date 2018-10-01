#if UNITY_EDITOR
using UnityEngine.TestTools;

/// <summary>
/// Adds the demo scene to test builds.
/// </summary>
public class DemoGameTestPrebuildSetup : IPrebuildSetup
{
    public void Setup()
    {
        UnityEditor.EditorBuildSettings.scenes = new[]
        {
            new UnityEditor.EditorBuildSettingsScene("Assets/Demo/Demo.unity", true)
        };
    }
}

#endif // UNITY_EDITOR
