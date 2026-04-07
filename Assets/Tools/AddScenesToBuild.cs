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

    static readonly string[] kExcludedSegments = { "xbox", "xr" };
    static readonly string[] kExcludedRoots    = { "Assets/Tests/", "ExternalSampleProjects/", "Packages/" };

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
