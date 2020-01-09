using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Samples.InGameHints;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class InGameHintsTests : InputTestFixture
{
    [UnityTest]
    [Category("Samples")]
    public IEnumerator Samples_InGameHints_ShowControlsAccordingToCurrentlyUsedDevice()
    {
        var switchController = InputSystem.AddDevice<SwitchProControllerHID>();
        var xboxController = InputSystem.AddDevice<XInputController>();
        var ps4Controller = InputSystem.AddDevice<DualShockGamepad>();

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var textGO = new GameObject();
        var text = textGO.AddComponent<Text>();

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = new Vector3(0, 0, 100);

        var player = new GameObject();
        player.SetActive(false); // Avoid PlayerInput grabbing devices before we have its configuration in place.
        var playerInput = player.AddComponent<PlayerInput>();
        playerInput.actions = new InGameHintsActions().asset;
        playerInput.defaultActionMap = "Gameplay";
        playerInput.defaultControlScheme = "Keyboard&Mouse";

        var inGameHints = player.AddComponent<InGameHintsExample>();
        inGameHints.helpText = text;
        inGameHints.pickupDistance = 10;

        player.SetActive(true);

        Assert.That(playerInput.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));

        yield return null;

        // Move player into pickup range.
        player.transform.position = cube.transform.position - new Vector3(0, 0, 5);

        yield return null;

        Assert.That(text.text, Does.StartWith("Press Q "));

        // Switch to PS4 controller.
        Press(ps4Controller.startButton);

        Assert.That(text.text, Does.StartWith("Press Cross "));

        // Switch to Xbox controller.
        Press(xboxController.startButton);

        Assert.That(text.text, Does.StartWith("Press A "));

        // Switch to Switch controller.
        Press(switchController.startButton);

        Assert.That(text.text, Does.StartWith("Press B "));
    }
}
