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

public class InputActionsEditorTests
{
    #region setup and teardown
    InputActionsEditorWindow m_Window;
    InputActionAsset m_Asset;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestUtils.MockDialogs();
        m_Asset = AssetDatabaseUtils.CreateAsset<InputActionAsset>();
        m_Asset.AddActionMap("First Name");
        m_Asset.AddActionMap("Second Name");
        m_Asset.AddActionMap("Third Name");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        AssetDatabaseUtils.Restore();
        TestUtils.RestoreDialogs();
    }

    [UnitySetUp]
    public IEnumerator UnitySetup()
    {
        var editor = InputActionsEditorWindow.OpenEditor(m_Asset);
        m_Window = editor;
        yield return WaitForNotDirty(editor.rootVisualElement);
    }

    [TearDown]
    public void TearDown()
    {
        m_Window?.Close();
    }

    #endregion

    #region Helper methods

    void Click(VisualElement ve)
    {
        Event evtd = new Event();
        evtd.type = EventType.MouseDown;
        evtd.mousePosition = ve.worldBound.center;
        evtd.clickCount = 1;
        using var pde = PointerDownEvent.GetPooled(evtd);
        ve.SendEvent(pde);

        Event evtu = new Event();
        evtu.type = EventType.MouseUp;
        evtu.mousePosition = ve.worldBound.center;
        evtu.clickCount = 1;
        using var pue = PointerUpEvent.GetPooled(evtu);
        ve.SendEvent(pue);
    }

    void SendText(VisualElement ve, string text, bool sendReturn = true)
    {
        foreach (var character in text)
        {
            var evtd = new Event() { type = EventType.KeyDown, keyCode = KeyCode.None, character = character };
            using var kde = KeyDownEvent.GetPooled(evtd);
            ve.SendEvent(kde);

            var evtu = new Event() { type = EventType.KeyUp, keyCode = KeyCode.None, character = character };
            using var kue = KeyUpEvent.GetPooled(evtu);
            ve.SendEvent(kue);
        }
        if (sendReturn)
        {
            SendReturn(ve);
        }
    }

    void SendReturn(VisualElement ve)
    {
        var evtd = new Event() { type = EventType.KeyDown, keyCode = KeyCode.Return };
        using var kde = KeyDownEvent.GetPooled(evtd);
        var evtd2 = new Event() { type = EventType.KeyDown, keyCode = KeyCode.None, character = '\n' };
        using var kde2 = KeyDownEvent.GetPooled(evtd2);
        ve.SendEvent(kde);
        ve.SendEvent(kde2);

        var evtu = new Event() { type = EventType.KeyUp, keyCode = KeyCode.Return };
        using var kue = KeyUpEvent.GetPooled(evtu);
        ve.SendEvent(kue);
    }

    void SendDeleteCommand(VisualElement ve)
    {
        var evt = new Event() { type = EventType.ExecuteCommand, commandName = "Delete" };
        using var ce = ExecuteCommandEvent.GetPooled(evt);
        ve.SendEvent(ce);
    }

    IEnumerator WaitForFocus(VisualElement ve, double timeoutSecs = 5.0)
    {
        return WaitUntil(() => ve.focusController.focusedElement == ve, "WaitForFocus", timeoutSecs);
    }

    IEnumerator WaitForNotDirty(VisualElement ve, double timeoutSecs = 5.0)
    {
        return WaitUntil(() => ve.panel.isDirty == false, "WaitForNotDirty", timeoutSecs);
    }

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

    // Wait until the action is true or the timeout is reached
    IEnumerator WaitUntil(Func<bool> action, string assertMessage, double timeoutSecs = 5.0)
    {
        var endTime = EditorApplication.timeSinceStartup + timeoutSecs;
        do
        {
            if (action()) yield break;
            yield return null;
        }
        while (endTime > EditorApplication.timeSinceStartup);
        Assert.That(action(), assertMessage);
    }

    #endregion

    [Test]
    public void CanListActionMaps()
    {
        var actionMapsContainer = m_Window.rootVisualElement.Q("action-maps-container");
        Assert.That(actionMapsContainer, Is.Not.Null);
        var actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assert.That(actionMapItem, Is.Not.Null);
        Assert.That(actionMapItem.Count, Is.EqualTo(3));
        Assert.That(m_Window.currentAssetInEdition.actionMaps[0].name, Is.EqualTo("First Name"));
        Assert.That(m_Window.currentAssetInEdition.actionMaps[1].name, Is.EqualTo("Second Name"));
        Assert.That(m_Window.currentAssetInEdition.actionMaps[2].name, Is.EqualTo("Third Name"));
    }

    [UnityTest]
    public IEnumerator CanCreateActionMap()
    {
        var button = m_Window.rootVisualElement.Q<Button>("add-new-action-map-button");
        Assume.That(button, Is.Not.Null);
        Click(button);

        // Wait for the focus to move out the button and start new ActionMaps editon
        yield return WaitForActionMapRename(3, isActive: true);

        // Rename the new action map
        SendText(m_Window.rootVisualElement, "New Name");

        // wait for the edition to end
        yield return WaitForActionMapRename(3, isActive: false);

        // Check on the UI side
        var actionMapsContainer = m_Window.rootVisualElement.Q("action-maps-container");
        var actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assert.That(actionMapItem, Is.Not.Null);
        Assert.That(actionMapItem.Count, Is.EqualTo(4));
        Assert.That(actionMapItem[3].Q<Label>("name").text, Is.EqualTo("New Name"));

        // Check on the asset side
        Assert.That(m_Window.currentAssetInEdition.actionMaps.Count, Is.EqualTo(4));
        Assert.That(m_Window.currentAssetInEdition.actionMaps[3].name, Is.EqualTo("New Name"));
    }

    [UnityTest]
    public IEnumerator CanRenameActionMap()
    {
        var actionMapsContainer = m_Window.rootVisualElement.Q("action-maps-container");
        var actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assume.That(actionMapItem[1].Q<Label>("name").text, Is.EqualTo("Second Name"));

        // for the selection the prevent some instabilities with current ui intregration
        m_Window.rootVisualElement.Q<ListView>("action-maps-list-view").Focus();
        m_Window.rootVisualElement.Q<ListView>("action-maps-list-view").selectedIndex = 1;

        yield return WaitForNotDirty(actionMapsContainer);
        yield return WaitForFocus(m_Window.rootVisualElement.Q("action-maps-list-view"));

        // refetch the action map item since the ui may have refreshed.
        actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();

        // clic twice to start the rename
        Click(actionMapItem[1]);
        Click(actionMapItem[1]);

        yield return WaitForActionMapRename(1, isActive: true);

        // Rename the new action map
        SendText(actionMapItem[1], "New Name");// .Q<TextField>())

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
        Assert.That(m_Window.currentAssetInEdition.actionMaps.Count, Is.EqualTo(3));
        Assert.That(m_Window.currentAssetInEdition.actionMaps[1].name, Is.EqualTo("New Name"));
    }

    [UnityTest]
    public IEnumerator CanDeleteActionMap()
    {
        var actionMapsContainer = m_Window.rootVisualElement.Q("action-maps-container");
        var actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assume.That(actionMapItem[1].Q<Label>("name").text, Is.EqualTo("Second Name"));

        // Select the second element
        Click(actionMapItem[1]);

        yield return WaitForFocus(m_Window.rootVisualElement.Q("action-maps-list-view"));

        SendDeleteCommand(m_Window.rootVisualElement);

        yield return WaitUntil(() => actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList().Count == 2, "wait for element to be deleted");

        // Check on the UI side
        actionMapItem = actionMapsContainer.Query<InputActionMapsTreeViewItem>().ToList();
        Assert.That(actionMapItem.Count, Is.EqualTo(2));
        Assert.That(actionMapItem[0].Q<Label>("name").text, Is.EqualTo("First Name"));
        Assert.That(actionMapItem[1].Q<Label>("name").text, Is.EqualTo("Third Name"));

        // Check on the asset side
        Assert.That(m_Window.currentAssetInEdition.actionMaps.Count, Is.EqualTo(2));
        Assert.That(m_Window.currentAssetInEdition.actionMaps[0].name, Is.EqualTo("First Name"));
        Assert.That(m_Window.currentAssetInEdition.actionMaps[1].name, Is.EqualTo("Third Name"));
    }
}
#endif
