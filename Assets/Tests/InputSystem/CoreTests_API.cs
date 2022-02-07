// Tests for the UnityEngine.Input API.

using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

partial class CoreTests
{
    #if UNITY_EDITOR
    [Test]
    [Category("API")]
    public void API_SettingsAreStoredInInputManagerAsset()
    {
        // Get rid of InputTestFixture to switch us back to the "live" system.
        TearDown();

        Assert.That(AssetDatabase.GetAssetPath(InputSystem.settings),
            Is.EqualTo("ProjectSettings/InputManager.asset"));
    }

    #endif

    //Q: under what circumstances do we migrate InputManager axis settings instead?
    [Test]
    [Category("API")]
    public void API_GlobalActionsDefaultToDefaultInputActionAsset()
    {
        Assert.That(InputSystem.actions.Select(a => $"{a.actionMap.name}/{a.name}"),
            Is.EquivalentTo(new DefaultInputActions().asset.Select(a => $"{a.actionMap.name}/{a.name}")));
        Assert.That(InputSystem.actions.SelectMany(a => a.bindings),
            Is.EquivalentTo(new DefaultInputActions().asset.SelectMany(a => a.bindings)));
    }

    [Test]
    [Category("API")]
    public void API_GlobalActionsAreAutomaticallyEnabled()
    {
        Assert.That(InputSystem.actions.enabled, Is.True);
    }

    //really?
    [Test]
    [Category("API")]
    public void API_CanSwitchGlobalActionsOnTheFly()
    {
        Assert.Fail();
    }

    [Test]
    [Category("API")]
    public void API_CanAccessGlobalActionsThroughGetAxisAPI()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Set(gamepad.leftStick, Vector2.right);

        Assert.That(Input.GetAxis("Horizontal"), Is.EqualTo(1f));
        Assert.That(Input.GetAxis("Vertical"), Is.EqualTo(0f));

        var keyboard = InputSystem.AddDevice<Keyboard>();

        Set(gamepad.leftStick, Vector2.zero);
        Press(keyboard.aKey);

        Assert.That(Input.GetAxis("Horizontal"), Is.EqualTo(-1));
        Assert.That(Input.GetAxis("Vertical"), Is.EqualTo(0f));
    }

    [UnityTest]
    [Category("API")]
    public IEnumerator API_CanAccessGlobalActionsThroughGetButtonAPI()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        Assert.That(Input.GetButton("Fire"), Is.False);
        Assert.That(Input.GetButtonUp("Fire"), Is.False);
        Assert.That(Input.GetButtonDown("Fire"), Is.False);

        Press(mouse.leftButton);
        yield return null;

        Assert.That(Input.GetButton("Fire"), Is.True);
        Assert.That(Input.GetButtonUp("Fire"), Is.False);
        Assert.That(Input.GetButtonDown("Fire"), Is.True);

        yield return null;

        Assert.That(Input.GetButton("Fire"), Is.True);
        Assert.That(Input.GetButtonUp("Fire"), Is.False);
        Assert.That(Input.GetButtonDown("Fire"), Is.False);

        Release(mouse.leftButton);
        yield return null;

        Assert.That(Input.GetButton("Fire"), Is.False);
        Assert.That(Input.GetButtonUp("Fire"), Is.True);
        Assert.That(Input.GetButtonDown("Fire"), Is.False);

        yield return null;

        Assert.That(Input.GetButton("Fire"), Is.False);
        Assert.That(Input.GetButtonUp("Fire"), Is.False);
        Assert.That(Input.GetButtonDown("Fire"), Is.False);
    }

    [Test]
    [Category("API")]
    public void API_CanAccessGlobalActionsThroughCodeGeneratedAPI()
    {
        Assert.Fail();
    }

    [Test]
    [Category("API")]
    public void API_CanRebindGlobalActions()
    {
        Assert.Fail();
    }

    [Test]
    [Category("API")]
    public void API_UIDefaultsToUsingGlobalActions()
    {
        Assert.Fail();
    }

    [Test]
    [Category("API")]
    public void API_CanListJoysticksAndGamepadsThroughGetJoystickNamesAPI()
    {
        Assert.Fail();
    }
}
