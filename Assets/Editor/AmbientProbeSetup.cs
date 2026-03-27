using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
static class AmbientProbeSetup
{
    static AmbientProbeSetup()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += () => DynamicGI.UpdateEnvironment();
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        DynamicGI.UpdateEnvironment();
    }
}
