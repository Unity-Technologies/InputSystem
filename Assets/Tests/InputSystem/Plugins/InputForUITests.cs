#if UNITY_2023_2_OR_NEWER // UnityEngine.InputForUI Module unavailable in earlier releases
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputForUI;
using UnityEngine.InputSystem;
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
    InputSystemProvider m_InputSystemProvider;

    private InputActionAsset storedActions;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

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
    public void PointerEventsAreDispatchedFromMouse()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        Update();

        PressAndRelease(mouse.leftButton);

        Update();

        Assert.IsTrue(m_InputForUIEvents.Count == 2);
        Assert.That(m_InputForUIEvents[0].type, Is.EqualTo(Event.Type.PointerEvent));
        Assert.That(m_InputForUIEvents[0].asPointerEvent.type, Is.EqualTo(PointerEvent.Type.ButtonPressed));
        Assert.That(m_InputForUIEvents[1].type, Is.EqualTo(Event.Type.PointerEvent));
        Assert.That(m_InputForUIEvents[1].asPointerEvent.type, Is.EqualTo(PointerEvent.Type.ButtonReleased));
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

        Assert.IsTrue(m_InputForUIEvents.Count == 1);
        Assert.That(m_InputForUIEvents[0] is Event
        {
            type: Event.Type.PointerEvent,
            asPointerEvent: { type: PointerEvent.Type.ButtonPressed,
                              eventSource: EventSource.Touch }
        });
    }

    [Test]
    [Category(kTestCategory)]
    // Presses a gamepad left stick left and verifies that a navigation move event is dispatched
    public void NavigationMoveWorks()
    {
        MoveWithGamepad();

        Assert.IsTrue(m_InputForUIEvents.Count == 1);
        Assert.That(m_InputForUIEvents[0] is Event
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move,
                                 direction: NavigationEvent.Direction.Left,
                                 eventSource: EventSource.Gamepad}
        });
    }

    void MoveWithGamepad()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        Update();
        Press(gamepad.leftStick.left);
        Update();
        Release(gamepad.leftStick.left);
        Update();
    }

    [Test]
    [Category(kTestCategory)]
    public void SendWheelEvent()
    {
        var kScrollUGUIScaleFactor = 3.0f; // See InputSystemProvider OnScrollWheelPerformed() callback
        var mouse = InputSystem.AddDevice<Mouse>();
        Update();
        // Make the minimum step of scroll delta to be ±1.0f
        Set(mouse.scroll.y, -1f / kScrollUGUIScaleFactor);
        Update();
        Assert.IsTrue(m_InputForUIEvents.Count == 1);
        Assert.That(m_InputForUIEvents[0].asPointerEvent.scroll, Is.EqualTo(new Vector2(0, 1)));
    }

    [Test]
    [Category(kTestCategory)]
    [TestCase(true)]
    [TestCase(false)]
    // The goal of this test is to make sure that InputSystemProvider works with and without project-wide actions asset
    // so that there is no impact in receiving the necessary input events for UI.
    // When there are no project-wide actions asset, the InputSystemProvider should still work as it currently gets
    // the actions from DefaultActionsAsset().asset.
    public void EventProviderWorksWithAndWithoutProjectWideActionsSet(bool useProjectWideActionsAsset)
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
        MoveWithGamepad();

        Assert.IsTrue(m_InputForUIEvents.Count == 1);
        Assert.That(m_InputForUIEvents[0] is Event
        {
            type: Event.Type.NavigationEvent,
            asNavigationEvent: { type: NavigationEvent.Type.Move,
                                 direction: NavigationEvent.Direction.Left,
                                 eventSource: EventSource.Gamepad}
        });
    }

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
