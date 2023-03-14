using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using Input = UnityEngine.InputSystem.HighLevel.Input;

internal partial class CoreTests
{
    private string m_TemplateAssetPath;

    [SetUp]
    public override void Setup()
    {
        // this asset takes the place of InputManager.asset for the sake of testing, as we don't really want to go changing
        // that asset in every test.
        var testInputManager = ScriptableObject.CreateInstance<TestInputManager>();
        AssetDatabase.CreateAsset(testInputManager, "Assets/TestInputManager.asset");

        // create a template input action asset from which the input action asset stuffed inside the InputManager will be created
        var globalActions = ScriptableObject.CreateInstance<InputActionAsset>();
        globalActions.AddActionMap("ActionMapOne").AddAction("ActionOne");
        m_TemplateAssetPath = Path.Combine(Environment.CurrentDirectory, "Assets/TestGlobalActions.inputactions");
        File.WriteAllText(m_TemplateAssetPath, globalActions.ToJson());

        InputSystem.SetGlobalActionAssetPaths(m_TemplateAssetPath, "Assets/TestInputManager.asset");

        base.Setup();
    }

    [TearDown]
    public override void TearDown()
    {
        if (File.Exists(m_TemplateAssetPath))
            File.Delete(m_TemplateAssetPath);

        AssetDatabase.DeleteAsset("Assets/TestInputManager.asset");

        base.TearDown();
    }

    [Test]
    [Category("GlobalActions")]
    public void GlobalActions_TemplateAssetIsInstalledOnFirstUse()
    {
        var asset = GlobalActionsAsset.GetOrCreateGlobalActionsAsset("Assets/TestInputManager.asset", m_TemplateAssetPath);

        Assert.That(asset, Is.Not.Null);
        Assert.That(asset.actionMaps.Count, Is.EqualTo(1));
        Assert.That(asset.actionMaps[0].actions.Count, Is.EqualTo(1));
    }

    [Test]
    [Category("GlobalActions")]
    public void GlobalActions_CanQueryActionsByStringName()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        Input.globalActions.FindAction("ActionOne").AddBinding("<keyboard>/w");

        Set(keyboard.wKey, 1);

        Assert.That(Input.IsControlDown("ActionOne"), Is.True);
        Assert.That(Input.IsControlPressed("ActionOne"), Is.True);

        Set(keyboard.wKey, 0);

        Assert.That(Input.IsControlPressed("ActionOne"), Is.False);
        Assert.That(Input.IsControlUp("ActionOne"), Is.True);

        InputSystem.Update();

        Assert.That(Input.IsControlUp("ActionOne"), Is.False);
    }

    public class TestInputManager : ScriptableObject
    {
    }
}
