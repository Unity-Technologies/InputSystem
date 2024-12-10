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
        m_Asset.AddActionMap("First Name");
        m_Asset.AddActionMap("Second Name");
        m_Asset.AddActionMap("Third Name");
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
}
#endif
