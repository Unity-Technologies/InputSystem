// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS && UNITY_6000_0_OR_NEWER

using NUnit.Framework;
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

internal class InputActionsEditorTests : UIToolkitBaseTestWindow<InputActionsEditorWindow>
{
    #region setup and teardown
    InputActionAsset m_Asset;

    public override void OneTimeSetUp()
    {
        base.OneTimeSetUp();
        m_Asset = AssetDatabaseUtils.CreateAsset<InputActionAsset>();
        var actionMap = m_Asset.AddActionMap("First Name");
        m_Asset.AddActionMap("Second Name");
        m_Asset.AddActionMap("Third Name");

        actionMap.AddAction("Action One");
        actionMap.AddAction("Action Two");
    }

    public override void OneTimeTearDown()
    {
        AssetDatabaseUtils.Restore();
        base.OneTimeTearDown();
    }

    public override IEnumerator UnitySetup()
    {
        m_Window = InputActionsEditorWindow.OpenEditor(m_Asset);
        yield return base.UnitySetup();
    }

    #endregion

    #region Helper methods

    IEnumerator WaitForActionMapRename(int index, bool isActive, double timeoutSecs = 5.0)
    {
        return WaitUntil(() =>
        {
            var actionMapItems = m_Window.rootVisualElement.Q("action-maps-container").Query<InputActionMapsTreeViewItem>().ToList();
            if (actionMapItems.Count > index && actionMapItems[index].IsFocused == isActive)
            {
                return true;
            }
            return false;
        }, $"WaitForActionMapRename {index} {isActive}", timeoutSecs);
    }

    IEnumerator WaitForActionRename(int index, bool isActive, double timeoutSecs = 5.0)
    {
        return WaitUntil(() =>
        {
            var actionItems = m_Window.rootVisualElement.Q("actions-container").Query<InputActionsTreeViewItem>().ToList();
            if (actionItems.Count > index && actionItems[index].IsFocused == isActive)
            {
                return true;
            }
            return false;
        }, $"WaitForActionRename {index} {isActive}", timeoutSecs);
    }

    #endregion

    [Test]
    [Ignore("Instability, see ISXB-1284")]
    public void CanListActionMaps()
    {
        var actionMapsContainer = m_Window.rootVisualElement.Q("action-maps-container");
        Assert.That(actionMapsContainer, Is.Not.Null);
        var actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assert.That(actionMapItem, Is.Not.Null);
        Assert.That(actionMapItem.Count, Is.EqualTo(3));
        Assert.That(m_Window.currentAssetInEditor.actionMaps[0].name, Is.EqualTo("First Name"));
        Assert.That(m_Window.currentAssetInEditor.actionMaps[1].name, Is.EqualTo("Second Name"));
        Assert.That(m_Window.currentAssetInEditor.actionMaps[2].name, Is.EqualTo("Third Name"));
    }

    [UnityTest]
    [Ignore("Instability, see ISXB-1284")]
    public IEnumerator CanCreateActionMap()
    {
        var button = m_Window.rootVisualElement.Q<Button>("add-new-action-map-button");
        Assume.That(button, Is.Not.Null);
        SimulateClickOn(button);

        // Wait for the focus to move out the button and start new ActionMaps editon
        yield return WaitForActionMapRename(3, isActive: true);

        // Rename the new action map
        SimulateTypingText("New Name");

        // wait for the edition to end
        yield return WaitForActionMapRename(3, isActive: false);

        // Check on the UI side
        var actionMapsContainer = m_Window.rootVisualElement.Q("action-maps-container");
        var actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assert.That(actionMapItem, Is.Not.Null);
        Assert.That(actionMapItem.Count, Is.EqualTo(4));
        Assert.That(actionMapItem[3].Q<Label>("name").text, Is.EqualTo("New Name"));

        // Check on the asset side
        Assert.That(m_Window.currentAssetInEditor.actionMaps.Count, Is.EqualTo(4));
        Assert.That(m_Window.currentAssetInEditor.actionMaps[3].name, Is.EqualTo("New Name"));
    }

    [UnityTest]
    [Ignore("Instability, see ISXB-1284")]
    public IEnumerator CanRenameActionMap()
    {
        var actionMapsContainer = m_Window.rootVisualElement.Q("action-maps-container");
        var actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assume.That(actionMapItem[1].Q<Label>("name").text, Is.EqualTo("Second Name"));

        // for the selection the prevent some instabilities with current ui intregration
        m_Window.rootVisualElement.Q<ListView>("action-maps-list-view").Focus();
        m_Window.rootVisualElement.Q<ListView>("action-maps-list-view").selectedIndex = 1;

        // changing the selection triggers a state change, wait for the scheduler to process the frame
        yield return WaitForSchedulerLoop();
        yield return WaitForNotDirty();
        yield return WaitForFocus(m_Window.rootVisualElement.Q("action-maps-list-view"));

        // refetch the action map item since the ui may have refreshed.
        actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();

        // clic twice to start the rename
        SimulateClickOn(actionMapItem[1]);
        // in case of the item is already focused, don't click again
        if (!actionMapItem[1].IsFocused)
        {
            SimulateClickOn(actionMapItem[1]);
        }

        yield return WaitForActionMapRename(1, isActive: true);

        // Rename the new action map
        SimulateTypingText("New Name");

        // wait for the edition to end
        yield return WaitForActionMapRename(1, isActive: false);

        // Check on the UI side
        actionMapsContainer = m_Window.rootVisualElement.Q("action-maps-container");
        Assume.That(actionMapsContainer, Is.Not.Null);
        actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assert.That(actionMapItem, Is.Not.Null);
        Assert.That(actionMapItem.Count, Is.EqualTo(3));
        Assert.That(actionMapItem[1].Q<Label>("name").text, Is.EqualTo("New Name"));

        // Check on the asset side
        Assert.That(m_Window.currentAssetInEditor.actionMaps.Count, Is.EqualTo(3));
        Assert.That(m_Window.currentAssetInEditor.actionMaps[1].name, Is.EqualTo("New Name"));
    }

    [UnityTest]
    [Ignore("Instability, see ISXB-1284")]
    public IEnumerator CanDeleteActionMap()
    {
        var actionMapsContainer = m_Window.rootVisualElement.Q("action-maps-container");
        var actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assume.That(actionMapItem[1].Q<Label>("name").text, Is.EqualTo("Second Name"));

        // Select the second element
        SimulateClickOn(actionMapItem[1]);

        yield return WaitForFocus(m_Window.rootVisualElement.Q("action-maps-list-view"));

        SimulateDeleteCommand();

        yield return WaitUntil(() => actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList().Count == 2, "wait for element to be deleted");

        // Check on the UI side
        actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assert.That(actionMapItem.Count, Is.EqualTo(2));
        Assert.That(actionMapItem[0].Q<Label>("name").text, Is.EqualTo("First Name"));
        Assert.That(actionMapItem[1].Q<Label>("name").text, Is.EqualTo("Third Name"));

        // Check on the asset side
        Assert.That(m_Window.currentAssetInEditor.actionMaps.Count, Is.EqualTo(2));
        Assert.That(m_Window.currentAssetInEditor.actionMaps[0].name, Is.EqualTo("First Name"));
        Assert.That(m_Window.currentAssetInEditor.actionMaps[1].name, Is.EqualTo("Third Name"));
    }

    [UnityTest]
    [Ignore("Instability, see ISXB-1284")]
    public IEnumerator CanRenameAction()
    {
        var actionContainer = m_Window.rootVisualElement.Q("actions-container");
        var actionItem = actionContainer.Query<InputActionsTreeViewItem>().ToList();
        Assume.That(actionItem[1].Q<Label>("name").text, Is.EqualTo("Action Two"));

        m_Window.rootVisualElement.Q<TreeView>("actions-tree-view").Focus();
        m_Window.rootVisualElement.Q<TreeView>("actions-tree-view").selectedIndex = 1;

        // Selection change triggers a state change, wait for the scheduler to process the frame
        yield return WaitForSchedulerLoop();
        yield return WaitForNotDirty();
        yield return WaitForFocus(m_Window.rootVisualElement.Q<TreeView>("actions-tree-view"));

        // Re-fetch the actions since the UI may have refreshed.
        actionItem = actionContainer.Query<InputActionsTreeViewItem>().ToList();

        // Click twice to start the rename
        SimulateClickOn(actionItem[1]);
        // If the item is already focused, don't click again
        if (!actionItem[1].IsFocused)
        {
            SimulateClickOn(actionItem[1]);
        }

        yield return WaitForActionRename(1, isActive: true);

        // Rename the action
        SimulateTypingText("New Name");

        // Wait for rename to end and focus to return from text field
        yield return WaitForSchedulerLoop();
        yield return WaitForFocus(m_Window.rootVisualElement.Q<TreeView>("actions-tree-view"));

        // Check on the UI side
        actionContainer = m_Window.rootVisualElement.Q("actions-container");
        Assume.That(actionContainer, Is.Not.Null);
        actionItem = actionContainer.Query<InputActionsTreeViewItem>().ToList();
        Assert.That(actionItem, Is.Not.Null);
        Assert.That(actionItem.Count, Is.EqualTo(2));
        Assert.That(actionItem[1].Q<Label>("name").text, Is.EqualTo("New Name"));

        // Check on the asset side
        Assert.That(m_Window.currentAssetInEditor.actionMaps[0].actions[1].name, Is.EqualTo("New Name"));
    }
}
#endif
