// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;

internal class SelectorsTests
{
    [Test]
    [Category("AssetEditor")]
    public void GetActionsAsTreeViewData_ReturnsActionsAndBindingsAsTreeViewData()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var actionMap = asset.AddActionMap("ActionMap");
        var actionOne = actionMap.AddAction("Action1", binding: "<Keyboard>/a");
        actionOne.AddCompositeBinding("2DVector")
            .With("left", "<Gamepad>/rightStick/x")
            .With("right", "<Gamepad>/rightStick/y");
        var actionTwo = actionMap.AddAction("Action2", binding: "<Keyboard>/d");


        var treeViewData = Selectors.GetActionsAsTreeViewData(TestData.EditorStateWithAsset(asset).Generate());


        Assert.That(treeViewData.Count, Is.EqualTo(2));
        Assert.That(treeViewData[0].data.name, Is.EqualTo("Action1"));
        Assert.That(treeViewData[1].data.name, Is.EqualTo("Action2"));

        Assert.That(treeViewData[0].hasChildren, Is.True);
        Assert.That(treeViewData[0].children.ElementAt(0).data.name, Is.EqualTo(InputControlPath.ToHumanReadableString(actionOne.bindings[0].path)));
        Assert.That(treeViewData[0].children.ElementAt(1).data.name, Is.EqualTo("2DVector"));

        Assert.That(treeViewData[0].children.ElementAt(1).hasChildren, Is.True);
        Assert.That(treeViewData[0].children.ElementAt(1).children.ElementAt(0).data.name, Is.EqualTo("Left: " + InputControlPath.ToHumanReadableString("<Gamepad>/rightStick/x")));
        Assert.That(treeViewData[0].children.ElementAt(1).children.ElementAt(1).data.name, Is.EqualTo("Right: " + InputControlPath.ToHumanReadableString("<Gamepad>/rightStick/y")));


        Assert.That(treeViewData[1].hasChildren, Is.True);
        Assert.That(treeViewData[1].children.ElementAt(0).data.name, Is.EqualTo(InputControlPath.ToHumanReadableString(actionTwo.bindings[0].path)));
    }
}
#endif
