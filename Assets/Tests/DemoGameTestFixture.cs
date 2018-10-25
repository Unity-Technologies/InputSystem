using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.DualShock;
using UnityEngine.Experimental.Input.Plugins.XInput;
using UnityEngine.Experimental.Input.Plugins.XR;
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
    public XRHMD hmd { get; set; }
    public XRController leftHand { get; set; }
    public XRController rightHand { get; set; }
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
            switch (value.ToLower())
            {
                case "osx": platform = RuntimePlatform.OSXPlayer; break;
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
        // NOTE: We use strings here instead of types as not all devices are available in all players.
        switch (platform)
        {
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                keyboard = (Keyboard)InputSystem.AddDevice("Keyboard");
                mouse = (Mouse)InputSystem.AddDevice("Mouse");
                ps4Gamepad = (DualShockGamepad)InputSystem.AddDevice("DualShockGamepadHID");
                xboxGamepad = (XInputController)InputSystem.AddDevice("XInputController");
                ////TODO: joystick
                break;

            ////TODO: other platforms
            default:
                throw new NotImplementedException();
        }

        // Check if we should add VR support.
        if (testProperties.ContainsKey("VR"))
        {
            var value = (string)testProperties["VR"][0];
            switch (value.ToLower())
            {
                case "":
                case "any":
                    // Add a combination of generic XRHMD and XRController instances that don't
                    // represent any specific set of hardware out there.
                    hmd = InputSystem.AddDevice<XRHMD>();
                    leftHand = InputSystem.AddDevice<XRController>();
                    rightHand = InputSystem.AddDevice<XRController>();
                    InputSystem.SetDeviceUsage(leftHand, CommonUsages.LeftHand);
                    InputSystem.SetDeviceUsage(rightHand, CommonUsages.RightHand);
                    break;

                default:
                    throw new NotImplementedException();
            }

            DemoGame.vrSupported = true;
        }
    }

    [TearDown]
    public void TearDown()
    {
        inputFixture.TearDown();
    }

    public void Click(string button, int playerIndex = 0)
    {
        if (playerIndex != 0)
            throw new NotImplementedException();

        ////TODO: drive this from a mouse input event so that we cover the whole UI action setup, too
        var buttonObject = GameObject.Find(button);
        Assert.That(buttonObject != null);
        buttonObject.GetComponent<Button>().onClick.Invoke();
    }

    public void Trigger(string action, int playerIndex = 0)
    {
        // Look up action.
        var controls = game.players[playerIndex].controls;
        var actionInstance = controls.asset.FindAction(action);
        if (actionInstance == null)
            throw new ArgumentException("action");

        // And trigger it.
        inputFixture.Trigger(actionInstance);
    }
}
