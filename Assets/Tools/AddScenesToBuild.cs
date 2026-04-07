using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Automatically keeps Build Settings populated with every project scene so the
/// Core Platform Menu works without manual intervention.
///
/// Scenes are refreshed:
///   - Before every player build  (IPreprocessBuildWithReport)
///   - When entering Play Mode    (playModeStateChanged)
///   - On demand via              QA Tools ▸ Refresh Build Scene List
///
/// The Core Platforms Menu scene is always placed at build index 0.
/// </summary>
public class AddScenesToBuild : IPreprocessBuildWithReport
{
    const string kMenuScene = "Assets/QA/Tests/Core Platform Menu/Core Platforms Menu.unity";

    static readonly string[] kExcludedSegments = { "xbox", "xr", "Esc Menu Additive" };
    static readonly string[] kExcludedRoots    = { "Assets/Tests/", "ExternalSampleProjects/" };

    // ── Build callback ──────────────────────────────────────────

    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        RefreshBuildScenes(silent: true);
    }

    // ── Play Mode hook ──────────────────────────────────────────

    [InitializeOnLoadMethod]
    static void RegisterPlayModeHook()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                RefreshBuildScenes(silent: true);
        };
    }

    // ── Menu items ──────────────────────────────────────────────

    [MenuItem("QA Tools/Open Core Scene Menu")]
    static void OpenScene()
    {
        EditorSceneManager.OpenScene(kMenuScene);
    }

    [MenuItem("QA Tools/Refresh Build Scene List")]
    static void RefreshManual()
    {
        RefreshBuildScenes(silent: false);
    }

    [MenuItem("QA Tools/Setup Core Platform Menu Scene")]
    static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog(
                "Setup Core Platform Menu",
                "This will create (or overwrite) the Core Platforms Menu scene with a " +
                "clean setup.  Continue?",
                "Create", "Cancel"))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color32(24, 24, 32, 255);
        cam.cullingMask      = 0;

        var menuGo = new GameObject("Scene Menu");
        AddSceneMenuComponent(menuGo);

        EditorSceneManager.SaveScene(scene, kMenuScene);
        RefreshBuildScenes(silent: false);
        Debug.Log("Core Platform Menu scene created at " + kMenuScene);
    }

    /// <summary>
    /// Adds the SceneMenu component by reflection since it lives in Assembly-CSharp
    /// which this editor assembly cannot directly reference.
    /// </summary>
    static void AddSceneMenuComponent(GameObject target)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = asm.GetType("SceneMenu");
            if (type != null && typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                target.AddComponent(type);
                return;
            }
        }
        Debug.LogWarning("SceneMenu type not found — add the component manually.");
    }

    // ── Core logic ──────────────────────────────────────────────

    static void RefreshBuildScenes(bool silent)
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene");
        var scenePaths = new List<string>();
        string menuPath = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.Equals(path, kMenuScene, StringComparison.OrdinalIgnoreCase))
            {
                menuPath = path;
                continue;
            }
            if (!IsExcluded(path))
                scenePaths.Add(path);
        }

        scenePaths.Sort(StringComparer.OrdinalIgnoreCase);

        if (menuPath != null)
            scenePaths.Insert(0, menuPath);

        var buildScenes = new EditorBuildSettingsScene[scenePaths.Count];
        for (int i = 0; i < scenePaths.Count; i++)
            buildScenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);

        EditorBuildSettings.scenes = buildScenes;

        if (!silent)
            Debug.Log($"Build scene list refreshed — {scenePaths.Count} scenes registered.");
    }

    static bool IsExcluded(string path)
    {
        for (int i = 0; i < kExcludedSegments.Length; i++)
            if (path.IndexOf(kExcludedSegments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        for (int i = 0; i < kExcludedRoots.Length; i++)
            if (path.StartsWith(kExcludedRoots[i], StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
