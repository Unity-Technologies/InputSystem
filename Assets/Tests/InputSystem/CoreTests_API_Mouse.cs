using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

partial class CoreTests
{
    public static IEnumerable<int> mouseButtonNumber = new[] { 0, 1, 2, 3, 4 };
    public static IEnumerable<string> mouseButtonName = new[] { "leftButton", "rightButton", "middleButton", "backButton", "forwardButton" };

    [UnityTest]
    [Category("API")]
    public IEnumerator API_CanReadMouseButtonsThroughGetMouseButtonAPI([ValueSource(nameof(mouseButtonNumber))] int buttonNumber, [ValueSource(nameof(mouseButtonName))] string buttonName)
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var button = (ButtonControl)mouse[buttonName];

        for (var i = 0; i < Input.kMouseButtonCount; ++i)
        {
            Assert.That(Input.GetMouseButton(i), Is.False);
            Assert.That(Input.GetMouseButtonDown(i), Is.False);
            Assert.That(Input.GetMouseButtonUp(i), Is.False);
        }

        Press(button);
        yield return null;

        Assert.That(Input.GetMouseButton(buttonNumber), Is.True);
        Assert.That(Input.GetMouseButtonDown(buttonNumber), Is.True);
        Assert.That(Input.GetMouseButtonUp(buttonNumber), Is.False);

        for (var i = 0; i < Input.kMouseButtonCount; ++i)
        {
            if (i == buttonNumber)
                continue;
            Assert.That(Input.GetMouseButton(i), Is.False);
            Assert.That(Input.GetMouseButtonDown(i), Is.False);
            Assert.That(Input.GetMouseButtonUp(i), Is.False);
        }

        yield return null;

        Assert.That(Input.GetMouseButton(buttonNumber), Is.True);
        Assert.That(Input.GetMouseButtonDown(buttonNumber), Is.False);
        Assert.That(Input.GetMouseButtonUp(buttonNumber), Is.False);

        for (var i = 0; i < Input.kMouseButtonCount; ++i)
        {
            if (i == buttonNumber)
                continue;
            Assert.That(Input.GetMouseButton(i), Is.False);
            Assert.That(Input.GetMouseButtonDown(i), Is.False);
            Assert.That(Input.GetMouseButtonUp(i), Is.False);
        }

        Release(button);
        yield return null;

        Assert.That(Input.GetMouseButton(buttonNumber), Is.False);
        Assert.That(Input.GetMouseButtonDown(buttonNumber), Is.False);
        Assert.That(Input.GetMouseButtonUp(buttonNumber), Is.True);

        for (var i = 0; i < Input.kMouseButtonCount; ++i)
        {
            if (i == buttonNumber)
                continue;
            Assert.That(Input.GetMouseButton(i), Is.False);
            Assert.That(Input.GetMouseButtonDown(i), Is.False);
            Assert.That(Input.GetMouseButtonUp(i), Is.False);
        }

        yield return null;

        Assert.That(Input.GetMouseButton(buttonNumber), Is.False);
        Assert.That(Input.GetMouseButtonDown(buttonNumber), Is.False);
        Assert.That(Input.GetMouseButtonUp(buttonNumber), Is.False);

        for (var i = 0; i < Input.kMouseButtonCount; ++i)
        {
            if (i == buttonNumber)
                continue;
            Assert.That(Input.GetMouseButton(i), Is.False);
            Assert.That(Input.GetMouseButtonDown(i), Is.False);
            Assert.That(Input.GetMouseButtonUp(i), Is.False);
        }
    }

    [Test]
    [Category("API")]
    public void API_CanDetectPresenceOfMouseThroughMouseAPI()
    {
        Assert.That(Input.mousePresent,  Is.False);

        InputSystem.AddDevice<Pen>();
        InputSystem.AddDevice<Touchscreen>();

        Assert.That(Input.mousePresent,  Is.False);

        var mouse = InputSystem.AddDevice<Mouse>();

        Assert.That(Input.mousePresent,  Is.True);

        InputSystem.RemoveDevice(mouse);

        Assert.That(Input.mousePresent,  Is.False);
    }

    [Test]
    [Category("API")]
    public void API_CanReadMousePositionThroughMouseAPI()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var pen = InputSystem.AddDevice<Pen>();

        Set(mouse.position, new Vector2(123, 234));

        Assert.That(Input.mousePosition, Is.EqualTo(new Vector2(123, 234)).Using(Vector2EqualityComparer.Instance));

        Set(pen.position, new Vector2(234, 345));

        Assert.That(Input.mousePosition, Is.EqualTo(new Vector2(234, 345)).Using(Vector2EqualityComparer.Instance));

        ////REVIEW: this probably will have to have some connection to simulateMouseWithTouches

        BeginTouch(1, new Vector2(345, 456));

        Assert.That(Input.mousePosition, Is.EqualTo(new Vector2(345, 456)).Using(Vector2EqualityComparer.Instance));
    }
}
