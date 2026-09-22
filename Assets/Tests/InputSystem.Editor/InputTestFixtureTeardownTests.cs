using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Regression tests for IN-107889: InputTestFixture.TearDown() fails to reset Input System state
/// between consecutive scene-based tests.
/// </summary>
/// <remarks>
/// Bug: When an <see cref="InputActionMap"/> belonging to the project-wide <see cref="InputSystem.actions"/>
/// asset is enabled before a test's <see cref="InputTestFixture.Setup"/> runs, the TearDown/Restore
/// cycle leaves the action map disconnected from its saved <see cref="InputActionState"/>.
///
/// Root cause:
/// 1. <c>TestHook_DisableActions()</c> calls <c>Disable()</c> + <c>OnSetupChanged()</c> on the
///    project-wide asset's maps. <c>Disable()</c> modifies the action state memory (phases → Disabled)
///    on the <em>same</em> InputActionState objects that were just saved in the snapshot, because
///    <c>Disable()</c> is called after <c>SaveAndResetState()</c> but references the same managed objects.
/// 2. <c>OnSetupChanged()</c> sets <c>map.m_State = null</c>, disconnecting the map from its state.
/// 3. <c>Restore()</c> restores <c>s_GlobalState</c> (the registry) but does NOT restore the per-map
///    back-references (<c>InputActionMap.m_State</c>, <c>m_MapIndexInState</c>) or the action phase
///    memory.
///
/// The fix:
/// 1. <c>TestHook_DisableActions()</c> should disconnect maps without modifying the saved state's memory,
///    so the saved snapshot retains the correct enabled phases.
/// 2. <c>Restore()</c> should re-link the back-references after <c>RestoreSavedState()</c> and
///    recompute <c>m_EnabledActionsCount</c> from the restored action phases.
/// </remarks>
internal class InputTestFixtureTeardownTests : InputTestFixture
{
    // Simulates a scene with a PlayerInput component using project-wide actions.
    // Created once before any [SetUp] runs, like a scene's OnEnable().
    private InputActionAsset m_PreTestAsset;
    private InputActionMap m_PreTestMap;
    private InputAction m_PreTestAction;

    [OneTimeSetUp]
    public void SimulateSceneLoad()
    {
        // Create an action asset simulating project-wide actions that a scene's
        // PlayerInput component would use.
        m_PreTestAsset = ScriptableObject.CreateInstance<InputActionAsset>();
        m_PreTestAsset.hideFlags = HideFlags.HideAndDontSave;
        m_PreTestMap = m_PreTestAsset.AddActionMap("Player");
        m_PreTestAction = m_PreTestMap.AddAction("Fire", InputActionType.Button);

        // Enable the map - creates an InputActionState and registers it in s_GlobalState.
        // This simulates PlayerInput.ActivateInput() running before the test starts.
        m_PreTestMap.Enable();

        // Register as project-wide actions on the real manager.
        // This causes TestHook_DisableActions() to treat our asset like project-wide actions
        // (i.e. call Disable() + OnSetupChanged() on it during each Setup()).
        InputSystem.s_Manager.actions = m_PreTestAsset;
    }

    [OneTimeTearDown]
    public void VerifyPreTestStatePreserved()
    {
        // After all test TearDowns, the action map should still be linked to its saved
        // InputActionState and the enabled count should reflect the pre-test enabled state.
        //
        // With the bug: map.m_State is null and map.enabled is false because:
        //   - TestHook_DisableActions() called Disable() on the map (clearing enabled state
        //     in the shared state memory) then OnSetupChanged() (setting m_State = null)
        //   - Restore() restores s_GlobalState but not map.m_State or m_EnabledActionsCount
        //
        // With the fix: map.m_State is properly re-linked and map.enabled is true because:
        //   - TestHook_DisableActions() no longer modifies the saved state's memory
        //   - Restore() calls RelinkRestoredStates() to restore back-references and
        //     recompute m_EnabledActionsCount from the action phase memory

        Assert.That(m_PreTestMap.m_State, Is.Not.Null,
            "Action map should be linked to its saved InputActionState after Restore(). " +
            "m_State was cleared by OnSetupChanged() during TestHook_DisableActions() " +
            "and was not re-linked by Restore().");

        Assert.That(m_PreTestMap.enabled, Is.True,
            "Action map should be enabled after Restore(): it was enabled before any test ran " +
            "and should be restored to that enabled state after TearDown().");

        // Clean up
        InputSystem.s_Manager.actions = null;
        Object.DestroyImmediate(m_PreTestAsset);
    }

    [Test]
    [Order(1)]
    public void TearDown_FirstTest_ProjectWideActionsAreReenabledForTest()
    {
        // During this test, project-wide actions may have been re-enabled by TestHook_EnableActions
        // (or left disabled if TestHook_EnableActions is a no-op for the test manager).
        // We're just running to trigger a Setup/TearDown cycle.
        Assert.Pass("First test ran successfully (triggering Setup/TearDown cycle)");
    }

    [Test]
    [Order(2)]
    public void TearDown_SecondTest_StateRemainsCorrectAfterSecondCycle()
    {
        // After the first test's TearDown() + this Setup(), verify setup completes without errors.
        // The [OneTimeTearDown] contains the actual assertion for the post-Restore() state.
        Assert.Pass("Second test ran successfully (triggering second Setup/TearDown cycle)");
    }
}
