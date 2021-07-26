using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CoreTestsFixture : InputTestFixture
{
    public override void TearDown()
    {
        // Destroy any GameObject in the first scene that isn't hidden and isn't the
        // test runner object. Do this first so that any cleanup finds the system in the
        // state it expects.
        foreach (var go in SceneManager.GetSceneAt(0).GetRootGameObjects())
        {
            if (go.hideFlags != 0 || go.name.Contains("tests runner"))
                continue;
            UnityEngine.Object.DestroyImmediate(go);
        }

        // Unload any additional scenes.
        for (var i = 1; i < SceneManager.sceneCount; ++i)
        {
            var scene = SceneManager.GetSceneAt(i);
            SceneManager.UnloadSceneAsync(scene);
        }

        base.TearDown();
    }
}
