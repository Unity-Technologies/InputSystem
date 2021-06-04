using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class RebindingUITests : CoreTestsFixture
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

    // https://fogbugz.unity3d.com/f/cases/1271591/
    [Test]
    [Category("Samples")]
    public void Samples_RebindingUI_SuppressingEventsDoesNotInterfereWithUIInput()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var actionMap = asset.AddActionMap("map");
        var action = actionMap.AddAction("action", binding: "<Keyboard>/a");

        var canvasGO = new GameObject();
        canvasGO.SetActive(false);
        canvasGO.AddComponent<Canvas>();

        // Set up UI input module.
        var eventSystemGO = new GameObject();
        eventSystemGO.SetActive(false);
        var eventSystem = eventSystemGO.AddComponent<TestEventSystem>();
        var uiInputModule = eventSystemGO.AddComponent<InputSystemUIInputModule>();
        var inputActions = new DefaultInputActions().asset;
        uiInputModule.actionsAsset = inputActions;
        uiInputModule.submit = InputActionReference.Create(inputActions["submit"]);

        var bindingButtonGO = new GameObject();
        bindingButtonGO.transform.parent = canvasGO.transform;
        var bindingButton = bindingButtonGO.AddComponent<Button>();

        var bindingLabelGO = new GameObject();
        bindingLabelGO.transform.parent = bindingButtonGO.transform;
        var bindingLabel = bindingLabelGO.AddComponent<Text>();

        var rebind = bindingButtonGO.AddComponent<RebindActionUI>();
        rebind.bindingId = action.bindings[0].id.ToString();
        rebind.actionReference = InputActionReference.Create(action);
        rebind.bindingText = bindingLabel;
        bindingButton.onClick.AddListener(rebind.StartInteractiveRebind);

        canvasGO.SetActive(true);
        eventSystemGO.SetActive(true);

        eventSystem.SetSelectedGameObject(bindingButtonGO);
        eventSystem.InvokeUpdate(); // Initial update switches the input module.

        Assert.That(rebind.ongoingRebind, Is.Null);
        Assert.That(bindingLabel.text, Is.EqualTo("A"));

        // As soon as the submit hits, the rebind starts -- which in turn enables suppression
        // of events. This means that the enter key release event will not reach the UI. The
        // UI should be fine with that.
        PressAndRelease(keyboard.enterKey);
        eventSystem.InvokeUpdate();

        Assert.That(rebind.ongoingRebind, Is.Not.Null);
        Assert.That(rebind.ongoingRebind.started, Is.True);
        Assert.That(rebind.ongoingRebind.candidates, Is.Empty);
        Assert.That(bindingLabel.text, Is.EqualTo("<Waiting...>"));
        Assert.That(inputActions["submit"].inProgress, Is.False);

        Press(keyboard.bKey);
        eventSystem.InvokeUpdate();

        Assert.That(rebind.ongoingRebind, Is.Not.Null);
        Assert.That(rebind.ongoingRebind.started, Is.True);
        Assert.That(rebind.ongoingRebind.candidates, Is.EquivalentTo(new[] { keyboard.bKey }));
        Assert.That(bindingLabel.text, Is.EqualTo("<Waiting...>"));
        Assert.That(inputActions["submit"].inProgress, Is.False);

        // Expire rebind wait time.
        currentTime += 1;
        InputSystem.Update();

        Assert.That(rebind.ongoingRebind, Is.Null);
        Assert.That(bindingLabel.text, Is.EqualTo("B"));
        Assert.That(inputActions["submit"].inProgress, Is.False);

        // Start another rebind via "Submit".
        PressAndRelease(keyboard.enterKey);
        eventSystem.InvokeUpdate();

        Assert.That(rebind.ongoingRebind, Is.Not.Null);
        Assert.That(rebind.ongoingRebind.started, Is.True);
        Assert.That(rebind.ongoingRebind.candidates, Is.Empty);
        Assert.That(bindingLabel.text, Is.EqualTo("<Waiting...>"));
    }

    private class TestEventSystem : EventSystem
    {
        public void InvokeUpdate()
        {
            Update();
        }
    }
}
