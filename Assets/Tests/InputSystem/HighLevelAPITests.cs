using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HighLevelAPI;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.TestTools;
using GamepadButton = UnityEngine.InputSystem.HighLevelAPI.GamepadButton;
using Input = UnityEngine.InputSystem.HighLevelAPI.Input;
using InputEvent = UnityEngine.InputSystem.HighLevelAPI.InputEvent;
using KeyControl = UnityEngine.InputSystem.HighLevelAPI.Key;

public class HighLevelAPITests : InputTestFixture
{
	private const string kCategory = "HighLevelAPI";

	[Test]
	[Category(kCategory)]
	public void BasicInput_CanGetMouseAndKeyboardInput()
	{
		Input.IsControlPressed(Inputs.Key_A);
		Input.IsControlPressed(Inputs.Gamepad_A);
		Input.IsControlDown(Inputs.Key_Space);
		Input.IsControlUp(Inputs.Joystick_Trigger);

		Assert.Fail("Not implemented");
	}

	[Test]
	[Category(kCategory)]
	public void BasicInput_CanCreateAxesFromBasicInputs()
	{
		var wsad = Input.GetAxis(Inputs.Key_W, Inputs.Key_S, Inputs.Key_A, Inputs.Key_D);
		var dpad = Input.GetAxisNormalized(Inputs.Gamepad_DpadLeft, Inputs.Gamepad_DpadRight, Inputs.Gamepad_DpadUp, Inputs.Gamepad_DpadDown);

		Assert.Fail("Not implemented");
	}

	[Test]
	[Category(kCategory)]
	public void BasicInput_CanGetGamepadStickInput()
	{
		Input.GetAxis(GamepadAxis.LeftStick);
	}

	[Test]
	[Category(kCategory)]
	public void BasicInput_CanGetInputFromASpecificGamepad()
	{
		var gamepadCount = Input.gamepads.Count;

		Input.GetAxis(GamepadAxis.LeftStick, GamepadSlot.Slot1);
		Input.IsGamepadButtonPressed(GamepadButton.A, 0);
		Input.IsGamepadButtonDown(GamepadButton.A, 0);
		Input.IsGamepadButtonUp(GamepadButton.A, 0);

		Assert.Fail("Not implemented");
	}

	
	[UnityTest]
	[Category(kCategory)]
	public IEnumerator Actions_CanQueryActions()
	{
		Input.IsActionPerforming("Gameplay/Move");
		Input.HasActionStarted("Gameplay/Move");
		Input.HasActionEnded("Gameplay/Move");

		yield return null;
		Assert.Fail("Not implemented");
	}

	[UnityTest]
	[Category(kCategory)]
	public IEnumerator Actions_CanQueryActionsByPlayerIndex()
	{
		var playerOne = Input.AssignNewPlayerToNextDevice();
		var playerTwo = Input.AssignNewPlayerToNextDevice();

		// TODO: Trigger two input events

		Assert.That(playerOne.isAssigned, Is.True);
		Assert.That(playerTwo.isAssigned, Is.True);

		playerOne.IsActionPerforming("Gameplay/Move");
		playerOne.HasActionStarted("Gameplay/Move");
		playerOne.HasActionEnded("Gameplay/Move");

		Assert.Fail("Not implemented");
		yield return null;
	}

	[Test]
	[Category(kCategory)]
	public void Actions_ConstantActionNamesAreGeneratedCorrectly()
	{
		Assert.Fail("Not implemented");
	}

	[UnityTest]
	[Category(kCategory)]
	public IEnumerator LocalMultiplayer_PlayerCanLeaveAndRejoinWithADifferentDevice()
	{
		var player = Input.AssignNewPlayerToNextDevice(Input.join);

		// Peform the join action on gamepad 1

		player.ReleaseDevices();
		var asyncAssignment = player.AssignToNextDevice(Input.join);

		while (asyncAssignment.isComplete == false)
		{
			// Perform the join action on gamepad 2 after some random time
			yield return null;
		}
	}

	[Test]
	[Category(kCategory)]
	public void Player_CanAssignExtraDevicesToAPlayer()
	{
		var player = Input.AssignNewPlayerToNextDevice(Input.join);
		player.AssignToDevice(Input.gamepads[1]);
	}

	[Test]
	[Category(kCategory)]
	public void Player_CanGetNotifiedWhenAnAssignedDeviceIsDisconnected()
	{
		var player = Input.AssignNewPlayerToNextDevice();

		// Press a key
		
		if (player.connectionStatus == PlayerStatus.JustDisconnected)
		{
			// show disconnected dialogue
		}

		while (player.connectionStatus == PlayerStatus.Disconnected)
		{
			// wait for a device to be connected again
		}
	}

	[Test]
	[Category(kCategory)]
	public void Player_AssignTwoPlayersToTheSameDevice()
	{
		// Sometimes you want to assign the same device to multiple players. An example
		// might be a local multi-player game where two players share the keyboard.
		// The current way to do this would be to assign bindings for both keys to each
		// action and then group those bindings into two control schemes, which would internally
		// set the binding mask 
		// Maybe the right way to do this is to provide a simple interface to change the bindings
		// for a player?
		var playerOne = Input.AssignNewPlayerToDevice(Input.keyboards[0]);
		var playerTwo = Input.AssignNewPlayerToDevice(Input.keyboards[0]);
		playerOne.UseControlGroup("WASD");
		playerTwo.UseControlGroup("IJKL");
	}

	[Test]
	[Category(kCategory)]
	public void Haptics_VibrateDevice()
	{
		// Gamepad.current.SetMotorSpeeds();
		Input.SetVibration(Input.gamepads[0], 1, 1, 0.5f, 0.5f);
	}

	[Test]
	[Category(kCategory)]
	public void Haptics_VibrateAllDevicesAssignedToPlayer()
	{
		var player = Input.AssignNewPlayerToNextDevice(Input.join);

		player.SetVibration(1, 1);
	}

	[Test]
	[Category(kCategory)]
	public void Interactions_CanChangeValuesOfInteractions()
	{
		var tapInteraction = new TapInteraction();
		Input.move.AddInteraction(tapInteraction);
		Input.move.RemoveInteraction(tapInteraction);
		Input.move.RemoveInteraction<TapInteraction>();

		Input.move.SetInteractionParameter<TapInteraction, float>(x => x.duration, 10);
		var duration = Input.move.GetInteractionParameter<TapInteraction, float>(x => x.duration);
	}

	[Test]
	[Category(kCategory)]
	public void Events_CheckIfThereWasAnyInputInThisFrame()
	{
		if (Input.lastInput.frame == Time.frameCount)
		{
			// Do something with the event
		}
	}

	[Test]
	[Category(kCategory)]
	public void Events_CanGetInputActionEventsViaOnInput()
	{

	}

	[Test]
	[Category(kCategory)]
	public void Events_CanGetBasicInputEventsViaOnInput()
	{

	}

	public class TestOnInputMonoBehaviour : MonoBehaviour
	{
		public void OnInput(ref InputEvent inputEvent)
		{
			var modifiers = new InputEventModifiers();

			if (inputEvent.hasModifiers)
				modifiers = inputEvent.modifiers;

			if (inputEvent.HasEventComponent<InputEventKey>())
			{
				Debug.Log(inputEvent.GetEventComponent<InputEventKey>().keyValue);
			}

			if (inputEvent.HasEventComponent<InputEventMouse>())
			{
				var mouseEventData = inputEvent.GetEventComponent<InputEventMouse>();
				Debug.Log($"Mouse change: {mouseEventData.delta}");
			}
		}
	}
}