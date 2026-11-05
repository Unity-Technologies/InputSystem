using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>
/// Tests for the custom inspector of the PlayerInput component.
/// </summary>
internal sealed class PlayerInputEditorTests
{
    private GameObject go;
    private PlayerInput playerInput;
    private InputActionAsset actions;
    private PlayerInputEditor editor;
    private EditorWindow window;
    private bool onGuiCalled;

    private class TestHostWindow : EditorWindow {}

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("Host");
        playerInput = go.AddComponent<PlayerInput>();
        actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(PlayerInputEditor.kDefaultInputActionsAssetPath);
        window = EditorWindow.GetWindow<TestHostWindow>();
    }

    [TearDown]
    public void TearDown()
    {
        if (editor != null)
            Object.DestroyImmediate(editor);
        window.Close();
        Undo.ClearUndo(playerInput);
        Object.DestroyImmediate(go);
    }

    private IEnumerator RenderInspector()
    {
        onGuiCalled = false;

        var container = new IMGUIContainer(() =>
        {
            // Like the Inspector window, pick up changes made to the target outside the editor (e.g. undo).
            editor.serializedObject.UpdateIfRequiredOrScript();
            editor.OnInspectorGUI();
            onGuiCalled = true;
        });

        window.rootVisualElement.Add(container);
        window.Repaint();

        var timeoutTime = Time.realtimeSinceStartupAsDouble + 3.0;
        do
        {
            yield return null; // let Unity process a frame
        }
        while (!onGuiCalled && Time.realtimeSinceStartupAsDouble < timeoutTime);

        container.RemoveFromHierarchy();
        Assert.That(onGuiCalled, Is.True, "Inspector GUI was not drawn");
    }

    // Replaces the actions of the asset with the given ones in a single map, the way a reimport does. The IDs are
    // derived from the position, so actions keep their ID across calls.
    private static void LoadActions(InputActionAsset asset, params string[] actionNames)
    {
        var actionsJson = actionNames.Select((name, i) =>
            $"{{\"name\":\"{name}\",\"type\":\"Button\",\"id\":\"00000000-0000-0000-0000-{i + 1:D12}\"}}");
        asset.LoadFromJson(
            $"{{\"maps\":[{{\"name\":\"map\",\"actions\":[{string.Join(",", actionsJson)}],\"bindings\":[]}}]}}");
    }

    // UUM-150956: switching to InvokeUnityEvents on a freshly added component did not show the per-action events
    // until the inspector was recreated.
    [UnityTest]
    public IEnumerator ActionEventsAreListed_WhenBehaviorIsSwitchedToInvokeUnityEvents(
        [Values(PlayerNotifications.SendMessages, PlayerNotifications.BroadcastMessages, PlayerNotifications.InvokeCSharpEvents)]
        PlayerNotifications initialBehavior)
    {
        Assert.That(actions, Is.Not.Null);

        playerInput.m_Actions = actions;
        playerInput.m_NotificationBehavior = initialBehavior;
        editor = (PlayerInputEditor)UnityEditor.Editor.CreateEditor(playerInput);

        yield return RenderInspector();
        Assert.That(playerInput.m_ActionEvents, Is.Null.Or.Empty);

        editor.serializedObject.FindProperty(nameof(PlayerInput.m_NotificationBehavior)).intValue =
            (int)PlayerNotifications.InvokeUnityEvents;
        editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();

        yield return RenderInspector();

        Assert.That(playerInput.m_ActionEvents.Select(e => e.actionId),
            Is.EquivalentTo(actions.Select(a => a.id.ToString())));
    }

    [UnityTest]
    public IEnumerator ActionEventsFollowActionsAsset_WhenSwitchingBackToInvokeUnityEvents()
    {
        Assert.That(actions, Is.Not.Null);

        playerInput.m_Actions = actions;
        playerInput.m_NotificationBehavior = PlayerNotifications.InvokeUnityEvents;
        editor = (PlayerInputEditor)UnityEditor.Editor.CreateEditor(playerInput);

        yield return RenderInspector();
        Assert.That(playerInput.m_ActionEvents, Is.Not.Empty);

        // Switch away, swap in an asset with different actions, then switch back.
        var otherActions = ScriptableObject.CreateInstance<InputActionAsset>();
        try
        {
            otherActions.AddActionMap("map").AddAction("other");

            var behaviorProperty = editor.serializedObject.FindProperty(nameof(PlayerInput.m_NotificationBehavior));
            behaviorProperty.intValue = (int)PlayerNotifications.SendMessages;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return RenderInspector();

            editor.serializedObject.FindProperty(nameof(PlayerInput.m_Actions)).objectReferenceValue = otherActions;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return RenderInspector();

            behaviorProperty.intValue = (int)PlayerNotifications.InvokeUnityEvents;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return RenderInspector();

            Assert.That(playerInput.m_ActionEvents.Select(e => e.actionId),
                Is.EquivalentTo(otherActions.Select(a => a.id.ToString())));
        }
        finally
        {
            Object.DestroyImmediate(otherActions);
        }
    }

    // Leaving InvokeUnityEvents and coming back with the same asset must pick up actions added in the meantime.
    [UnityTest]
    public IEnumerator ActionEventsFollowActionsAsset_WhenReenteringInvokeUnityEventsWithSameAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        try
        {
            LoadActions(asset, "first");

            playerInput.m_Actions = asset;
            playerInput.m_NotificationBehavior = PlayerNotifications.InvokeUnityEvents;
            editor = (PlayerInputEditor)UnityEditor.Editor.CreateEditor(playerInput);
            yield return RenderInspector();

            var behaviorProperty = editor.serializedObject.FindProperty(nameof(PlayerInput.m_NotificationBehavior));
            behaviorProperty.intValue = (int)PlayerNotifications.SendMessages;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return RenderInspector();

            LoadActions(asset, "first", "second");

            behaviorProperty.intValue = (int)PlayerNotifications.InvokeUnityEvents;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            yield return RenderInspector();

            Assert.That(playerInput.m_ActionEvents.Select(e => e.actionId),
                Is.EquivalentTo(asset.Select(a => a.id.ToString())));
        }
        finally
        {
            Object.DestroyImmediate(asset);
        }
    }

    // Behavior changes made through undo/redo don't go through the inspector's dropdown.
    [UnityTest]
    public IEnumerator ActionEventsAreListed_WhenInvokeUnityEventsIsRestoredByUndoRedo()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        try
        {
            LoadActions(asset, "first");

            playerInput.m_Actions = asset;
            playerInput.m_NotificationBehavior = PlayerNotifications.SendMessages;
            editor = (PlayerInputEditor)UnityEditor.Editor.CreateEditor(playerInput);
            yield return RenderInspector();

            Undo.IncrementCurrentGroup();
            editor.serializedObject.FindProperty(nameof(PlayerInput.m_NotificationBehavior)).intValue =
                (int)PlayerNotifications.InvokeUnityEvents;
            editor.serializedObject.ApplyModifiedProperties();
            Undo.IncrementCurrentGroup();
            yield return RenderInspector();

            Assert.That(playerInput.m_ActionEvents.Select(e => e.actionId),
                Is.EquivalentTo(asset.Select(a => a.id.ToString())));

            Undo.PerformUndo();
            yield return RenderInspector();
            Assert.That(playerInput.m_NotificationBehavior, Is.EqualTo(PlayerNotifications.SendMessages));

            LoadActions(asset, "first", "second");

            Undo.PerformRedo();
            yield return RenderInspector();
            Assert.That(playerInput.m_NotificationBehavior, Is.EqualTo(PlayerNotifications.InvokeUnityEvents));

            Assert.That(playerInput.m_ActionEvents.Select(e => e.actionId),
                Is.EquivalentTo(asset.Select(a => a.id.ToString())));
        }
        finally
        {
            Object.DestroyImmediate(asset);
        }
    }
}
