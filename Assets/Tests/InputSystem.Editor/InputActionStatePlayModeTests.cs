using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Enters real play mode so that the engine runs every RuntimeInitializeOnLoadMethod in its own order.
// This deliberately does not derive from InputTestFixture, which replaces the runtime and global state.
internal class InputActionStatePlayModeTests
{
    const string k_TestCategory = "Editor";
    const string k_ArmedKey = "InputSystem.Tests.InputActionStatePlayModeTests.Armed";
    const string k_EnabledAfterSceneLoadKey = "InputSystem.Tests.InputActionStatePlayModeTests.EnabledAfterSceneLoad";
    const string k_SavedOptionsKey = "InputSystem.Tests.InputActionStatePlayModeTests.SavedOptions";
    const string k_SavedOptionsEnabledKey = "InputSystem.Tests.InputActionStatePlayModeTests.SavedOptionsEnabled";

    static readonly bool[] k_DisableDomainReloadValues = { false, true };

    static InputAction s_EarlyAction;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void EnableActionBeforeSceneLoad()
    {
        if (!SessionState.GetBool(k_ArmedKey, false))
            return;

        s_EarlyAction = new InputAction(binding: "<Keyboard>/space");
        s_EarlyAction.Enable();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RecordActionAfterSceneLoad()
    {
        if (!SessionState.GetBool(k_ArmedKey, false))
            return;

        SessionState.SetBool(k_EnabledAfterSceneLoadKey, s_EarlyAction != null && s_EarlyAction.enabled);
    }

    [SetUp]
    public void SetUp()
    {
        // Each domain reload during the test runs SetUp again, so only record the options once.
        if (SessionState.GetInt(k_SavedOptionsKey, -1) != -1)
            return;

        // Unity 6000.0 reports no options while they are disabled, so enable them to read the stored ones.
        var optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        SessionState.SetInt(k_SavedOptionsKey, (int)EditorSettings.enterPlayModeOptions);
        SessionState.SetBool(k_SavedOptionsEnabledKey, optionsEnabled);
        EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
    }

    [TearDown]
    public void TearDown()
    {
        if (Application.isPlaying)
            return;

        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(k_SavedOptionsKey, 0);
        EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(k_SavedOptionsEnabledKey, false);

        // Entering play mode saves the modified settings to disk, so save the restored ones too.
        AssetDatabase.SaveAssets();
        SessionState.EraseInt(k_SavedOptionsKey);
        SessionState.EraseBool(k_SavedOptionsEnabledKey);
        SessionState.EraseBool(k_ArmedKey);
        SessionState.EraseBool(k_EnabledAfterSceneLoadKey);

        s_EarlyAction?.Dispose();
        s_EarlyAction = null;
    }

    static void SetDomainReloadDisabled(bool disabled)
    {
        // Unity 6000.0 ignores enterPlayModeOptions unless the options are enabled.
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = disabled
            ? EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload
            : EnterPlayModeOptions.None;
    }

    static bool HasOnActionChangeCallback(Action<object, InputActionChange> callback)
    {
        var callbacks = InputActionState.s_GlobalState.onActionChange;
        for (var i = 0; i < callbacks.length; ++i)
        {
            if (callbacks[i] == callback)
                return true;
        }
        return false;
    }

    [UnityTest]
    [Category(k_TestCategory)]
    [Description("https://jira.unity3d.com/browse/UUM-151403")]
    public IEnumerator Actions_EnabledBeforeSceneLoad_StayEnabled_WhenEnteringPlayMode(
        [ValueSource(nameof(k_DisableDomainReloadValues))] bool disableDomainReload)
    {
        SetDomainReloadDisabled(disableDomainReload);
        SessionState.SetBool(k_ArmedKey, true);

        yield return new EnterPlayMode();

        SessionState.SetBool(k_ArmedKey, false);
        Assert.That(SessionState.GetBool(k_EnabledAfterSceneLoadKey, false), Is.True);

        yield return new ExitPlayMode();

        // Without a domain reload the action object survives, but exiting play mode must still clean up its state.
        if (disableDomainReload)
            Assert.That(s_EarlyAction.enabled, Is.False);
    }

    [UnityTest]
    [Category(k_TestCategory)]
    [Description("https://jira.unity3d.com/browse/UUM-151403")]
    public IEnumerator Actions_OnActionChangeCallbacksAddedInPlayMode_AreRemovedOnExit_WhenDomainReloadIsDisabled()
    {
        SetDomainReloadDisabled(true);

        Action<object, InputActionChange> editModeCallback = (obj, change) => {};
        Action<object, InputActionChange> playModeCallback = (obj, change) => {};
        var survivingListener = ScriptableObject.CreateInstance<ActionChangeListener>();
        survivingListener.hideFlags = HideFlags.HideAndDontSave;
        var destroyedListener = ScriptableObject.CreateInstance<ActionChangeListener>();
        destroyedListener.hideFlags = HideFlags.HideAndDontSave;
        Action<object, InputActionChange> survivingListenerCallback = survivingListener.OnActionChange;
        Action<object, InputActionChange> destroyedListenerCallback = destroyedListener.OnActionChange;
        InputSystem.onActionChange += editModeCallback;
        try
        {
            yield return new EnterPlayMode();

            Assert.That(HasOnActionChangeCallback(editModeCallback), Is.True);
            InputSystem.onActionChange += playModeCallback;

            // Like an editor window opened during play mode, which stays open after it.
            InputSystem.onActionChange += survivingListenerCallback;

            // Like a component of a play mode GameObject, which is destroyed on exit.
            InputSystem.onActionChange += destroyedListenerCallback;
            Object.DestroyImmediate(destroyedListener);

            yield return new ExitPlayMode();

            Assert.That(HasOnActionChangeCallback(editModeCallback), Is.True);
            Assert.That(HasOnActionChangeCallback(playModeCallback), Is.False);
            Assert.That(HasOnActionChangeCallback(survivingListenerCallback), Is.True);
            Assert.That(HasOnActionChangeCallback(destroyedListenerCallback), Is.False);
        }
        finally
        {
            InputSystem.onActionChange -= editModeCallback;
            InputSystem.onActionChange -= playModeCallback;
            InputSystem.onActionChange -= survivingListenerCallback;
            InputSystem.onActionChange -= destroyedListenerCallback;
            Object.DestroyImmediate(survivingListener);
        }
    }

    class ActionChangeListener : ScriptableObject
    {
        public void OnActionChange(object actionOrMap, InputActionChange change)
        {
        }
    }
}
