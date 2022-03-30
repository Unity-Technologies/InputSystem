using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

partial class CoreTests
{
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
}
