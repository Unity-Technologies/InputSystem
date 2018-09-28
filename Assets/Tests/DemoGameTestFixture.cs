using System;
using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.DualShock;
using UnityEngine.Experimental.Input.Plugins.XInput;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Gyroscope = UnityEngine.Experimental.Input.Gyroscope;

/// <summary>
/// Fixture to set up tests for <see cref="DemoGame"/>.
/// </summary>
[PrebuildSetup("DemoGameTestPrebuildSetup")]
public class DemoGameTestFixture
{
    public DemoGame game { get; set; }
    public InputTestFixture inputFixture { get; set; }
    public RuntimePlatform platform { get; private set; }

    public Mouse mouse { get; set; }
    public Keyboard keyboard { get; set; }
    public Touchscreen touchscreen { get; set; }
    public DualShockGamepad ps4Gamepad { get; set; }
    public XInputController xboxGamepad { get; set; }
    public Joystick joystick { get; set; }
    public Pen pen { get; set; }
    public Gyroscope gyro { get; set; }
    ////TODO: on-screen controls

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Set up input.
        inputFixture = new InputTestFixture();
        inputFixture.Setup();

        // See if we have a platform set for the current test.
        var testProperties = TestContext.CurrentContext.Test.Properties;
        if (testProperties.ContainsKey("Platform"))
        {
            var value = (string)testProperties["Platform"][0];
            switch (value)
            {
                case "OSX": platform = RuntimePlatform.OSXPlayer; break;
                default: throw new NotImplementedException();
            }
        }
        else
        {
            platform = Application.platform;
        }
        DemoGame.platform = platform;

        // Give us a fresh scene.
        yield return SceneManager.LoadSceneAsync("Assets/Demo/Demo.unity", LoadSceneMode.Single);
        game = GameObject.Find("DemoGame").GetComponent<DemoGame>();

        // Set up default device matrix for current platform.
        switch (platform)
        {
            #if UNITY_STANDALONE_OSX || UNITY_EDITOR
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                keyboard = InputSystem.AddDevice<Keyboard>();
                mouse = InputSystem.AddDevice<Mouse>();
                ps4Gamepad = InputSystem.AddDevice<DualShockGamepadHID>();
                xboxGamepad = InputSystem.AddDevice<XInputController>();
                ////TODO: joystick
                break;
            #endif

            ////TODO: other platforms
            default:
                throw new NotImplementedException();
        }
    }

    [TearDown]
    public void TearDown()
    {
        inputFixture.TearDown();
    }

    public void Click(string button, DemoPlayerController player = null)
    {
        if (player != null)
            throw new NotImplementedException();

        ////TODO: drive this from a mouse input event so that we cover the whole UI action setup, too
        var buttonObject = GameObject.Find(button);
        Assert.That(buttonObject != null);
        buttonObject.GetComponent<Button>().onClick.Invoke();
    }
}
