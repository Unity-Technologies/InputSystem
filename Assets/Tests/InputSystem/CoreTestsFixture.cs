using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CoreTestsFixture : InputTestFixture
{
    public override void TearDown()
    {
        // Destroy any GameObject in the current scene that isn't hidden and isn't the
        // test runner object. Do this first so that any cleanup finds the system in the
        // state it expects.
        var scene = SceneManager.GetActiveScene();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.hideFlags != 0 || go.name.Contains("tests runner"))
                continue;
            UnityEngine.Object.DestroyImmediate(go);
        }

        base.TearDown();
    }
}
