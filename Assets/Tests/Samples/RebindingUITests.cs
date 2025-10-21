using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class RebindingUITests : CoreTestsFixture
{
    private int m_Counter;

    public override void Setup()
    {
        base.Setup();
        m_Counter = 0;
    }

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
    [UnityTest]
    [Category("Samples")]
    public IEnumerator Samples_RebindingUI_SuppressingEventsDoesNotInterfereWithUIInput()
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
        yield return null;


        Assert.That(rebind.ongoingRebind, Is.Not.Null);
        Assert.That(rebind.ongoingRebind.started, Is.True);
        Assert.That(rebind.ongoingRebind.candidates, Is.Empty);
        Assert.That(bindingLabel.text, Is.EqualTo("<Waiting...>"));
        Assert.That(inputActions["submit"].inProgress, Is.False);

        Press(keyboard.bKey);
        eventSystem.InvokeUpdate();
        yield return null;

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
        yield return null;

        Assert.That(rebind.ongoingRebind, Is.Not.Null);
        Assert.That(rebind.ongoingRebind.started, Is.True);
        Assert.That(rebind.ongoingRebind.candidates, Is.Empty);
        Assert.That(bindingLabel.text, Is.EqualTo("<Waiting...>"));
    }

    [UnityTest]
    [Category("Samples")]
    public IEnumerator Samples_RebindingUI_InvokeUnityEventForwardsEvent()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        var action1 = map.AddAction(name: "first", type: InputActionType.Button, binding: "<Gamepad>/buttonNorth");
        var action2 = map.AddAction(name: "second", type: InputActionType.Button, binding: "<Gamepad>/buttonSouth");

        action1.Enable();
        action2.Enable();

        UnityAction incrementByOne = () => ++ m_Counter;
        UnityAction incrementByTwo = () => m_Counter += 2;

        var go = new GameObject();
        var invoke = go.AddComponent<InvokeUnityEvent>();

        // Setup both action and unity event
        invoke.action = InputActionReference.Create(action1);
        invoke.onPerformed.AddListener(incrementByOne);

        // Press button and check that unity event is invoked
        PressAndRelease(gamepad.buttonNorth);
        yield return null;
        Assert.That(m_Counter, Is.EqualTo(1));

        // Switch action
        invoke.action = InputActionReference.Create(action2);

        // Press button and check that no unity event is invoked
        PressAndRelease(gamepad.buttonNorth);
        yield return null;
        Assert.That(m_Counter, Is.EqualTo(1));

        // Press other button and check that no unity event is invoked
        PressAndRelease(gamepad.buttonSouth);
        yield return null;
        Assert.That(m_Counter, Is.EqualTo(2));

        // Remove event
        invoke.onPerformed = null;

        // Press other button and check that nothing happens
        PressAndRelease(gamepad.buttonSouth);
        yield return null;
        Assert.That(m_Counter, Is.EqualTo(2));

        // Add other event and set action to null
        var unityEvent = new UnityEvent();
        unityEvent.AddListener(incrementByTwo);
        invoke.onPerformed = unityEvent;
        invoke.action = null;

        // Press other button and check that nothing happens
        PressAndRelease(gamepad.buttonSouth);
        yield return null;
        Assert.That(m_Counter, Is.EqualTo(2));

        // Set action back to initial configuration
        invoke.action = InputActionReference.Create(action1);
        PressAndRelease(gamepad.buttonNorth);
        yield return null;
        Assert.That(m_Counter, Is.EqualTo(4));
    }

    private class TestEventSystem : EventSystem
    {
        public void InvokeUpdate()
        {
            Update();
        }
    }
}
