using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Genre-style shortcut examples (RTS, shooter, etc.) migrated from sample projects into the Input System test suite.
/// </summary>
internal partial class CoreTests
{
    /// <summary>
    /// Here we have some RTS-related actions and tests. This whole thing emerged out of having to implement control groups in RTS games.
    /// </summary>
    private static InputActionMap CreateRtsExampleShortcutMap(
        out InputAction actionOne,
        out InputAction actionShiftOne,
        out InputAction actionControlShiftOne)
    {
        var map = new InputActionMap("RTSShortcuts");

        actionOne = map.AddAction("One", InputActionType.Button);
        actionOne.AddBinding("<Keyboard>/1");

        actionShiftOne = map.AddAction("ShiftOne", InputActionType.Button);
        actionShiftOne.AddCompositeBinding("OneModifier")
            .With("modifier", "<Keyboard>/shift")
            .With("binding", "<Keyboard>/1");
        actionShiftOne.Priority = 1;

        actionControlShiftOne = map.AddAction("ControlShiftOne", InputActionType.Button);
        actionControlShiftOne.AddCompositeBinding("TwoModifiers")
            .With("modifier1", "<Keyboard>/ctrl")
            .With("modifier2", "<Keyboard>/shift")
            .With("binding", "<Keyboard>/1");
        actionControlShiftOne.Priority = 2;

        return map;
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_Genres_RTS_Key1Only_ActivatesPlainOne()
    {
        EnableActionPriorityShortcutResolution();

        var keyboard = InputSystem.AddDevice<Keyboard>();
        using var map = CreateRtsExampleShortcutMap(out var actionOne, out var actionShiftOne, out var actionControlShiftOne);
        map.Enable();

        Press((ButtonControl)keyboard.digit1Key);
        InputSystem.Update();

        Assert.That(actionOne.IsPressed(), Is.True);
        Assert.That(actionShiftOne.IsPressed(), Is.False);
        Assert.That(actionControlShiftOne.IsPressed(), Is.False);

        Release((ButtonControl)keyboard.digit1Key);
        InputSystem.Update();
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_Genres_RTS_ShiftOne_SuppressesPlainOne()
    {
        EnableActionPriorityShortcutResolution();

        var keyboard = InputSystem.AddDevice<Keyboard>();
        using var map = CreateRtsExampleShortcutMap(out var actionOne, out var actionShiftOne, out var actionControlShiftOne);
        map.Enable();

        Press((ButtonControl)keyboard.shiftKey);
        Press((ButtonControl)keyboard.digit1Key);
        InputSystem.Update();

        Assert.That(actionOne.IsPressed(), Is.False, "Plain 1 should not activate when Shift+1 wins.");
        Assert.That(actionShiftOne.IsPressed(), Is.True, "Shift+1 should activate.");
        Assert.That(actionControlShiftOne.IsPressed(), Is.False);

        Release((ButtonControl)keyboard.digit1Key);
        Release((ButtonControl)keyboard.shiftKey);
        InputSystem.Update();
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_Genres_RTS_ControlShiftOne_SuppressesLowerTiers()
    {
        EnableActionPriorityShortcutResolution();

        var keyboard = InputSystem.AddDevice<Keyboard>();
        using var map = CreateRtsExampleShortcutMap(out var actionOne, out var actionShiftOne, out var actionControlShiftOne);
        map.Enable();

        Press((ButtonControl)keyboard.ctrlKey);
        Press((ButtonControl)keyboard.shiftKey);
        Press((ButtonControl)keyboard.digit1Key);
        InputSystem.Update();

        Assert.That(actionOne.IsPressed(), Is.False, "Plain 1 should not activate when Ctrl+Shift+1 wins.");
        Assert.That(actionShiftOne.IsPressed(), Is.False, "Shift+1 should not activate when Ctrl+Shift+1 wins.");
        Assert.That(actionControlShiftOne.IsPressed(), Is.True, "Ctrl+Shift+1 should activate.");

        Release((ButtonControl)keyboard.digit1Key);
        Release((ButtonControl)keyboard.shiftKey);
        Release((ButtonControl)keyboard.ctrlKey);
        InputSystem.Update();
    }

    /// <summary>
    /// The following section emerged from trying input shortcuts with some generic joystick-controlled games, platformer/scroller style.
    /// In those games, the move action overlaps with a plenty of special abilities.
    /// </summary>
    private static InputActionMap CreateGenericJoystickExampleShortcutMap(
        out InputAction actionMove,
        out InputAction actionJump,
        out InputAction actionJumpKick)
    {
        var map = new InputActionMap("JoystickExample");

        actionMove = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
        actionMove.AddBinding("<Gamepad>/leftStick");

        actionJump = map.AddAction("Jump", InputActionType.Button);
        actionJump.AddBinding("<Gamepad>/buttonSouth");

        actionJumpKick = map.AddAction("Jump Kick", InputActionType.Button);

        actionJumpKick.AddCompositeBinding("OneModifier")
            .With("modifier", "<Gamepad>/leftShoulder")
            .With("binding", "<Gamepad>/buttonSouth");

        actionJumpKick.Priority = 1;

        return map;
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_Genres_GenericJoystick_LeftStick_Activates_JustOneAction()
    {
        EnableActionPriorityShortcutResolution();

        var gamepad = InputSystem.AddDevice<Gamepad>();
        using var map = CreateGenericJoystickExampleShortcutMap(out var actionMove, out var actionJump, out var actionJumpKick);
        map.Enable();

        Set(gamepad.leftStick, Vector2.up);

        InputSystem.Update();

        Assert.IsTrue(actionMove.IsPressed(), "Move should be active when the left stick is tilted.");
        Assert.IsFalse(actionJump.IsPressed(), "Jump should not be active without X (south).");
        Assert.IsFalse(actionJumpKick.IsPressed(), "Jump Kick should not be active without L1+X.");

        Set(gamepad.leftStick, Vector2.zero);
        InputSystem.Update();
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_Genres_GenericJoystick_LeftStickAndX_Triggers_Move_And_Jump()
    {
        EnableActionPriorityShortcutResolution();

        var gamepad = InputSystem.AddDevice<Gamepad>();
        using var map = CreateGenericJoystickExampleShortcutMap(out var actionMove, out var actionJump, out var actionJumpKick);
        map.Enable();

        Set(gamepad.leftStick, Vector2.up);
        Press(gamepad.buttonSouth);

        InputSystem.Update();

        Assert.IsTrue(actionMove.IsPressed(), "Move should still be active while the stick is tilted.");
        Assert.IsTrue(actionJump.IsPressed(), "Jump should be active when X (south) is pressed.");
        Assert.IsFalse(actionJumpKick.IsPressed(), "Jump Kick should not be active without L1.");

        Release(gamepad.buttonSouth);
        Set(gamepad.leftStick, Vector2.zero);
        InputSystem.Update();
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_Genres_GenericJoystick_L1AndX_Only_Triggers_JumpKick()
    {
        EnableActionPriorityShortcutResolution();

        var gamepad = InputSystem.AddDevice<Gamepad>();
        using var map = CreateGenericJoystickExampleShortcutMap(out var actionMove, out var actionJump, out var actionJumpKick);
        map.Enable();

        Press(gamepad.leftShoulder);
        Press(gamepad.buttonSouth);

        InputSystem.Update();

        Assert.IsFalse(actionMove.IsPressed(), "Move should not be active when the stick is neutral.");
        Assert.IsFalse(actionJump.IsPressed(), "Jump should not consume when Jump Kick wins (priority + consume).");
        Assert.IsTrue(actionJumpKick.IsPressed(), "Jump Kick should be active for L1+X.");

        Release(gamepad.buttonSouth);
        Release(gamepad.leftShoulder);
        InputSystem.Update();
    }

    /// <summary>
    /// The following section originates from shooter games where move overlaps with a bunch of weapon controls and team interactions.
    /// </summary>
    private static InputActionMap CreateShooterExampleShortcutMap(
        out InputAction actionMove,
        out InputAction actionRunFast,
        out InputAction actionTeamChat)
    {
        var map = new InputActionMap("ShooterExample");

        actionMove = map.AddAction("Move", InputActionType.Value);
        actionMove.AddCompositeBinding("2DVector")
            .With("up", "<Keyboard>/w")
            .With("down", "<Keyboard>/s")
            .With("left", "<Keyboard>/a")
            .With("right", "<Keyboard>/d");

        actionRunFast = map.AddAction("Run Fast", InputActionType.Button);
        actionRunFast.AddCompositeBinding("OneModifier")
            .With("modifier", "<Keyboard>/shift")
            .With("binding", "<Keyboard>/w");

        actionTeamChat = map.AddAction("Team chat", InputActionType.Button);
        actionTeamChat.AddCompositeBinding("TwoModifiers")
            .With("modifier1", "<Keyboard>/alt")
            .With("modifier2", "<Keyboard>/shift")
            .With("binding", "<Keyboard>/w");

        actionTeamChat.Priority = 2;

        return map;
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_Genres_Shooter_Move_Only_Triggers_Move()
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        using var map = CreateShooterExampleShortcutMap(out var actionMove, out var actionRunFast, out var actionTeamChat);
        map.Enable();

        Press(keyboard.wKey);
        InputSystem.Update();

        Assert.IsTrue(actionMove.IsPressed(), "Move should be activated by W.");
        Assert.IsFalse(actionRunFast.IsPressed(), "Run Fast should not be activated by W alone.");
        Assert.IsFalse(actionTeamChat.IsPressed(), "Team chat should not be activated by W alone.");

        Release(keyboard.wKey);
        InputSystem.Update();
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_Genres_Shooter_ShiftW_Triggers_Move_And_RunFast()
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        using var map = CreateShooterExampleShortcutMap(out var actionMove, out var actionRunFast, out var actionTeamChat);
        map.Enable();

        Press(keyboard.leftShiftKey);
        Press(keyboard.wKey);

        InputSystem.Update();

        Assert.IsTrue(actionMove.IsPressed(), "Move should still be activated by W.");
        Assert.IsTrue(actionRunFast.IsPressed(), "Run Fast should be activated by Shift+W.");
        Assert.IsFalse(actionTeamChat.IsPressed(), "Team chat should not be activated without Alt.");

        Release(keyboard.wKey);
        Release(keyboard.leftShiftKey);
        InputSystem.Update();
    }

    [Test]
    [Category("Actions Priority")]
    public void Actions_Priority_Genres_Shooter_AltShiftW_Only_Triggers_TeamChat()
    {
        EnableActionPriorityShortcutResolution();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        using var map = CreateShooterExampleShortcutMap(out var actionMove, out var actionRunFast, out var actionTeamChat);
        map.Enable();

        Press(keyboard.leftAltKey);
        Press(keyboard.leftShiftKey);
        Press(keyboard.wKey);

        InputSystem.Update();

        Assert.IsTrue(
            actionTeamChat.IsPressed() || actionTeamChat.IsInProgress(),
            "Team chat should trigger when Alt, Shift, and W are down together.");

        Assert.IsFalse(actionMove.IsPressed(), "Move shouldn't be activated!");
        Assert.IsFalse(actionRunFast.IsPressed(), "Run Fast shouldn't be activated!");

        Release(keyboard.leftAltKey);
        Release(keyboard.leftShiftKey);
        Release(keyboard.wKey);

        InputSystem.Update();
    }
}
