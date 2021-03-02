using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

internal class CoreEditorTests
{
    private readonly string m_TestAssetPath = $"Assets/__TestInputAsset.{InputActionAsset.Extension}";

    // https://issuetracker.unity3d.com/issues/inputsystem-runtime-rebinds-are-leaking-into-inputactions-asset
    // https://fogbugz.unity3d.com/f/cases/1190502/
    [UnityTest]
    [Category("Actions")]
    public IEnumerator Actions_InteractiveRebinding_NewBindingsAreRemovedFromAssetOnExitingPlayMode()
    {
        CreateTestInputAsset();

        yield return new EnterPlayMode();

        // reload the input action asset after the domain reload
        var asset = LoadTestInputAsset();
        var action = asset.FindAction("action1");

        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var rebind =
            new InputActionRebindingExtensions.RebindingOperation()
                .WithAction(action)
                .WithRebindAddingNewBinding(group: "testGroup")
                .Start())
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.Update();
        }

        yield return new ExitPlayMode();

        var actualAsset = LoadTestInputAsset();
        Assert.That(actualAsset.actionMaps[0].actions[0].bindings.Count, Is.Zero);

        AssetDatabase.DeleteAsset(m_TestAssetPath);
    }

    [UnityTest]
    [Category("Actions")]
    public IEnumerator Actions_NewControlSchemesAreRemovedFromAssetOnExitingPlayMode()
    {
        CreateTestInputAsset();

        yield return new EnterPlayMode();

        var asset = LoadTestInputAsset();
        asset.AddControlScheme("testControlScheme");

        yield return new ExitPlayMode();

        var actualAsset = LoadTestInputAsset();
        Assert.That(actualAsset.controlSchemes.Count, Is.Zero);

        AssetDatabase.DeleteAsset(m_TestAssetPath);
    }

    private void CreateTestInputAsset()
    {
        var inputActionMap = new InputActionMap("actionMap");
        inputActionMap.AddAction("action1");

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(inputActionMap);

        File.WriteAllText(m_TestAssetPath, asset.ToJson());
        AssetDatabase.ImportAsset(m_TestAssetPath);
    }

    private InputActionAsset LoadTestInputAsset()
    {
        return AssetDatabase.LoadAssetAtPath<InputActionAsset>(m_TestAssetPath);
    }
}
