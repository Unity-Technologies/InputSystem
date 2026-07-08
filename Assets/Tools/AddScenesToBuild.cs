using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility that populates Build Settings with every project scene so the
/// Core Platform Menu can discover them at runtime.
///
/// This script is intentionally passive — it never runs automatically during CI
/// builds or arbitrary play-mode sessions.  The scene list is only refreshed:
///
///   - When entering Play Mode with the menu scene open (for manual testing)
///   - On demand via  QA Tools > Refresh Build Scene List
///
/// The Core Platforms Menu scene is always placed at build index 0.
/// </summary>
public static class AddScenesToBuild
{
    const string kMenuScene = "Assets/QA/Tests/Core Platform Menu/Core Platforms Menu.unity";

    static readonly string[] kExcludedSegments = { "xbox", "xr" };
    static readonly string[] kExcludedRoots    = { "ExternalSampleProjects/", "Packages/" };

    // ── Play Mode hook (menu scene only) ─────────────────────────

    [InitializeOnLoadMethod]
    static void RegisterPlayModeHook()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            var activeScene = SceneManager.GetActiveScene();
            if (!string.Equals(activeScene.path, kMenuScene, StringComparison.OrdinalIgnoreCase)) return;

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
            if (!string.IsNullOrEmpty(kExcludedSegments[i]) && path.Contains(kExcludedSegments[i], StringComparison.OrdinalIgnoreCase))
                return true;
        for (int i = 0; i < kExcludedRoots.Length; i++)
            if (path.StartsWith(kExcludedRoots[i], StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
