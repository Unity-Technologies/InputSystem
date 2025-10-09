using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Tests.InputSystem.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>
/// Editor tests for <see cref="UnityEngine.InputSystem.InputActionReference"/>.
/// </summary>
/// <remarks>
/// This test need fixed asset paths since mid-test domain reloads would otherwise discard data.
///
/// Be aware that if you get failures in editor tests that switch between play mode and edit mode via coroutines
/// you might get misleading stack traces that indicate errors in different places than they actually happen.
/// At least this have been observered for exception stack traces.
/// </remarks>
internal class InputActionReferenceEditorTestsWithScene
{
    private const string TestCategory = "Editor";

    private Scene m_Scene;

    private const string assetPath = "Assets/__InputActionReferenceEditorTests.inputactions";
    private const string dummyPath = "Assets/__InputActionReferenceEditorTestsDummy.asset";
    private const string scenePath = "Assets/__InputActionReferenceEditorTestsScene.unity";

    private void CreateAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var map1 = new InputActionMap("map1");
        map1.AddAction("action1");
        map1.AddAction("action2");
        asset.AddActionMap(map1);

        System.IO.File.WriteAllText(assetPath, asset.ToJson());
        Object.DestroyImmediate(asset);
        AssetDatabase.ImportAsset(assetPath);
    }

    [SetUp]
    public void SetUp()
    {
        // This looks odd, but when we yield into play mode from our test coroutine we may get a domain reload
        // (depending on editor preferences) which will trigger another SetUp() mid-test.
        if (Application.isPlaying)
            return;

        EditorPrefsTestUtils.SaveEditorPrefs();

        m_Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        CreateAsset();

        var go = new GameObject("Root");
        var behaviour = go.AddComponent<InputActionBehaviour>();
        var reference = InputActionImporter.LoadInputActionReferencesFromAsset(assetPath).First(
            r => "action1".Equals(r.action.name));
        behaviour.referenceAsField = reference;
        behaviour.referenceAsReference = reference;

        TestUtils.SaveScene(m_Scene, scenePath);
    }

    [TearDown]
    public void TearDown()
    {
        // This looks odd, but when we yield into play mode from our test coroutine we may get a domain reload
        // (depending on editor preferences) which will trigger another TearDown() mid-test.
        if (Application.isPlaying)
            return;

        EditorPrefsTestUtils.RestoreEditorPrefs();

        // Close scene
        EditorSceneManager.CloseScene(m_Scene, true);

        // Clean-up
        AssetDatabase.DeleteAsset(dummyPath);
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.DeleteAsset(scenePath);
    }

    private void DisableDomainReloads()
    {
        // Assumes off before running tests.
        Debug.Assert(!EditorPrefsTestUtils.IsDomainReloadsDisabled());
        Debug.Assert(!EditorPrefsTestUtils.IsSceneReloadsDisabled());

        // Safe to store since state wouldn't be reset by domain reload.
        EditorPrefsTestUtils.DisableDomainReload();
    }

    private static InputActionBehaviour GetBehaviour() => Object.FindFirstObjectByType<InputActionBehaviour>();
    private static InputActionAsset GetAsset() => AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);

    // For unclear reason, NUnit fails to assert throwing exceptions after transition into play-mode.
    // So until that can be sorted out, we do it manually (in the same way) ourselves.
    private static void AssertThrows<T>(Action action) where T : Exception
    {
        var exceptionThrown = false;
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, $"Expected exception of type {typeof(T)} to be thrown but it was not.");
    }

    private static bool[] _disableDomainReloadsValues = new bool[] { false, true };

    [UnityTest]
    [Category(TestCategory)]
    [Description("https://issuetracker.unity3d.com/product/unity/issues/guid/ISXB-1584")]
    public IEnumerator ReferenceSetInPlaymodeShouldBeRestored_WhenExitingPlaymode(
        [ValueSource(nameof(_disableDomainReloadsValues))] bool disableDomainReloads)
    {
        if (disableDomainReloads)
            DisableDomainReloads();

        // Edit-mode section
        {
            // Sanity check our initial edit-mode state
            var obj = GetBehaviour();
            Assert.That(obj.referenceAsField.action, Is.SameAs(GetAsset().FindAction("map1/action1")));
            Assert.That(obj.referenceAsReference.action, Is.SameAs(GetAsset().FindAction("map1/action1")));

            // Enter play-mode (This will lead to domain reload by default).
            yield return new EnterPlayMode();
        }

        // Play-mode section
        {
            var obj = GetBehaviour();
            var editModeAction = GetAsset().FindAction("map1/action1");
            var playModeAction = GetAsset().FindAction("map1/action2");

            // Make sure our action reference is consistent in play-mode
            Assert.That(obj.referenceAsField.action, Is.SameAs(editModeAction));

            // ISXB-1584: Attempting assignment of persisted input action reference in play-mode in editor.
            // Rationale: We cannot allow this since it would corrupt the source asset since changes applied to SO
            // mapped to an asset isn't reverted when exiting play-mode.
            //
            // Here we would like to do:
            //      Assert.Throws<InvalidOperationException>(() => obj.reference.Set(null));
            //
            // But we can't since it would fail with NullReferenceException.
            // It turns out that because of the domain reload / Unity’s internal serialization quirks, the obj is
            // sometimes null inside the lambda when NUnit captures it for execution. So to work around this we
            // instead do the same kind of check manually for now which doesn't seem to have this problem.
            //
            // It is odd since NUnit does basically does the same thing (apart from wrapping the lambda as a
            // TestDelegate). So the WHY for this problem remains unclear for now.
            AssertThrows<InvalidOperationException>(() => obj.referenceAsField.Set(playModeAction));
            AssertThrows<InvalidOperationException>(() => obj.referenceAsReference.Set(editModeAction));

            // Make sure there were no side-effects.
            Assert.That(obj.referenceAsField.action, Is.SameAs(editModeAction));
            Assert.That(obj.referenceAsReference.action, Is.SameAs(editModeAction));

            // Correct usage is to use a run-time assigned input action reference instead. It is up to the user
            // to decide whether this reference should additionally be persisted (which is possible by saving it to
            // and asset, or by using SerializeReference).
            obj.referenceAsField = InputActionReference.Create(playModeAction);
            obj.referenceAsReference = InputActionReference.Create(playModeAction);

            // Makes sure we have the expected reference.
            Assert.That(obj.referenceAsField.action, Is.SameAs(playModeAction));
            Assert.That(obj.referenceAsReference.action, Is.SameAs(playModeAction));

            // Exit play-mode (This will lead to domain reload by default).
            yield return new ExitPlayMode();
        }

        // Edit-mode section
        {
            // Make sure our reference is back to its initial edit mode state
            var obj = GetBehaviour();
            var editModeAction = GetAsset().FindAction("map1/action1");
            Assert.That(obj.referenceAsField.action, Is.SameAs(editModeAction));
            Assert.That(obj.referenceAsReference.action, Is.SameAs(editModeAction));
        }

        yield return null;
    }
}
