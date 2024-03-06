using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CoreTestsFixture : InputTestFixture
{
    public override void SetUp()
    {
        base.SetUp();

        // https://fogbugz.unity3d.com/f/cases/1377009/
        // Make sure the runtime's timeline has an offset compared to the input timeline.
        // This ensures that we're tapping the right timeline in the code as in practice,
        // the runtime's timeline will always have an offset.
        runtime.currentTimeOffsetToRealtimeSinceStartup += 10;
        runtime.currentTime = 20;
    }

    public override void TearDown()
    {
        // Unload any additional scenes.
        if (SceneManager.sceneCount > 1)
        {
            // Switch back to UTR scene if this is currently not the active scene
            var utrScene = SceneManager.GetSceneAt(0);
            if (SceneManager.GetActiveScene() != utrScene)
                SceneManager.SetActiveScene(utrScene);

            // Unload all other scenes
            for (var i = SceneManager.sceneCount - 1; i != 0; --i)
                SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
        }

        // Destroy any GameObject in the first scene that isn't hidden and isn't the
        // test runner object. Do this first so that any cleanup finds the system in the
        // state it expects.
        var activeScene = SceneManager.GetActiveScene();
        foreach (var go in activeScene.GetRootGameObjects())
        {
            if (go.hideFlags != 0 || go.name.Contains("tests runner"))
                continue;
            UnityEngine.Object.DestroyImmediate(go);
        }

        base.TearDown();
    }

    public void ResetTime()
    {
        runtime.currentTimeOffsetToRealtimeSinceStartup = default;
        runtime.currentTimeForFixedUpdate = default;
        currentTime = default;
    }
}
