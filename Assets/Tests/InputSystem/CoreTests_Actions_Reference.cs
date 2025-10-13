using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

partial class CoreTests
{
    #region Helpers

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

    private void AssertDefaults(InputActionReference reference)
    {
        Assert.That(reference.asset, Is.Null);
        Assert.That(reference.action, Is.Null);
        Assert.That(reference.name, Is.EqualTo(string.Empty));
        Assert.That(reference.ToDisplayName(), Is.Null);
        Assert.That(reference.ToString(), Is.EqualTo($" ({typeof(InputActionReference).FullName})"));
    }

    #endregion

    [Test]
    [Category("Actions")]
    public void Actions_Reference_Defaults()
    {
        AssertDefaults(ScriptableObject.CreateInstance<InputActionReference>());
    }

    [TestCase(false, "map1", "action1")]
    [TestCase(true, null, "action1")]
    [TestCase(true, "map1", null)]
    [Category("Actions")]
    public void Actions_Reference_SetByNameThrows_IfAnyArgumentIsNull(bool validAsset, string mapName, string actionName)
    {
        var asset = CreateAssetWithTwoActions();
        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        Assert.Throws<ArgumentNullException>(() => reference.Set(validAsset ? asset : null, mapName, actionName));
    }

    [TestCase("doesNotExist", "action1")]
    [TestCase("map1", "doesNotExist")]
    [Category("Actions")]
    public void Actions_Reference_SetByNameThrows_IfNoMatchingMapOrActionExists(string mapName, string actionName)
    {
        var asset = CreateAssetWithTwoActions();
        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        Assert.Throws<ArgumentException>(() => reference.Set(asset, mapName, actionName));
    }

    [Test]
    [Category("Actions")]
    public void Actions_Reference_SetByReferenceThrows_IfActionDoNotBelongToAnAsset()
    {
        // Case 1: Action was never part of any asset
        var action = new InputAction("action");
        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        Assert.Throws<InvalidOperationException>(() => reference.Set(action));

        // Case 2: Action was part of an asset but then removed
        var asset = CreateAssetWithTwoActions();
        var action1 = asset.FindAction("map1/action1");
        asset.RemoveAction("map1/action1");
        Assert.Throws<InvalidOperationException>(() => reference.Set(action1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_Reference_CreateReturnsReference_WhenCreatedFromValidAction()
    {
        var asset = CreateAssetWithTwoActions();
        var action = asset.FindAction("map1/action2");

        var reference = InputActionReference.Create(action);
        Assert.That(reference.action, Is.SameAs(action));
    }

    [Test]
    [Category("Actions")]
    public void Actions_Reference_CreateReturnsNullReferenceObject_IfActionIsNull()
    {
        var reference = InputActionReference.Create(null);
        Assert.That(reference.action, Is.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    [Category("Actions")]
    public void Actions_Reference_CanResolveAction_WhenSet(bool setByReference)
    {
        var asset = CreateAssetWithTwoActions();
        var action = asset.FindAction("map1/action2");

        // Note that setting reference is allowed when its an in-memory object and not a reference object backed
        // by an asset.
        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        if (setByReference)
            reference.Set(action);
        else
            reference.Set(asset, "map1", "action2");

        Assert.That(reference.action, Is.SameAs(action));
    }

    [Test]
    [Category("Actions")]
    public void Actions_Reference_CanResolveAction_AfterActionHasBeenRenamed()
    {
        var asset = CreateAssetWithSingleAction();
        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        reference.Set(asset, "map2", "action3");
        var action = asset.FindAction("map2/action3");

        action.Rename("newName");
        Assert.That(reference.action, Is.SameAs(action));
    }

    [Test]
    [Category("Actions")]
    public void Actions_Reference_CanResolveToDefaultState_AfterActionHasBeenSetToNull()
    {
        var asset = CreateAssetWithTwoActions();
        var reference = InputActionReference.Create(asset.FindAction("map1/action1"));
        reference.Set(null);
        AssertDefaults(reference);
    }

    [Test(Description = "https://issuetracker.unity3d.com/product/unity/issues/guid/ISXB-1584")]
    [Category("Actions")]
    public void Actions_Reference_CanResolveActionAfterReassignmentToActionFromAnotherAsset()
    {
        var asset1 = CreateAssetWithTwoActions();
        var asset2 = CreateAssetWithSingleAction();

        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        reference.Set(asset1, "map1", "action1");
        Assert.That(reference.action, Is.Not.Null); // Looks redundant, but important for test case to resolve

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
    public void Actions_Reference_ShouldReevaluateIfAssociatedEntityIsDeleted(Type typeToDelete)
    {
        var asset = CreateAssetWithTwoActions();
        var action = asset.FindAction("map1/action1");

        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        reference.Set(asset, "map1", "action1");

        Assert.That(reference.action, Is.SameAs(action));
        Assert.That(reference.asset, Is.SameAs(asset));
        Assert.That(reference.name, Is.EqualTo("map1/action1"));
        Assert.That(reference.ToDisplayName(), Is.EqualTo("map1/action1"));
        Assert.That(reference.ToString(), Is.EqualTo(":map1/action1"));

        // Remove the referenced action directly or indirectly. Note that this doesn't destroy an action or action map
        // since they are regular reference types. However, an InputActionReference is based on asset an and action ID
        // So if a map is removed from an asset but remains valid in memory it is no longer a valid reference.
        if (typeToDelete == typeof(InputAction))
            asset.RemoveAction("map1/action1");
        else if (typeToDelete == typeof(InputActionMap))
            asset.RemoveActionMap("map1");
        else if (typeToDelete == typeof(InputActionAsset))
            UnityEngine.Object.DestroyImmediate(asset);

        Assert.That(reference.action, Is.Null);
        Assert.That(reference.asset, Is.SameAs(asset));
        Assert.That(reference.name, Is.EqualTo("map1/action1")); // Unexpected when no longer existing
        Assert.That(reference.ToDisplayName(), Is.EqualTo("map1/action1")); // Unexpected when no longer existing
        Assert.That(reference.ToString(), Is.EqualTo($"map1/action1 ({typeof(InputActionReference).FullName})")); // Unexpected when no longer existing
    }
}
