using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.UI;

public class RebindingUITests : InputTestFixture
{
    [Test]
    [Category("Samples")]
    public void Samples_CanCreateRebindingUI()
    {
        var canvasGO = new GameObject();
        canvasGO.AddComponent<Canvas>();

        var actionLabelGO = new GameObject();
        actionLabelGO.transform.parent = canvasGO.transform;
        var actionLabel = actionLabelGO.AddComponent<Text>();

        var bindingLabelGO = new GameObject();
        bindingLabelGO.transform.parent = canvasGO.transform;
        var bindingLabel = bindingLabelGO.AddComponent<Text>();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var actionMap = asset.AddActionMap("map");
        var action = actionMap.AddAction("action", binding: "<Mouse>/leftButton");

        var go = new GameObject();
        var rebind = go.AddComponent<RebindActionUI>();
        rebind.bindingId = action.bindings[0].id.ToString();
        rebind.actionReference = InputActionReference.Create(action);
        rebind.actionLabel = actionLabel;
        rebind.bindingText = bindingLabel;

        Assert.That(bindingLabel.text, Is.EqualTo("LMB"));
        Assert.That(actionLabel.text, Is.EqualTo("action"));

        // Go through rebind.
        var keyboard = InputSystem.AddDevice<Keyboard>();
        rebind.StartInteractiveRebind();

        Assert.That(rebind.ongoingRebind, Is.Not.Null);
        Assert.That(rebind.ongoingRebind.started, Is.True);

        Press(keyboard.spaceKey);

        currentTime += 2;
        InputSystem.Update();

        Assert.That(rebind.ongoingRebind, Is.Null);
        Assert.That(action.bindings[0].overridePath, Is.EqualTo("<Keyboard>/space"));
        Assert.That(bindingLabel.text, Is.EqualTo("Space"));
    }

    [Test]
    [Category("Samples")]
    public void Samples_RebindingUI_UpdatesWhenKeyboardLayoutChanges()
    {
        var canvasGO = new GameObject();
        canvasGO.AddComponent<Canvas>();

        var bindingLabelGO = new GameObject();
        bindingLabelGO.transform.parent = canvasGO.transform;
        var bindingLabel = bindingLabelGO.AddComponent<Text>();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var actionMap = asset.AddActionMap("map");
        var action = actionMap.AddAction("action", binding: "<Keyboard>/a");

        var go = new GameObject();
        var rebind = go.AddComponent<RebindActionUI>();
        rebind.bindingId = action.bindings[0].id.ToString();
        rebind.actionReference = InputActionReference.Create(action);
        rebind.bindingText = bindingLabel;

        Assert.That(bindingLabel.text, Is.EqualTo("A"));

        SetKeyInfo(Key.A, "Q");

        Assert.That(bindingLabel.text, Is.EqualTo("Q"));
    }
}
