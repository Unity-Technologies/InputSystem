using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

internal class MultiTouchRegressionTests : CoreTestsFixture
{
    private InputSystemUIInputModule m_InputModule;
    private Touchscreen m_Touchscreen;
    private Button m_Button;
    private int m_ClickCount;

    public override void Setup()
    {
        base.Setup();

        var canvas = new GameObject("Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        var buttonGo = new GameObject("Button");
        buttonGo.transform.SetParent(canvas.transform);
        m_Button = buttonGo.AddComponent<Button>();
        m_Button.gameObject.AddComponent<Image>();
        var buttonRect = m_Button.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(100f, 40f);
        buttonRect.localPosition = Vector3.zero;

        GameObject go = new GameObject("Event System");
        var eventSystem = go.AddComponent<EventSystem>();
        eventSystem.pixelDragThreshold = 1;

        m_InputModule = go.AddComponent<InputSystemUIInputModule>();
        Cursor.lockState = CursorLockMode.None;

        m_Touchscreen = InputSystem.AddDevice<Touchscreen>();
        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var uiActions = actions.AddActionMap("UI");
        var pointAction = uiActions.AddAction("point", type: InputActionType.PassThrough);
        pointAction.AddBinding("<Touchscreen>/touch0/position");
        pointAction.AddBinding("<Touchscreen>/touch1/position");
        pointAction.Enable();
        m_InputModule.point = InputActionReference.Create(pointAction);

        m_ClickCount = 0;
        m_Button.onClick.AddListener(() => m_ClickCount++);
    }

    [UnityTest]
    [Description("Regression test for UUM-138595: UI buttons stop responding after multi-touch gesture")]
    public IEnumerator MultiTouchGesture_DoesNotBlockSubsequentSingleTaps()
    {
        var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        var tapPos = screenCenter;
        var finger1StartPos = screenCenter + new Vector2(-60, 0);
        var finger1EndPos = screenCenter + new Vector2(-30, 0);
        var finger2StartPos = screenCenter + new Vector2(60, 0);
        var finger2EndPos = screenCenter + new Vector2(30, 0);

        // Step 1: Single tap on button - should trigger click
        InputSystem.QueueStateEvent(m_Touchscreen,
            new TouchState { touchId = 1, position = tapPos, phase = TouchPhase.Began });
        yield return null;

        InputSystem.QueueStateEvent(m_Touchscreen,
            new TouchState { touchId = 1, position = tapPos, phase = TouchPhase.Ended });
        yield return null;

        yield return null;
        Assert.That(m_ClickCount, Is.EqualTo(1), "Button should respond to first single tap");

        // Step 2: Perform multi-touch pinch gesture
        InputSystem.QueueStateEvent(m_Touchscreen,
            new TouchState { touchId = 1, position = finger1StartPos, phase = TouchPhase.Began });
        yield return null;

        InputSystem.QueueStateEvent(m_Touchscreen,
            new TouchState { touchId = 2, position = finger2StartPos, phase = TouchPhase.Began });
        yield return null;

        InputSystem.QueueStateEvent(m_Touchscreen,
            new TouchState { touchId = 1, position = finger1EndPos, phase = TouchPhase.Moved });
        InputSystem.QueueStateEvent(m_Touchscreen,
            new TouchState { touchId = 2, position = finger2EndPos, phase = TouchPhase.Moved });
        yield return null;

        InputSystem.QueueStateEvent(m_Touchscreen,
            new TouchState { touchId = 1, position = finger1EndPos, phase = TouchPhase.Ended });
        InputSystem.QueueStateEvent(m_Touchscreen,
            new TouchState { touchId = 2, position = finger2EndPos, phase = TouchPhase.Ended });
        yield return null;

        yield return null;

        // Step 3: Single tap on button after multi-touch - should still trigger click
        InputSystem.QueueStateEvent(m_Touchscreen,
            new TouchState { touchId = 3, position = tapPos, phase = TouchPhase.Began });
        yield return null;

        InputSystem.QueueStateEvent(m_Touchscreen,
            new TouchState { touchId = 3, position = tapPos, phase = TouchPhase.Ended });
        yield return null;

        yield return null;
        Assert.That(m_ClickCount, Is.EqualTo(2),
            "Button should respond to tap after multi-touch gesture (UUM-138595 regression)");
    }
}
