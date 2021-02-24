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
    [UnityTest]
    [Category("Actions")]
    public IEnumerator Actions_InteractiveRebinding_NewBindingsAreRemovedFromAssetOnExitingPlayMode()
    {
        var assetPath = $"Assets/{Path.GetRandomFileName().Substring(0, 8)}.{InputActionAsset.Extension}";
        
        // store the asset path in editor prefs so we can still access it after the domain reload on entering/exiting playmode
        EditorPrefs.SetString("assetPath", assetPath);

        var inputActionMap = new InputActionMap("actionMap");
        inputActionMap.AddAction("action1");
        
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionMap(inputActionMap);

        File.WriteAllText(assetPath, asset.ToJson());
        AssetDatabase.ImportAsset(assetPath);

        yield return new EnterPlayMode();

        // reload the input action asset after the domain reload
        asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(EditorPrefs.GetString("assetPath"));
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

        var actualAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(EditorPrefs.GetString("assetPath"));
        Assert.That(actualAsset.actionMaps[0].actions[0].bindings.Count, Is.Zero);

        AssetDatabase.DeleteAsset(EditorPrefs.GetString("assetPath"));
        EditorPrefs.DeleteKey("assetPath");
    }
}
