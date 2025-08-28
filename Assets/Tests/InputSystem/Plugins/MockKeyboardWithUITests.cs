using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

[Description("Check https://jira.unity3d.com/browse/ISXB-1561 for more details.")]
public class MockKeyboardWithUITests : InputTestFixture
{
    private Keyboard m_Keyboard;
    private GameObject m_CanvasGo;
    private GameObject m_EventSystemGo;
    private GameObject m_ButtonGo;
    private InputActionAsset m_InputActionAsset;

    public class CancellationHandler : MonoBehaviour, ICancelHandler
    {
        public Action trigger;
        public void OnCancel(BaseEventData eventData) => trigger();
    }

    public override void Setup()
    {
        base.Setup();

        // Add a mock keyboard
        m_Keyboard = InputSystem.AddDevice<Keyboard>();

        // first creating the UI stuff
        m_CanvasGo = new GameObject("canvas");
        m_ButtonGo = new GameObject("button", typeof(Button), typeof(CancellationHandler));
        m_ButtonGo.transform.SetParent(m_CanvasGo.transform, false);

        // Now creating the Input layer
        m_EventSystemGo = new GameObject("eventSystem", typeof(EventSystem));
        EventSystem.current.SetSelectedGameObject(m_ButtonGo);

        var inputModule = m_EventSystemGo.AddComponent<InputSystemUIInputModule>();

        m_InputActionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
        var actionMap = new InputActionMap("UI");
        var submitAction = actionMap.AddAction("Submit", binding: "Keyboard/enter", type: InputActionType.Button);
        var cancelAction = actionMap.AddAction("Cancel", binding: "Keyboard/escape", type: InputActionType.Button);
        m_InputActionAsset.AddActionMap(actionMap);
        actionMap.Enable();
        inputModule.actionsAsset = m_InputActionAsset;
        inputModule.submit = InputActionReference.Create(submitAction);
        inputModule.cancel = InputActionReference.Create(cancelAction);
    }

    public override void TearDown()
    {
        GameObject.Destroy(m_EventSystemGo);
        GameObject.Destroy(m_CanvasGo);

        InputSystem.RemoveDevice(m_Keyboard);

        base.TearDown();
    }

    private static bool[] FrameWaitingOptions = { true, false };

    [UnityTest]
    public IEnumerator MockKeyboard_PressEnter_TriggersButtonOnClick([ValueSource(nameof(FrameWaitingOptions))] bool waitFrame)
    {
        bool invokedListener = false;

        m_ButtonGo.GetComponent<Button>().onClick.AddListener(() => invokedListener = true);

        Press(m_Keyboard.enterKey);

        // We'd like to test both options to be safe with rapid actions that happened within a single frame
        if (waitFrame)
            yield return null;

        Release(m_Keyboard.enterKey);

        yield return null;

        Assert.IsTrue(invokedListener, "The button should have been clicked here.");
    }

    [UnityTest]
    public IEnumerator MockKeyboard_PressEscape_TriggersCancelHandler([ValueSource(nameof(FrameWaitingOptions))] bool waitFrame)
    {
        bool invokedListener = false;
        m_ButtonGo.GetComponent<CancellationHandler>().trigger = () => invokedListener = true;

        Press(m_Keyboard.escapeKey);

        // We'd like to test both options to be safe with rapid actions that happened within a single frame
        if (waitFrame)
            yield return null;

        Release(m_Keyboard.escapeKey);

        yield return null;

        Assert.IsTrue(invokedListener, "The cancel event should have been raised here.");
    }
}