#if UNITY_2023_2_OR_NEWER // UnityEngine.InputForUI Module unavailable in earlier releases
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputForUI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
#endif
using UnityEngine.InputSystem.Plugins.InputForUI;
using UnityEngine.TestTools;
using Event = UnityEngine.InputForUI.Event;
using EventProvider = UnityEngine.InputForUI.EventProvider;

// Note that these tests do not verify InputForUI default bindings at all.
// It only verifies integration with Project-wide Input Actions.
// Note that with current design and fixture, testing play-mode integration without Project-wide Input Actions
// would require a separate assembly, or potentially e.g. delete all actions to prevent matching.
//
// These tests are not meant to test the InputForUI module itself, but rather the integration between the InputForUI
// module and the InputSystem package.
// Be aware that these tests don't account for events dispatched by the InputEventPartialProvider. Those events are
// already tested in the Input Manager provider.
// Also, the internals to test InputEventPartialProvider are not exposed publicly, so we can't test them here.
[PrebuildSetup(typeof(ProjectWideActionsBuildSetup))]
[PostBuildCleanup(typeof(ProjectWideActionsBuildSetup))]
public class InputForUITests : InputTestFixture
{
    private const string kTestCategory = "InputForUI";

    readonly List<Event> m_InputForUIEvents = new List<Event>();
    private int m_CurrentInputEventToCheck;
    InputSystemProvider m_InputSystemProvider;

    private InputActionAsset storedActions;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        m_CurrentInputEventToCheck = 0;

        storedActions = InputSystem.actions;

        m_InputSystemProvider = new InputSystemProvider();
        EventProvider.SetMockProvider(m_InputSystemProvider);

        // Register at least one consumer so the mock update gets invoked
        EventProvider.Subscribe(InputForUIOnEvent);
    }

    [TearDown]
    public override void TearDown()
    {
        EventProvider.Unsubscribe(InputForUIOnEvent);
        EventProvider.ClearMockProvider();
        m_InputForUIEvents.Clear();

        InputSystem.s_Manager.actions = storedActions;

#if UNITY_EDITOR
        if (File.Exists(kAssetPath))
            UnityEditor.AssetDatabase.DeleteAsset(kAssetPath);
#endif

        base.TearDown();
    }

    internal Event GetNextRecordedUIEvent()
    {
        return m_InputForUIEvents[m_CurrentInputEventToCheck++];
    }

    private bool InputForUIOnEvent(in Event ev)
    {
        m_InputForUIEvents.Add(ev);
        return true;
    }

    [Test]
    [Category(kTestCategory)]
    public void InputSystemActionAssetIsNotNull()
    {
        // Test assumes a compatible action asset configuration exists for UI
        Assert.IsTrue(m_InputSystemProvider.ActionAssetIsNotNull(),
            "Test is invalid since InputSystemProvider actions are not available");
    }

    [Test]
    [Category(kTestCategory)]
    // Checks that mouse events are ignored when a touch is active.
    // This is to workaround the issue ISXB-269 on Windows.
    public void TouchIsPressedAndMouseEventsAreIgnored()
    {
        var touch = InputSystem.AddDevice<Touchscreen>();
        var mouse = InputSystem.AddDevice<Mouse>();
        // Set initial mouse position to (0.5, 0.5) so that we get a delta when the mouse is moved, to dispatch
        // a pointer move event
        Set(mouse.position, new Vector2(0.5f, 0.5f));
        Update();

        // Start touch and move mouse to the same position to replicate the issue of duplicated Mouse events for
        // Touch events on Windows.
        BeginTouch(1, new Vector2(100f, 0.5f));
        Move(mouse.position, new Vector2(100f, 0.5f));
        Update();

        Assert.AreEqual(1, m_InputForUIEvents.Count);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonPressed,
                              eventSource: EventSource.Touch }
        });
    }

    #region UI_Input_Actions
    // Test all default UI actions, and sure that InputSystemProvider works with and without project-wide actions asset
    // so that there is no impact in receiving the necessary input events for UI.
    // When there are no project-wide actions asset, the InputSystemProvider should still work as it currently gets
    // the actions from DefaultActionsAsset().asset.

    // Utility functions
    void PressUpdateReleaseUpdate(ButtonControl button)
    {
        Press(button);
        Update();
        Release(button);
        Update();
    }

    void TestGamepadNavigationCardinalDirections()
    {
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move, direction: NavigationEvent.Direction.Left, eventSource: EventSource.Gamepad }
        });
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move, direction: NavigationEvent.Direction.Up, eventSource: EventSource.Gamepad }
        });
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move, direction: NavigationEvent.Direction.Right, eventSource: EventSource.Gamepad }
        });
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move, direction: NavigationEvent.Direction.Down, eventSource: EventSource.Gamepad }
        });
    }

    void TestKeyboardNavigationCardinalDirections()
    {
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move, direction: NavigationEvent.Direction.Left, eventSource: EventSource.Keyboard }
        });
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move, direction: NavigationEvent.Direction.Up, eventSource: EventSource.Keyboard }
        });
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move, direction: NavigationEvent.Direction.Right, eventSource: EventSource.Keyboard }
        });
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move, direction: NavigationEvent.Direction.Down, eventSource: EventSource.Keyboard }
        });
    }

    [Test]
    [Category(kTestCategory)]
    [TestCase(true)]
    [TestCase(false)]
    public void UIActionNavigation_FiresUINavigationEvents_FromInputsGamepadJoystickAndKeyboard(bool useProjectWideActionsAsset)
    {
        Update();
        if (!useProjectWideActionsAsset)
        {
            // Remove the project-wide actions asset in play mode and player.
            // It will call InputSystem.onActionChange and re-set InputSystemProvider.actionAsset
            // This the case where no project-wide actions asset is available in the project.
            InputSystem.s_Manager.actions = null;
        }
        Update();

        var gamepad = InputSystem.AddDevice<Gamepad>();
        PressUpdateReleaseUpdate(gamepad.leftStick.left);
        PressUpdateReleaseUpdate(gamepad.leftStick.up);
        PressUpdateReleaseUpdate(gamepad.leftStick.right);
        PressUpdateReleaseUpdate(gamepad.leftStick.down);
        TestGamepadNavigationCardinalDirections();

        PressUpdateReleaseUpdate(gamepad.rightStick.left);
        PressUpdateReleaseUpdate(gamepad.rightStick.up);
        PressUpdateReleaseUpdate(gamepad.rightStick.right);
        PressUpdateReleaseUpdate(gamepad.rightStick.down);
        TestGamepadNavigationCardinalDirections();

        PressUpdateReleaseUpdate(gamepad.dpad.left);
        PressUpdateReleaseUpdate(gamepad.dpad.up);
        PressUpdateReleaseUpdate(gamepad.dpad.right);
        PressUpdateReleaseUpdate(gamepad.dpad.down);
        TestGamepadNavigationCardinalDirections();


        var joystick = InputSystem.AddDevice<Joystick>();
        PressUpdateReleaseUpdate(joystick.stick.left);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move, direction: NavigationEvent.Direction.Left, eventSource: EventSource.Unspecified}
        });
        PressUpdateReleaseUpdate(joystick.stick.up);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move, direction: NavigationEvent.Direction.Up, eventSource: EventSource.Unspecified}
        });
        PressUpdateReleaseUpdate(joystick.stick.right);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move, direction: NavigationEvent.Direction.Right, eventSource: EventSource.Unspecified}
        });
        PressUpdateReleaseUpdate(joystick.stick.down);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move, direction: NavigationEvent.Direction.Down, eventSource: EventSource.Unspecified}
        });


        var keyboard = InputSystem.AddDevice<Keyboard>();
        PressUpdateReleaseUpdate(keyboard.aKey);
        PressUpdateReleaseUpdate(keyboard.wKey);
        PressUpdateReleaseUpdate(keyboard.dKey);
        PressUpdateReleaseUpdate(keyboard.sKey);
        TestKeyboardNavigationCardinalDirections();

        PressUpdateReleaseUpdate(keyboard.leftArrowKey);
        PressUpdateReleaseUpdate(keyboard.upArrowKey);
        PressUpdateReleaseUpdate(keyboard.rightArrowKey);
        PressUpdateReleaseUpdate(keyboard.downArrowKey);
        TestKeyboardNavigationCardinalDirections();
    }

    [Test]
    [Category(kTestCategory)]
    [TestCase(true)]
    [TestCase(false)]
    public void UIActionSubmit_FiresUISubmitEvents_FromInputsGamepadJoystickAndKeyboard(bool useProjectWideActionsAsset)
    {
        Update();
        if (!useProjectWideActionsAsset)
        {
            InputSystem.s_Manager.actions = null;
        }
        Update();

        PressUpdateReleaseUpdate(InputSystem.AddDevice<Gamepad>().buttonSouth);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Submit, eventSource: EventSource.Gamepad }
        });

        PressUpdateReleaseUpdate(InputSystem.AddDevice<Joystick>().trigger);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Submit, eventSource: EventSource.Unspecified }
        });

        PressUpdateReleaseUpdate(InputSystem.AddDevice<Keyboard>().enterKey);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Submit, eventSource: EventSource.Keyboard }
        });

        Assert.AreEqual(3, m_InputForUIEvents.Count);
    }

    [Test]
    [Category(kTestCategory)]
    [TestCase(true)]
    [TestCase(false)]
    public void UIActionCancel_FiresUICancelEvents_FromInputsGamepadAndKeyboard(bool useProjectWideActionsAsset)
    {
        Update();
        if (!useProjectWideActionsAsset)
        {
            InputSystem.s_Manager.actions = null;
        }
        Update();

        PressUpdateReleaseUpdate(InputSystem.AddDevice<Gamepad>().buttonEast);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Cancel, eventSource: EventSource.Gamepad }
        });

        PressUpdateReleaseUpdate(InputSystem.AddDevice<Keyboard>().escapeKey);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Cancel, eventSource: EventSource.Keyboard }
        });

        Assert.AreEqual(2, m_InputForUIEvents.Count);
    }

    [Test]
    [Category(kTestCategory)]
    [TestCase(true)]
    [TestCase(false)]
    public void UIActionPoint_FiresUIPointEvents_FromInputsMousePenAndTouch(bool useProjectWideActionsAsset)
    {
        Update();
        if (!useProjectWideActionsAsset)
        {
            InputSystem.s_Manager.actions = null;
        }
        Update();

        var mouse = InputSystem.AddDevice<Mouse>();
        Set(mouse.position, new Vector2(0.5f, 0.5f));
        Update();
        Move(mouse.position, new Vector2(100f, 0.5f));
        Update();

        var pen = InputSystem.AddDevice<Pen>();
        Set(pen.position, new Vector2(0.5f, 0.5f));
        Update();
        Move(pen.position, new Vector2(100f, 0.5f));
        Update();

        InputSystem.AddDevice<Touchscreen>();
        BeginTouch(1, new Vector2(0.5f, 0.5f));
        Update();
        EndTouch(1, new Vector2(100f, 100f));
        Update();

        // Touch screen move is three actions: press, move, release
        Assert.AreEqual(5, m_InputForUIEvents.Count);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.PointerMoved, eventSource: EventSource.Mouse }
        });
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.PointerMoved, eventSource: EventSource.Pen }
        });
        // Skip button down event that touch generates
        ++m_CurrentInputEventToCheck;
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.PointerMoved, eventSource: EventSource.Touch }
        });
    }

    [Test]
    [Category(kTestCategory)]
    [TestCase(true)]
    [TestCase(false)]
    public void UIActionClick_FiresUIClickEvents_FromInputsMousePenAndTouch(bool useProjectWideActionsAsset)
    {
        Update();
        if (!useProjectWideActionsAsset)
        {
            InputSystem.s_Manager.actions = null;
        }
        Update();

        var mouse = InputSystem.AddDevice<Mouse>();
        Click(mouse.leftButton);
        Update();
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonPressed, eventSource: EventSource.Mouse, button: PointerEvent.Button.MouseLeft, clickCount: 1 }
        });
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonReleased, eventSource: EventSource.Mouse, button: PointerEvent.Button.MouseLeft, clickCount: 1 }
        });

        Click(mouse.rightButton);
        Update();
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonPressed, eventSource: EventSource.Mouse, button: PointerEvent.Button.MouseRight, clickCount: 1 }
        });
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonReleased, eventSource: EventSource.Mouse, button: PointerEvent.Button.MouseRight, clickCount: 1 }
        });

        Click(mouse.middleButton);
        Update();
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonPressed, eventSource: EventSource.Mouse, button: PointerEvent.Button.MouseMiddle, clickCount: 1 }
        });
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonReleased, eventSource: EventSource.Mouse, button: PointerEvent.Button.MouseMiddle, clickCount: 1 }
        });

        var pen = InputSystem.AddDevice<Pen>();
        Click(pen.tip);
        Update();
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonPressed, eventSource: EventSource.Pen, button: PointerEvent.Button.MouseLeft, clickCount: 1 }
        });
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonReleased, eventSource: EventSource.Pen, button: PointerEvent.Button.MouseLeft, clickCount: 1 }
        });

        InputSystem.AddDevice<Touchscreen>();
        BeginTouch(1, new Vector2(0.0f, 0.5f));
        EndTouch(1, new Vector2(0.0f, 0.5f));
        Update();
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonPressed, eventSource: EventSource.Touch, button: PointerEvent.Button.MouseLeft, clickCount: 1 }
        });
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonReleased, eventSource: EventSource.Touch, button: PointerEvent.Button.MouseLeft, clickCount: 1 }
        });

        Assert.AreEqual(10, m_InputForUIEvents.Count);
    }

    const float kScrollUGUIScaleFactor = 3.0f; // See InputSystemProvider OnScrollWheelPerformed() callback

    [Test]
    [Category(kTestCategory)]
    [TestCase(true)]
    [TestCase(false)]
    public void UIActionScroll_FiresUIScrollEvents_FromInputMouse(bool useProjectWideActionsAsset)
    {
        Update();
        if (!useProjectWideActionsAsset)
        {
            InputSystem.s_Manager.actions = null;
        }
        Update();

        var mouse = InputSystem.AddDevice<Mouse>();
        Update();
        // Make the minimum step of scroll delta to be ±1.0f
        Set(mouse.scroll.y, -1f / kScrollUGUIScaleFactor);
        Update();
        Assert.AreEqual(1, m_InputForUIEvents.Count);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.Scroll, eventSource: EventSource.Mouse, scroll: {x: 0, y: 1} }
        });
    }

#if UNITY_INPUT_SYSTEM_PLATFORM_SCROLL_DELTA
    [Category(kTestCategory)]
    [TestCase(1.0f)]
    [TestCase(120.0f)]
    public void UIActionScroll_ReceivesNormalizedScrollWheelDelta(float scrollWheelDeltaPerTick)
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        Update();

        // Set scroll delta with a custom range.
        ((InputTestRuntime)InputRuntime.s_Instance).scrollWheelDeltaPerTick = scrollWheelDeltaPerTick;
        Set(mouse.scroll, new Vector2(0, scrollWheelDeltaPerTick));
        Update();

        // UI should receive scroll delta in its expected range.
        Assert.AreEqual(1, m_InputForUIEvents.Count);
        Assert.That(GetNextRecordedUIEvent() is
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.Scroll, eventSource: EventSource.Mouse, scroll: {x: 0, y: -kScrollUGUIScaleFactor} }
        });
    }

#endif

    #endregion

#if UNITY_EDITOR
    // These tests shouldn't really be in a non editor-only assembly but for now we guard them until moved.
    private const string kAssetPath = "Assets/InputSystem_InputForUI_TestActions.inputactions";

    [Test(Description = "Verifies that default actions (Project-wide) OR default actions have no verification errors.")]
    [Category(kTestCategory)]
    [TestCase(true)]
    [TestCase(true)]
    public void DefaultActions_ShouldNotGenerateAnyVerificationWarnings(bool useProjectWideActions)
    {
        if (!useProjectWideActions)
            InputSystem.s_Manager.actions = null;
        Update();
        LogAssert.NoUnexpectedReceived();
    }

    [Ignore("We currently allow a PWA asset without an UI action map and rely on defaults instead. This allows users that do not want it or use something else to avoid using it.")]
    [Test(Description = "Verifies that user-supplied project-wide input actions generates warnings if action map is missing.")]
    [Category(kTestCategory)]
    public void ActionsWithoutUIMap_ShouldGenerateWarnings()
    {
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath(kAssetPath);
        asset.RemoveActionMap(asset.FindActionMap("UI", throwIfNotFound: true));

        InputSystem.s_Manager.actions = asset;
        Update();

        var link = EditorHelpers.GetHyperlink(kAssetPath);
        LogAssert.Expect(LogType.Warning, new Regex($"^InputActionMap with path 'UI' in asset '{link}' could not be found."));
        if (InputActionAssetVerifier.DefaultReportPolicy == InputActionAssetVerifier.ReportPolicy.ReportAll)
        {
            LogAssert.Expect(LogType.Warning, new Regex($"^InputAction with path 'UI/Point' in asset '{link}' could not be found."));
            LogAssert.Expect(LogType.Warning, new Regex($"^InputAction with path 'UI/Navigate' in asset '{link}' could not be found."));
            LogAssert.Expect(LogType.Warning, new Regex($"^InputAction with path 'UI/Submit' in asset '{link}' could not be found."));
            LogAssert.Expect(LogType.Warning, new Regex($"^InputAction with path 'UI/Cancel' in asset '{link}' could not be found."));
            LogAssert.Expect(LogType.Warning, new Regex($"^InputAction with path 'UI/Click' in asset '{link}' could not be found."));
            LogAssert.Expect(LogType.Warning, new Regex($"^InputAction with path 'UI/MiddleClick' in asset '{link}' could not be found."));
            LogAssert.Expect(LogType.Warning, new Regex($"^InputAction with path 'UI/RightClick' in asset '{link}' could not be found."));
            LogAssert.Expect(LogType.Warning, new Regex($"^InputAction with path 'UI/ScrollWheel' in asset '{link}' could not be found."));
        }
        // else: expect suppression of child errors
        LogAssert.NoUnexpectedReceived();
    }

    [Test(Description = "Verifies that user-supplied project-wide input actions generates warnings if any required action is missing.")]
    [Category(kTestCategory)]
    [TestCase("UI/Point")]
    [TestCase("UI/Navigate")]
    [TestCase("UI/Submit")]
    [TestCase("UI/Cancel")]
    [TestCase("UI/Click")]
    [TestCase("UI/MiddleClick")]
    [TestCase("UI/RightClick")]
    [TestCase("UI/ScrollWheel")]
    public void ActionMapWithNonExistentRequiredAction_ShouldGenerateWarning(string actionPath)
    {
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath(kAssetPath);
        var action = asset.FindAction(actionPath);
        action.Rename("Other");

        InputSystem.s_Manager.actions = asset;
        Update();

        //var link = AssetDatabase.GetAssetPath()//EditorHelpers.GetHyperlink(kAssetPath);
        LogAssert.Expect(LogType.Warning, new Regex($"^InputAction with path '{actionPath}' in asset \"{kAssetPath}\" could not be found."));
        LogAssert.NoUnexpectedReceived();
    }

    [Test(Description = "Verifies that user-supplied project-wide input actions generates warnings if they lack bindings.")]
    [Category(kTestCategory)]
    [TestCase("UI/Point")]
    [TestCase("UI/Navigate")]
    [TestCase("UI/Submit")]
    [TestCase("UI/Cancel")]
    [TestCase("UI/Click")]
    [TestCase("UI/MiddleClick")]
    [TestCase("UI/RightClick")]
    [TestCase("UI/ScrollWheel")]
    public void ActionMapWithUnboundRequiredAction_ShouldGenerateWarning(string actionPath)
    {
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath(kAssetPath);
        var map = asset.FindActionMap("UI");

        // Recreate a map with selected action bindings removed (unfortunately there is no remove so this is what we got)
        asset.RemoveActionMap(map);
        var newMap = new InputActionMap(map.name);
        var n = map.actions.Count;
        for (var i = 0; i < n; ++i)
        {
            // Create a cloned action with only binding changed
            var source = map.actions[i];
            var action = newMap.AddAction(name: source.name,
                type: source.type,
                binding: actionPath == map.name + '/' + source.name ? null : "SomeIrrelevantBindingThatWillNeverResolve",
                processors: source.processors,
                interactions: source.interactions,
                expectedControlLayout: null,
                groups: null);
            action.expectedControlType = source.expectedControlType;
        }

        asset.AddActionMap(newMap);

        InputSystem.s_Manager.actions = asset;
        Update();

        LogAssert.Expect(LogType.Warning, new Regex($"^InputAction with path '{actionPath}' in asset \"{kAssetPath}\" do not have any configured bindings."));
        LogAssert.NoUnexpectedReceived();
    }

    [Test(Description =
            "Verifies that user-supplied project-wide input actions generates warnings if they have a different action type")]
    [TestCase("UI/Point", InputActionType.Button)]
    [TestCase("UI/Navigate", InputActionType.Button)]
    [TestCase("UI/Submit", InputActionType.Value)]
    [TestCase("UI/Cancel", InputActionType.Value)]
    [TestCase("UI/Click", InputActionType.Value)]
    [TestCase("UI/MiddleClick", InputActionType.Value)]
    [TestCase("UI/RightClick", InputActionType.Value)]
    [TestCase("UI/ScrollWheel", InputActionType.Button)]
    public void ActionWithUnexpectedActionType_ShouldGenerateWarning(string actionPath, InputActionType unexpectedType)
    {
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath(kAssetPath);
        var action = asset.FindAction(actionPath);
        Debug.Assert(action.type != unexpectedType); // not really an assert, sanity check test assumption for correctness
        var expectedType = action.type;
        action.m_Type = unexpectedType; // change directly via internals for now

        InputSystem.s_Manager.actions = asset;
        Update();

        LogAssert.Expect(LogType.Warning,
            new Regex($"^InputAction with path '{actionPath}' in asset \"{kAssetPath}\" has 'type' set to 'InputActionType.{unexpectedType}'"));
        LogAssert.NoUnexpectedReceived();
    }

    [Test(Description =
            "Verifies that user-supplied project-wide input actions generates warnings if they have a specified expected control type")]
    [TestCase("UI/Point", "Quaternion")]
    [TestCase("UI/Navigate", "Touch")]
    [TestCase("UI/Submit", "Vector2")]
    [TestCase("UI/Cancel", "Vector3")]
    [TestCase("UI/Click", "Axis")]
    [TestCase("UI/MiddleClick", "Delta")]
    [TestCase("UI/RightClick", "Bone")]
    [TestCase("UI/ScrollWheel", "Eyes")]
    public void ActionWithDifferentExpectedControlType_ShouldGenerateWarning(string actionPath, string unexpectedControlType)
    {
        var asset = ProjectWideActionsAsset.CreateDefaultAssetAtPath(kAssetPath);
        var action = asset.FindAction(actionPath);
        Debug.Assert(action.expectedControlType != unexpectedControlType); // not really an assert, sanity check test assumption for correctness
        var expectedControlType = action.expectedControlType;
        action.expectedControlType = unexpectedControlType;

        InputSystem.s_Manager.actions = asset;
        Update();

        LogAssert.Expect(LogType.Warning,
            new Regex($"^InputAction with path '{actionPath}' in asset \"{kAssetPath}\" has 'expectedControlType' set to '{unexpectedControlType}'"));
        LogAssert.NoUnexpectedReceived();
    }

#endif // UNITY_EDITOR

    static void Update()
    {
        EventProvider.NotifyUpdate();
        InputSystem.Update();
    }
}
#endif
