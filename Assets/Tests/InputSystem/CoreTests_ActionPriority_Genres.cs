using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Genre-style shortcut examples (RTS, shooter, etc.) migrated from sample projects into the Input System test suite.
/// </summary>
internal partial class CoreTests
{
    /// <summary>
    /// Programmatic map matching the RTS example scene: plain <c>1</c>, <c>Shift+1</c>, and <c>Ctrl+Shift+1</c> on the same binding key.
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
        EnableComplexityShortcutResolution();
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
        EnableComplexityShortcutResolution();
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
        EnableComplexityShortcutResolution();
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
}
