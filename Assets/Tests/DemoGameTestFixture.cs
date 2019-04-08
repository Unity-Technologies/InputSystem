using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Plugins.DualShock;
using UnityEngine.Experimental.Input.Plugins.Steam;
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
    public InputTestFixture input { get; set; }
    public SteamTestFixture steam { get; set; }
    public RuntimePlatform platform { get; private set; }

    public Mouse mouse { get; set; }
    public Keyboard keyboard { get; set; }
    public Touchscreen touchscreen { get; set; }
    public Gamepad gamepad { get; set; }
    public DualShockGamepad ps4Gamepad { get; set; }
    public XInputController xboxGamepad { get; set; }
    public Joystick joystick { get; set; }
    public Pen pen { get; set; }
    public Gyroscope gyro { get; set; }
    public XRHMD hmd { get; set; }
    public XRController leftHand { get; set; }
    public XRController rightHand { get; set; }
    public InputDevice steamController { get; set; }
    ////TODO: on-screen controls

    public DemoPlayerController player1
    {
        get { return game.players[0]; }
    }

    /// <summary>
    /// Enumerate all fired projectiles that currently exist in the scene.
    /// </summary>
    public GameObject[] projectiles
    {
        get { return GameObject.FindGameObjectsWithTag("Projectile"); }
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Set up input.
        input = new InputTestFixture();
        input.Setup();

        // See if we have a platform set for the current test.
        var testProperties = TestContext.CurrentContext.Test.Properties;
        if (testProperties.ContainsKey("Platform"))
        {
            var value = (string)testProperties["Platform"][0];
            switch (value.ToLower())
            {
                case "windows":
                    platform = RuntimePlatform.WindowsPlayer;
                    break;

                case "osx":
                    platform = RuntimePlatform.OSXPlayer;
                    break;

                case "android":
                    platform = RuntimePlatform.Android;
                    break;

                case "ios":
                    platform = RuntimePlatform.IPhonePlayer;
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
        else
        {
            platform = Application.platform;
        }
        DemoGame.platform = platform;

        // If there's a "Platform" property on the test or no specific "Device" property, add the default
        // set of devices for the current platform.
        if (testProperties.ContainsKey("Platform") || !testProperties.ContainsKey("Device"))
        {
            // Set up default device matrix for current platform.
            // NOTE: We use strings here instead of types as not all devices are available in all players.
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    keyboard = (Keyboard)InputSystem.AddDevice("Keyboard");
                    mouse = (Mouse)InputSystem.AddDevice("Mouse");
                    pen = (Pen)InputSystem.AddDevice("Pen");
                    touchscreen = (Touchscreen)InputSystem.AddDevice("Touchscreen");
                    gamepad = ps4Gamepad = (DualShockGamepad)InputSystem.AddDevice("DualShockGamepadHID");
                    xboxGamepad = (XInputController)InputSystem.AddDevice("XInputController");
                    ////TODO: joystick
                    break;

                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    keyboard = (Keyboard)InputSystem.AddDevice("Keyboard");
                    mouse = (Mouse)InputSystem.AddDevice("Mouse");
                    gamepad = ps4Gamepad = (DualShockGamepad)InputSystem.AddDevice("DualShockGamepadHID");
                    xboxGamepad = (XInputController)InputSystem.AddDevice("XInputController");
                    ////TODO: joystick
                    break;

                case RuntimePlatform.PS4:
                    ps4Gamepad = (DualShockGamepad)InputSystem.AddDevice("DualShockGamepadPS4");
                    break;

                case RuntimePlatform.XboxOne:
                    xboxGamepad = (XInputController)InputSystem.AddDevice("XboxOneGamepad");
                    break;

                ////TODO: other platforms
                default:
                    throw new NotImplementedException();
            }
        }

        // Add whatever devices are specified in explicit "Device" properties.
        if (testProperties.ContainsKey("Device"))
        {
            foreach (var value in testProperties["Device"])
            {
                switch (((string)value).ToLower())
                {
                    case "gamepad":
                    {
                        var device = InputSystem.AddDevice<Gamepad>();
                        if (gamepad == null)
                            gamepad = device;
                        break;
                    }

                    case "keyboard":
                    {
                        var device = InputSystem.AddDevice<Keyboard>();
                        if (keyboard == null)
                            keyboard = device;
                        break;
                    }

                    case "mouse":
                    {
                        var device = InputSystem.AddDevice<Mouse>();
                        if (mouse == null)
                            mouse = device;
                        break;
                    }

                    default:
                        throw new NotImplementedException();
                }
            }
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

        // Check if we should add Steam support.
        if (testProperties.ContainsKey("Steam"))
        {
            ////TODO: create steam test fixture
            steamController = InputSystem.AddDevice("SteamDemoController");
        }

        // Give us a fresh scene.
        yield return SceneManager.LoadSceneAsync("Assets/Demo/Demo.unity", LoadSceneMode.Single);
        game = GameObject.Find("DemoGame").GetComponent<DemoGame>();

        ////FIXME: for some reason the Start() function doesn't get called during tests.... WTF?
        game.Start();
    }

    [TearDown]
    public void TearDown()
    {
        // It looks like the test runner is stupidly reusing test fixture instances instead of
        // creating a new object for every run. So we really have to clean up well.

        game.players.Each(UnityEngine.Object.DestroyImmediate);
        UnityEngine.Object.DestroyImmediate(game.gameObject);

        DemoPlayerController.ClearUIHintsCache();

        input.TearDown();

        game = null;
        input = null;
        steam = null;

        mouse = null;
        keyboard = null;
        touchscreen = null;
        gamepad = null;
        ps4Gamepad = null;
        xboxGamepad = null;
        joystick = null;
        pen = null;
        gyro = null;
        hmd = null;
        leftHand = null;
        rightHand = null;
        steamController = null;
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

    public void Trigger(InputAction action)
    {
        input.Trigger(action);
    }

    /// <summary>
    /// Press a key on the keyboard.
    /// </summary>
    /// <param name="key"></param>
    /// <remarks>
    /// Requires the current platform to have a keyboard.
    /// </remarks>
    public void Press(Key key, double timeOffset = 0)
    {
        var keyboard = InputSystem.GetDevice<Keyboard>();
        Debug.Assert(keyboard != null);
        input.Set(keyboard[key], 1, timeOffset);
    }

    /// <summary>
    /// Release a key on the keyboard.
    /// </summary>
    /// <param name="key"></param>
    /// <remarks>
    /// Requires the current platform to have a keyboard.
    /// </remarks>
    public void Release(Key key, double timeOffset = 0)
    {
        var keyboard = InputSystem.GetDevice<Keyboard>();
        Debug.Assert(keyboard != null);
        input.Set(keyboard[key], 1, timeOffset);
    }

    /// <summary>
    /// Press a button on a device.
    /// </summary>
    /// <param name="button"></param>
    public void Press(ButtonControl button, double timeOffset = 0)
    {
        input.Set(button, 1, timeOffset);
    }

    /// <summary>
    /// Release a button on a device.
    /// </summary>
    /// <param name="button"></param>
    public void Release(ButtonControl button, double timeOffset = 0)
    {
        input.Set(button, 0, timeOffset);
    }

    public void Set<TValue>(InputControl<TValue> control, TValue value, double timeOffset = 0)
        where TValue : struct
    {
        input.Set(control, value, timeOffset);
    }

    /// <summary>
    /// Retrieve a GameObject by its name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public GameObject GO(string name)
    {
        var obj = GameObject.Find(name);
        if (obj == null)
            throw new Exception(string.Format("Cannot find GameObject '{0}'", name));
        return obj;
    }

    /// <summary>
    /// Retrieve a component on a named GameObject.
    /// </summary>
    /// <param name="name"></param>
    /// <typeparam name="TObject"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public TComponent GO<TComponent>(string name)
        where TComponent : Component
    {
        var obj = GO(name);
        var component = obj.GetComponent<TComponent>();
        if (component == null)
            throw new Exception(string.Format("Cannot find component of type '{0}' on GameObject '{1}'",
                typeof(TComponent).Name, name));
        return component;
    }
}
