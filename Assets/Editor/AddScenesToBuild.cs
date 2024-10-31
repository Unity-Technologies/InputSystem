using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class AddScenesToBuild : EditorWindow
{
    private const string corePlatformsMenu = "Assets/QA/Tests/Core Platform Menu/Core Platforms Menu.unity";

    [MenuItem("QA Tools/Open Core Scene Menu")]
    static void OpenScene()
    {
        EditorSceneManager.OpenScene(corePlatformsMenu);
    }

    [MenuItem("QA Tools/Add All Core Samples to Build")]
    private static void AddAllScenesToBuildExcludingXboxAndXR()
    {
        // Get all available scenes in the project
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        string[] scenePaths = new string[sceneGuids.Length];

        for (int i = 0; i < sceneGuids.Length; i++)
        {
            scenePaths[i] = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
        }
        // Filter out scenes in folders containing "xbox" or "xr"
        List<string> filteredScenePaths = new List<string>();
        string coreScene = null;

        // Find the corePlatformsMenu scene and remove it from the general scene list
        for (int i = 0; i < scenePaths.Length; i++)
        {
            if (scenePaths[i] == corePlatformsMenu)
            {
                coreScene = scenePaths[i];
            }
            else if (!IsPathInExcludedFolder(scenePaths[i]))
            {
                filteredScenePaths.Add(scenePaths[i]);
            }
        }

        // Add and ensure "Core Platforms Menu" is at the beginning of the list
        if (!string.IsNullOrEmpty(coreScene))
        {
            filteredScenePaths.Insert(0, coreScene);
        }

        // Update the build settings
        EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[filteredScenePaths.Count];
        for (int i = 0; i < filteredScenePaths.Count; i++)
        {
            buildScenes[i] = new EditorBuildSettingsScene(filteredScenePaths[i], true);
        }
        EditorBuildSettings.scenes = buildScenes;
        Debug.Log("All scenes (excluding Xbox and XR) added to build settings.");
    }

    private static bool IsPathInExcludedFolder(string path)
    {
        // Specify folder names to exclude
        string[] excludedFolders = { "xbox", "xr" };

        // Check if the path or any part of it contains any of the excluded folder names
        foreach (string folder in excludedFolders)
        {
            if (path.ToLower().Contains(folder.ToLower()))
            {
                return true;
            }
        }
        return false;
    }

    private static void SaveBuildSettings()
    {
        // Save the current build settings to EditorPrefs
        int sceneCount = EditorBuildSettings.scenes.Length;
        EditorPrefs.SetInt("BuildSettingsSceneCount", sceneCount);
        for (int i = 0; i < sceneCount; i++)
        {
            EditorPrefs.SetString($"BuildSettingsScenePath_{i}", EditorBuildSettings.scenes[i].path);
            EditorPrefs.SetBool($"BuildSettingsSceneEnabled_{i}", EditorBuildSettings.scenes[i].enabled);
        }
    }

    private static void RestoreBuildSettings()
    {
        // Restore the build settings from EditorPrefs
        int sceneCount = EditorPrefs.GetInt("BuildSettingsSceneCount", 0);
        EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[sceneCount];
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = EditorPrefs.GetString($"BuildSettingsScenePath_{i}", "");
            bool sceneEnabled = EditorPrefs.GetBool($"BuildSettingsSceneEnabled_{i}", false);

            buildScenes[i] = new EditorBuildSettingsScene(scenePath, sceneEnabled);
        }
        EditorBuildSettings.scenes = buildScenes;
    }
}
