using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Editor;

public class AssetEditorTests
{
    static AssetInspectorWindow GetTestAssetWindow()
    {
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Packages/com.unity.inputsystem/Tests/InputSystem/Editor/TestAsset.inputactions");
        AssetInspectorWindow.OnOpenAsset(asset.GetInstanceID(), -1);
        var w = Resources.FindObjectsOfTypeAll<AssetInspectorWindow>()[0];
        return w;
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (var w in Resources.FindObjectsOfTypeAll<AssetInspectorWindow>())
        {
            w.CloseWithoutSaving();
        }
    }

    [Test]
    public void FilteringByName()
    {
        var assetWindow = GetTestAssetWindow();

        assetWindow.m_InputActionWindowToolbar.m_SearchText = "mix";
        assetWindow.m_InputActionWindowToolbar.OnSearchChanged("mix");

        Assert.That(assetWindow.m_ActionsTree.GetRootElement().children, Has.Count.EqualTo(1));
        Assert.That(assetWindow.m_ActionsTree.GetRootElement().children[0].displayName, Is.EqualTo("Action with mixed bindings"));
        Assert.That(assetWindow.m_ActionsTree.GetRootElement().children[0].children, Has.Count.EqualTo(3));
    }

    [Test]
    public void FilteringByGroup()
    {
        var assetWindow = GetTestAssetWindow();

        // Select "Keyboard and mouse" scheme
        assetWindow.m_InputActionWindowToolbar.OnControlSchemeSelected(0);

        // All 3 actions are visible (including those without any bindings)
        Assert.That(assetWindow.m_ActionsTree.GetRootElement().children, Has.Count.EqualTo(3));
        //Only bindings matching the filter are visible
        Assert.That(assetWindow.m_ActionsTree.GetRootElement().children[1].children, Has.Count.EqualTo(2));
    }

    [Test]
    public void FilteringByGroupAndDevice()
    {
        var assetWindow = GetTestAssetWindow();

        // Select "Keyboard and mouse" scheme
        assetWindow.m_InputActionWindowToolbar.OnControlSchemeSelected(0);
        // Select "Mouse" device
        assetWindow.m_InputActionWindowToolbar.OnDeviceSelected(1);

        // All 3 actions are visible (including those without any bindings)
        Assert.That(assetWindow.m_ActionsTree.GetRootElement().children, Has.Count.EqualTo(3));
        //Only bindings matching the filters are visible
        Assert.That(assetWindow.m_ActionsTree.GetRootElement().children[0].children, Has.Count.EqualTo(1));
        Assert.That(assetWindow.m_ActionsTree.GetRootElement().children[1].children, Has.Count.EqualTo(1));
    }

    [Test]
    public void FilteringByGroupDeviceAndName()
    {
        var assetWindow = GetTestAssetWindow();

        assetWindow.m_InputActionWindowToolbar.m_SearchText = "right";
        assetWindow.m_InputActionWindowToolbar.OnSearchChanged("right");

        // Select "Keyboard and mouse" scheme
        assetWindow.m_InputActionWindowToolbar.OnControlSchemeSelected(0);
        // Select "Mouse" device
        assetWindow.m_InputActionWindowToolbar.OnDeviceSelected(1);

        // All 3 actions are visible (including those without any bindings)
        Assert.That(assetWindow.m_ActionsTree.GetRootElement().children, Has.Count.EqualTo(1));
        //Only bindings matching the filters are visible
        Assert.That(assetWindow.m_ActionsTree.GetRootElement().children[0].children, Has.Count.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator NewBindingWithGroupFilter()
    {
        var assetWindow = GetTestAssetWindow();

        // Select "Keyboard and mouse" scheme
        assetWindow.m_InputActionWindowToolbar.OnControlSchemeSelected(0);

        Assert.That(assetWindow.m_ActionsTree.GetRootElement().children[0].children, Has.Count.EqualTo(2));

        assetWindow.m_ContextMenu.OnAddBinding(assetWindow.m_ActionsTree.GetRootElement().children[0]);
        yield return null;

        //Only bindings matching the filter are visible
        Assert.That(assetWindow.m_ActionsTree.GetRootElement().children[0].children, Has.Count.EqualTo(3));
    }

    [UnityTest]
    public IEnumerator NewBindingIsSelected()
    {
        var assetWindow = GetTestAssetWindow();

        assetWindow.m_ContextMenu.OnAddBinding(assetWindow.m_ActionsTree.GetRootElement().children[0]);
        yield return null;

        // Is new binding selected
        var selectedRow = (BindingTreeItem)assetWindow.m_ActionsTree.GetSelectedRow();
        Assert.That(selectedRow.path, Is.Null.Or.Empty);
    }

    [UnityTest]
    public IEnumerator NewCompositeIsSelected()
    {
        var assetWindow = GetTestAssetWindow();

        var args = new object[2]
        {
            assetWindow.m_ActionsTree.GetRootElement().children[0],
            InputBindingComposite.s_Composites.names.First()
        };
        assetWindow.m_ContextMenu.OnAddCompositeBinding(args);
        yield return null;

        // Is new composite selected
        var selectedRow = (BindingTreeItem)assetWindow.m_ActionsTree.GetSelectedRow();
        Assert.That(selectedRow.path, Is.Null.Or.Empty);
    }

    [UnityTest]
    public IEnumerator NewActionMapIsSelected()
    {
        var assetWindow = GetTestAssetWindow();

        assetWindow.m_ContextMenu.OnAddActionMap();
        yield return null;

        // Is new composite selected
        var selectedRow = (ActionMapTreeItem)assetWindow.m_ActionMapsTree.GetSelectedRow();
        Assert.That(selectedRow.displayName, Is.EqualTo("default"));
    }

    [UnityTest]
    public IEnumerator NewActionIsSelected()
    {
        var assetWindow = GetTestAssetWindow();

        assetWindow.m_ContextMenu.OnAddAction();
        yield return null;

        // Is new composite selected
        var selectedRow = (ActionTreeItem)assetWindow.m_ActionsTree.GetSelectedRow();
        Assert.That(selectedRow.displayName, Is.EqualTo("action"));
    }

    [UnityTest]
    [Ignore("For some reason it's impossible to focus the tree view from the test")]
    public IEnumerator CanCopyAndPaste()
    {
        EditorUtility.ClearProgressBar();
        var assetWindow = GetTestAssetWindow();

        assetWindow.m_ActionsTree.SetSelection(new[] {assetWindow.m_ActionsTree.GetRootElement().children[1].id});

        var e = new Event();
        e.type = EventType.ExecuteCommand;
        e.commandName = "Copy";
        assetWindow.SendEvent(e);

        yield return null;

        assetWindow.m_ActionMapsTree.SetSelection(new[] {assetWindow.m_ActionMapsTree.GetRootElement().children[1].id});
        assetWindow.m_ActionMapsTree.OnSelectionChanged();
        e.type = EventType.ExecuteCommand;
        e.commandName = "Paste";
        assetWindow.SendEvent(e);
    }
}
