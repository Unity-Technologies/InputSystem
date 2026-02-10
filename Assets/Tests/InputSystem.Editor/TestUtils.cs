using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.SceneManagement;

// Replicated in this test assembly to avoid building public API picked up by PackageValidator.
internal class TestUtils
{
#if UNITY_EDITOR
    // Replaces all dialogs in Input System editor code
    public static void MockDialogs()
    {
        // Default mock dialogs to avoid unexpected cancellation of standard flows
        Dialog.InputActionAsset.SetSaveChanges((_) => Dialog.Result.Discard);
        Dialog.InputActionAsset.SetDiscardUnsavedChanges((_) => Dialog.Result.Discard);
        Dialog.InputActionAsset.SetCreateAndOverwriteExistingAsset((_) => Dialog.Result.Discard);
        Dialog.ControlScheme.SetDeleteControlScheme((_) => Dialog.Result.Delete);
    }

    public static void RestoreDialogs()
    {
        // Re-enable dialogs.
        Dialog.InputActionAsset.SetSaveChanges(null);
        Dialog.InputActionAsset.SetDiscardUnsavedChanges(null);
        Dialog.InputActionAsset.SetCreateAndOverwriteExistingAsset(null);
        Dialog.ControlScheme.SetDeleteControlScheme(null);
    }

    /// <summary>
    /// Returns the (default) name of a scene asset created at the given path.
    /// </summary>
    /// <param name="scenePath">The path to the asset.</param>
    /// <returns>Scene name.</returns>
    public static string GetSceneName(string scenePath)
    {
        return System.IO.Path.GetFileNameWithoutExtension(scenePath);
    }

    /// <summary>
    /// Saves a scene to the given path.
    /// </summary>
    /// <param name="scene">The scene to be saved.</param>
    /// <param name="scenePath">The target scene path.</param>
    /// <param name="makeDirty">If true, force saves the scene by making it explicitly dirty.</param>
    /// <exception cref="Exception">Throws exception if failed to save scene
    /// (if dirty or <paramref name="makeDirty"/> was true)</exception>
    public static void SaveScene(Scene scene, string scenePath, bool makeDirty = true)
    {
        var wasDirty = scene.isDirty;
        if (makeDirty)
            EditorSceneManager.MarkSceneDirty(scene);
        var wasSaved = EditorSceneManager.SaveScene(scene, scenePath);
        if (!wasSaved && (wasDirty || makeDirty))
            throw new Exception($"Failed to save scene to {scenePath}");
    }

    /// <summary>
    /// Adds a scene asset to build settings unless it is already present.
    /// </summary>
    /// <param name="scenePath">The scene path to the scene asset.</param>
    /// <returns>Value tuple where first argument (int) specifies the build index of the scene and the second
    /// argument (bool) specifies whether the scene was added (true) or already present (false).</returns>
    public static ValueTuple<int, bool> AddSceneToEditorBuildSettings(string scenePath)
    {
        // Determine if scene is already present or not.
        var existingScenes = EditorBuildSettings.scenes;
        var index = FindSceneIndexByPath(existingScenes, scenePath);
        if (index >= 0)
            return new ValueTuple<int, bool>(index, false);

        // Add the new scene.
        var newScenes = new EditorBuildSettingsScene[existingScenes.Length + 1];
        Array.Copy(existingScenes, newScenes, existingScenes.Length);
        newScenes[existingScenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;

        return new ValueTuple<int, bool>(existingScenes.Length, true);
    }

    /// <summary>
    /// Removes a scene asset from editor build settings given its path.
    /// </summary>
    /// <param name="scenePath">The asset path of the scene to be removed.</param>
    /// <returns>true if the scene was removed, false if it do not exist within editor build settings scenes.</returns>
    public static bool RemoveSceneFromEditorBuildSettings(string scenePath)
    {
        var existingScenes = EditorBuildSettings.scenes;
        var index = FindSceneIndexByPath(existingScenes, scenePath);
        if (index < 0)
            return false;

        var newScenes = new EditorBuildSettingsScene[existingScenes.Length - 1];
        Array.Copy(existingScenes, newScenes, index);
        Array.Copy(existingScenes, index + 1, newScenes, index, existingScenes.Length - index - 1);
        EditorBuildSettings.scenes = newScenes;
        return true;
    }

    private static int FindSceneIndexByPath(EditorBuildSettingsScene[] scenes, string scenePath)
    {
        for (var i = 0; i < scenes.Length; ++i)
        {
            if (scenes[i].path == scenePath)
                return i;
        }
        return -1;
    }

#endif // UNITY_EDITOR
}
