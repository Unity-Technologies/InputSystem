using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = System.Object;

partial class CoreTests
{
    private static InputActionAsset CreateAssetWithTwoActions()
    {
        var map1 = new InputActionMap("map1");
        map1.AddAction("action1");
        map1.AddAction("action2");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map1);
        return asset;
    }

    private static InputActionAsset CreateAssetWithSingleAction()
    {
        var map2 = new InputActionMap("map2");
        map2.AddAction("action3");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map2);
        return asset;
    }

    [Test]
    public void Actions_Reference_AssetAndActionsReturnsNull_IfNotSet()
    {
        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        Assert.That(reference.asset, Is.Null);
        Assert.That(reference.action, Is.Null);
        Assert.That(reference.ToDisplayName(), Is.Null);
    }

    [Test]
    public void Actions_Reference_SetNull()
    {
        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        reference.Set(null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_Reference_CanResolveAction()
    {
        var asset = CreateAssetWithTwoActions();
        var reference = ScriptableObject.CreateInstance<InputActionReference>();

        reference.Set(asset, "map1", "action2");

        Assert.That(reference.action, Is.SameAs(asset.FindAction("map1/action2")));
    }

    [Test]
    [Category("Actions")]
    public void Actions_Reference_CanResolveAction_EvenAfterActionHasBeenRenamed()
    {
        var map = new InputActionMap("map");
        var action = map.AddAction("oldName");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        reference.Set(asset, "map", "oldName");

        action.Rename("newName");

        var referencedAction = reference.action;

        Assert.That(referencedAction, Is.SameAs(action));
    }

    [Test(Description = "https://issuetracker.unity3d.com/product/unity/issues/guid/ISXB-1584")]
    [Category("Actions")]
    public void Actions_Reference_CanResolveActionAfterReassignment()
    {
        var asset1 = CreateAssetWithTwoActions();
        var asset2 = CreateAssetWithSingleAction();

        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        reference.Set(asset1, "map1", "action1");
        Assert.That(reference.action, Is.Not.Null);
        Assert.That(reference.action, Is.SameAs(asset1.FindAction("map1/action1"))); // Redundant, but important for test case
        Assert.That(reference.asset, Is.SameAs(asset1));
        Assert.That(reference.name, Is.EqualTo("map1/action1"));
        Assert.That(reference.ToDisplayName(), Is.EqualTo("map1/action1"));
        Assert.That(reference.ToString(), Is.EqualTo(":map1/action1"));

        reference.Set(asset2, "map2", "action3");
        Assert.That(reference.action, Is.Not.Null);
        Assert.That(reference.action, Is.SameAs(asset2.FindAction("map2/action3")));
        Assert.That(reference.asset, Is.SameAs(asset2));
        Assert.That(reference.name, Is.EqualTo("map2/action3"));
        Assert.That(reference.ToDisplayName(), Is.EqualTo("map2/action3"));
        Assert.That(reference.ToString(), Is.EqualTo(":map2/action3"));
    }

    [TestCase(typeof(InputAction))]
    [TestCase(typeof(InputActionMap))]
    [TestCase(typeof(InputActionAsset))]
    public void Actions_Reference_NameShouldReflectReferencedAction(Type typeToDelete)
    {
        var map = new InputActionMap("map");
        var action1 = map.AddAction("action1");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(map);

        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        reference.Set(asset, "map", "action1");

        Assert.That(reference.action, Is.SameAs(action1));
        Assert.That(reference.asset, Is.SameAs(asset));
        Assert.That(reference.name, Is.EqualTo("map/action1"));
        Assert.That(reference.ToDisplayName(), Is.EqualTo("map/action1"));
        Assert.That(reference.ToString(), Is.EqualTo(":map/action1"));

        // Delete the referenced action directly or indirectly
        if (typeToDelete == typeof(InputAction))
            asset.RemoveAction("map/action1");
        else if (typeToDelete == typeof(InputActionMap))
            asset.RemoveActionMap("map");
        else if (typeToDelete == typeof(InputActionAsset))
            UnityEngine.Object.DestroyImmediate(asset);

        // TODO reference need to react to this
        Assert.That(reference.action, Is.Null);
        Assert.That(reference.asset, Is.SameAs(asset));
        Assert.That(reference.name, Is.EqualTo("map/action1")); // Unexpected when no longer existing
        Assert.That(reference.ToDisplayName(), Is.EqualTo("map/action1")); // Unexpected when no longer existing
        //Assert.That(reference.ToString(), Is.EqualTo(":" + new Guid(action1.m_Id))); // Unexpected when no longer existing
    }

    // List of bugs addressed on this branch:
    // (FIXED) InputActionReference - Do not set m_Action when calling Set.
    // (FIXED) InputActionReference.asset - Returns incorrect asset after being .Set if .action is called before Set.
    // (FIXED) InputActionReference - Assigned with Set during play-mode corrupts InputActionAsset by mutating asset reference into pointing into potentially another asset and overwrites existign asset.
    // (FIXED) InputActionReferencePropertyDrawer - Assigns direct reference into InputActionAsset to InputActionReference fields leading to corruption.
    // InputActionReference - Contains stale reference and looks like a valid reference if the action is deleted. .action returns removed action.
    // InputActionReference - Do not invalidate if action, action map or asset is deleted/destroyed. .action still returns the action.
    // (FIXED) InputActionAsset.RemoveActionMap - Do not remove actions within the map and they keep a stale reference to the removed map.
    // (FIXED) InputActionAsset.RemoveAction - Throws exception if action do not have any bindings.


    // TODO Make a test where action map is deleted
    // TODO Make a test where asset is deleted
    // TODO Make an undo resolve test
}
