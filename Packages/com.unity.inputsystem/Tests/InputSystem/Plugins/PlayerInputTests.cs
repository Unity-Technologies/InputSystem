using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.PlayerInput;
using UnityEngine.Experimental.Input.Plugins.UI;
using UnityEngine.Experimental.Input.Plugins.Users;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Gyroscope = UnityEngine.Experimental.Input.Gyroscope;

/// <summary>
/// Tests for <see cref="PlayerInput"/> and <see cref="PlayerInputManager"/>.
/// </summary>
internal class PlayerInputTests : InputTestFixture
{
    public override void TearDown()
    {
        base.TearDown();

        // Destroy manager, if present.
        if (PlayerInputManager.instance != null)
            Object.DestroyImmediate(PlayerInputManager.instance);

        // Destroy players, if present.
        foreach (var player in PlayerInput.all.ToArray())
            Object.DestroyImmediate(player);

        Assert.That(PlayerInput.all, Is.Empty);
        Assert.That(InputUser.all, Is.Empty);
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanInstantiatePlayer()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var prefab = new GameObject();
        prefab.SetActive(false);
        var prefabPlayerInput = prefab.AddComponent<PlayerInput>();
        prefabPlayerInput.actions = InputActionAsset.FromJson(kActions);

        var player = PlayerInput.Instantiate(prefab);

        Assert.That(player, Is.Not.Null);
        Assert.That(player.playerIndex, Is.EqualTo(0));
        Assert.That(player.actions, Is.SameAs(prefabPlayerInput.actions));
        Assert.That(player.devices, Is.EquivalentTo(new[] { gamepad }));
        Assert.That(player.controlScheme, Is.EqualTo("Gamepad"));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanInstantiatePlayer_WithSpecificControlScheme()
    {
        InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var prefab = new GameObject();
        prefab.SetActive(false);
        var prefabPlayerInput = prefab.AddComponent<PlayerInput>();
        prefabPlayerInput.actions = InputActionAsset.FromJson(kActions);

        var player = PlayerInput.Instantiate(prefab, controlScheme: "Keyboard&Mouse");

        Assert.That(player.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));
        Assert.That(player.controlScheme, Is.EqualTo("Keyboard&Mouse"));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanInstantiatePlayer_WithSpecificDevice()
    {
        var prefab = new GameObject();
        prefab.SetActive(false);
        prefab.AddComponent<PlayerInput>();

        InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Keyboard>();
        InputSystem.AddDevice<Mouse>();

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var instance = PlayerInput.Instantiate(prefab, pairWithDevices: gamepad);

        Assert.That(instance.devices, Is.EquivalentTo(new[] { gamepad }));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanGetAllPlayers()
    {
        Assert.That(PlayerInput.all, Is.Empty);

        var go1 = new GameObject();
        var playerInput1 = go1.AddComponent<PlayerInput>();

        Assert.That(PlayerInput.all, Is.EquivalentTo(new[] { playerInput1 }));

        var go2 = new GameObject();
        var playerInput2 = go2.AddComponent<PlayerInput>();

        Assert.That(PlayerInput.all, Is.EquivalentTo(new[] { playerInput1, playerInput2 }));

        // Should go away if disabled.
        playerInput1.enabled = false;

        Assert.That(PlayerInput.all, Is.EquivalentTo(new[] { playerInput2 }));

        // And reappear if re-enabled.
        playerInput1.enabled = true;

        Assert.That(PlayerInput.all, Is.EquivalentTo(new[] { playerInput2, playerInput1 }));

        Object.DestroyImmediate(go2);

        Assert.That(PlayerInput.all, Is.EquivalentTo(new[] { playerInput1 }));

        Object.DestroyImmediate(go1);

        Assert.That(PlayerInput.all, Is.Empty);
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_PlayersAreSortedByIndex()
    {
        var playerPrefab = new GameObject();
        playerPrefab.SetActive(false);
        playerPrefab.AddComponent<PlayerInput>();

        var player1 = PlayerInput.Instantiate(playerPrefab);
        var player2 = PlayerInput.Instantiate(playerPrefab);

        Assert.That(PlayerInput.all, Is.EquivalentTo(new[] { player1, player2 }));
        Assert.That(PlayerInput.all[0].playerIndex, Is.EqualTo(0));
        Assert.That(PlayerInput.all[1].playerIndex, Is.EqualTo(1));

        Object.DestroyImmediate(player1);

        var player3 = PlayerInput.Instantiate(playerPrefab);

        Assert.That(PlayerInput.all, Is.EquivalentTo(new[] { player3, player2 }));
        Assert.That(PlayerInput.all[0].playerIndex, Is.EqualTo(0));
        Assert.That(PlayerInput.all[1].playerIndex, Is.EqualTo(1));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanAssignActionsToPlayer()
    {
        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();

        var actions = InputActionAsset.FromJson(kActions);
        playerInput.actions = actions;

        Assert.That(playerInput.actions, Is.SameAs(actions));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_AutomaticallyEnablesFirstActionMapInAsset()
    {
        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();

        playerInput.actions = InputActionAsset.FromJson(kActions);

        Assert.That(playerInput.actions.actionMaps[0].enabled, Is.True);
        Assert.That(playerInput.actions.actionMaps[1].enabled, Is.False);
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_AssigningNewActionsToPlayer_DisablesExistingActions()
    {
        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();

        var actions1 = InputActionAsset.FromJson(kActions);
        var actions2 = InputActionAsset.FromJson(kActions);

        playerInput.actions = actions1;

        Assert.That(actions1.actionMaps[0].enabled, Is.True);
        Assert.That(actions2.actionMaps[0].enabled, Is.False);

        playerInput.actions = actions2;

        Assert.That(actions1.actionMaps[0].enabled, Is.False);
        Assert.That(actions2.actionMaps[0].enabled, Is.True);
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_AssigningSameActionsToDifferentPlayers_DuplicatesActions()
    {
        var go1 = new GameObject();
        var playerInput1 = go1.AddComponent<PlayerInput>();

        var go2 = new GameObject();
        var playerInput2 = go2.AddComponent<PlayerInput>();

        var actions = InputActionAsset.FromJson(kActions);

        playerInput1.actions = actions;
        playerInput2.actions = actions;

        Assert.That(playerInput1.actions, Is.Not.SameAs(playerInput2.actions));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanPassivateAndReactivateInputBySendingMessages()
    {
        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();

        playerInput.actions = InputActionAsset.FromJson(kActions);

        Assert.That(playerInput.active, Is.True);

        go.SendMessage("PassivateInput");

        Assert.That(playerInput.active, Is.False);

        go.SendMessage("ActivateInput");

        Assert.That(playerInput.active, Is.True);
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_PairsFirstAvailableDeviceByDefault()
    {
        InputSystem.AddDevice<Gyroscope>(); // Noise. We don't have Gyroscope support so this should get ignored.
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Gamepad>();

        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.actions = InputActionAsset.FromJson(kActions);

        Assert.That(playerInput.devices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(playerInput.actions.devices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(playerInput.user, Is.Not.Null);
        Assert.That(playerInput.user.pairedDevices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(playerInput.user.actions, Is.SameAs(playerInput.actions));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_PrefersDefaultControlSchemeIfAvailable()
    {
        InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Gamepad>();

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.defaultControlScheme = "Keyboard&Mouse";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        Assert.That(playerInput.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));
        Assert.That(playerInput.actions.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));
        Assert.That(playerInput.user, Is.Not.Null);
        Assert.That(playerInput.user.pairedDevices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_PlayerIndex_IsAssignedAutomatically()
    {
        var go1 = new GameObject();
        var playerInput1 = go1.AddComponent<PlayerInput>();

        var go2 = new GameObject();
        var playerInput2 = go2.AddComponent<PlayerInput>();

        Assert.That(playerInput1.playerIndex, Is.EqualTo(0));
        Assert.That(playerInput2.playerIndex, Is.EqualTo(1));

        Object.DestroyImmediate(go1);

        // Adding new player should fill now vacant slot #0.
        var go3 = new GameObject();
        var playerInput3 = go3.AddComponent<PlayerInput>();

        Assert.That(playerInput3.playerIndex, Is.EqualTo(0));
        Assert.That(playerInput2.playerIndex, Is.EqualTo(1));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_PlayerIndex_DoesNotChange()
    {
        var go1 = new GameObject();
        go1.AddComponent<PlayerInput>();

        var go2 = new GameObject();
        var playerInput2 = go2.AddComponent<PlayerInput>();

        Object.DestroyImmediate(go1);

        Assert.That(playerInput2.playerIndex, Is.EqualTo(1));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanReceiveMessageWhenActionIsTriggered()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var go = new GameObject();
        var listener = go.AddComponent<MessageListener>();
        var playerInput = go.AddComponent<PlayerInput>();

        playerInput.notificationBehavior = PlayerNotifications.SendMessages;
        playerInput.actions = InputActionAsset.FromJson(kActions);

        Press(gamepad.buttonSouth);

        Assert.That(listener.messages, Is.EquivalentTo(new[] {new Message("OnFire", 1f)}));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanReceiveEventWhenActionIsTriggered()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var go = new GameObject();
        var listener = go.AddComponent<MessageListener>();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;
        playerInput.actions = InputActionAsset.FromJson(kActions);
        listener.SetUpEvents(playerInput);

        Press(gamepad.buttonSouth);

        Assert.That(listener.messages,
            Is.EquivalentTo(new[] {new Message("gameplay/fire", 1f)}));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanReceiveDeviceLostMessage()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var go = new GameObject();
        var listener = go.AddComponent<MessageListener>();
        var playerInput = go.AddComponent<PlayerInput>();

        playerInput.notificationBehavior = PlayerNotifications.SendMessages;
        playerInput.actions = InputActionAsset.FromJson(kActions);

        Assert.That(playerInput.devices, Is.EquivalentTo(new[] { gamepad }));

        InputSystem.RemoveDevice(gamepad);

        Assert.That(playerInput.devices, Is.Empty);
        Assert.That(playerInput.hasMissingRequiredDevices, Is.True);
        Assert.That(listener.messages,
            Is.EquivalentTo(new[] {new Message(PlayerInput.DeviceLostMessage, playerInput)}));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanReceiveJoinMessagesThroughPlayerManager()
    {
        var go1 = new GameObject();
        var playerInput1 = go1.AddComponent<PlayerInput>();

        var manager = new GameObject();
        var listener = manager.AddComponent<MessageListener>();
        manager.AddComponent<PlayerInputManager>();

        // Should get join message for player who had already joined.
        Assert.That(listener.messages, Is.EquivalentTo(new[] {new Message("OnPlayerJoined", playerInput1)}));

        listener.messages.Clear();

        var go2 = new GameObject();
        var playerInput2 = go2.AddComponent<PlayerInput>();

        var go3 = new GameObject();
        var playerInput3 = go3.AddComponent<PlayerInput>();

        Assert.That(listener.messages,
            Is.EquivalentTo(new[]
            {
                new Message("OnPlayerJoined", playerInput2),
                new Message("OnPlayerJoined", playerInput3)
            }));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanJoinPlayersThroughButtonPress()
    {
        var playerPrefab = new GameObject();
        playerPrefab.SetActive(false);
        playerPrefab.AddComponent<PlayerInput>();

        var manager = new GameObject();
        var listener = manager.AddComponent<MessageListener>();
        var managerComponent = manager.AddComponent<PlayerInputManager>();
        managerComponent.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
        managerComponent.playerPrefab = playerPrefab;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        // First wiggle stick and make sure it does NOT result in a join.
        Set(gamepad.leftStick.x, 0.5f);
        Set(gamepad.leftStick.y, 0.5f);

        Assert.That(PlayerInput.all, Is.Empty);
        Assert.That(listener.messages, Is.Empty);

        // Now press button and make sure it DOES result in a join.
        Press(gamepad.buttonSouth);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(1));
        Assert.That(listener.messages, Is.EquivalentTo(new[] { new Message("OnPlayerJoined", PlayerInput.all[0])}));

        // Press another button and make sure it does NOT result in anything.

        listener.messages.Clear();

        Set(gamepad.buttonSouth, 0);
        Press(gamepad.buttonNorth);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(1));
        Assert.That(listener.messages, Is.Empty);
    }

    // If a player presses a button on a device that can't be used with the player's actions, the join
    // is refused.
    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_JoiningPlayerThroughButtonPress_WillFailIfDeviceIsNotUsableWithPlayerActions()
    {
        var playerPrefab = new GameObject();
        playerPrefab.SetActive(false);
        playerPrefab.AddComponent<PlayerInput>();
        playerPrefab.GetComponent<PlayerInput>().actions = InputActionAsset.FromJson(kActions);

        var manager = new GameObject();
        var listener = manager.AddComponent<MessageListener>();
        var managerComponent = manager.AddComponent<PlayerInputManager>();
        managerComponent.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
        managerComponent.playerPrefab = playerPrefab;

        // Create a device based on the HID layout with a single button control.
        const string kLayout = @"
            {
                ""name"" : ""TestDevice"",
                ""extend"" : ""HID"",
                ""controls"" : [
                    { ""name"" : ""button"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(kLayout);
        var device = InputSystem.AddDevice("TestDevice");

        using (StateEvent.From(device, out var eventPtr))
        {
            ((ButtonControl)device["button"]).WriteValueIntoEvent(1f, eventPtr);
            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();
        }

        Assert.That(listener.messages, Is.Empty);
        Assert.That(PlayerInput.all, Is.Empty);
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanJoinPlayersThroughAction()
    {
        var playerPrefab = new GameObject();
        playerPrefab.SetActive(false);
        playerPrefab.AddComponent<PlayerInput>();

        // Perform join when there is *any* activity on the gamepad.
        // NOTE: This uses "<Gamepad>" instead of "<Gamepad>/*". The latter binds to each control
        //       individually whereas the former just binds to the gamepad as a single control.
        // NOTE: We don't enable the action here. PlayerInputManager should do that in sync with
        //       enabling/disabling joins.
        var joinAction = new InputAction(binding: "<Gamepad>");

        var manager = new GameObject();
        var listener = manager.AddComponent<MessageListener>();
        var managerComponent = manager.AddComponent<PlayerInputManager>();
        managerComponent.joinAction = joinAction;
        managerComponent.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenJoinActionIsTriggered;
        managerComponent.playerPrefab = playerPrefab;

        Assert.That(joinAction.enabled, Is.True);

        var gamepad = InputSystem.AddDevice<Gamepad>();
        Set(gamepad.leftStick.x, 0.5f);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(1));
        Assert.That(listener.messages, Is.EquivalentTo(new[] { new Message("OnPlayerJoined", PlayerInput.all[0])}));

        listener.messages.Clear();

        // Press button and make sure it does NOT result in anything.
        Press(gamepad.buttonNorth);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(1));
        Assert.That(listener.messages, Is.Empty);

        // Disable joining and make sure the action gets disabled.
        managerComponent.DisableJoining();

        Assert.That(joinAction.enabled, Is.False);
    }

    [Test]
    [Category("PlayerInput")]
    [Ignore("TODO")]
    public void TODO_PlayerInput_CanJoinPlayersThroughUI()
    {
        Assert.Fail();
    }

    [Test]
    [Category("PlayerInput")]
    [Ignore("TODO")]
    public void TODO_PlayerInput_CanLimitMaxPlayerCount()
    {
        Assert.Fail();
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_HasNoPlayerCountLimitByDefault()
    {
        var go = new GameObject();
        var manager = go.AddComponent<PlayerInputManager>();

        Assert.That(manager.maxPlayerCount, Is.LessThan(0));
    }

    // This tests the core of the PlayerInputManager logic that dynamically scales split-screen
    // configurations up and down.
    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanSetUpSplitScreen()
    {
        var actions = InputActionAsset.FromJson(kActions);

        var playerPrefab = new GameObject();
        playerPrefab.SetActive(false);
        playerPrefab.AddComponent<PlayerInput>();
        playerPrefab.AddComponent<Camera>();
        playerPrefab.GetComponent<PlayerInput>().camera = playerPrefab.GetComponent<Camera>();
        playerPrefab.GetComponent<PlayerInput>().actions = actions;

        var manager = new GameObject();
        var managerComponent = manager.AddComponent<PlayerInputManager>();
        managerComponent.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
        managerComponent.playerPrefab = playerPrefab;
        managerComponent.splitScreen = true;

        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        var gamepad3 = InputSystem.AddDevice<Gamepad>();
        var gamepad4 = InputSystem.AddDevice<Gamepad>();

        // Join two players and make sure we get two screen side-by-side.
        Press(gamepad1.buttonSouth);
        Press(gamepad2.buttonSouth);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(2));

        Assert.That(PlayerInput.all[0].splitScreenIndex, Is.EqualTo(0));
        Assert.That(PlayerInput.all[1].splitScreenIndex, Is.EqualTo(1));

        // Player #1: Upper Left.
        Assert.That(PlayerInput.all[0].camera.rect.x, Is.EqualTo(0));
        Assert.That(PlayerInput.all[0].camera.rect.y, Is.EqualTo(0));
        Assert.That(PlayerInput.all[0].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[0].camera.rect.height, Is.EqualTo(1));

        // Player #2: Upper Right.
        Assert.That(PlayerInput.all[1].camera.rect.x, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.y, Is.EqualTo(0));
        Assert.That(PlayerInput.all[1].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.height, Is.EqualTo(1));

        // Add one more player and make sure we got a 2x2 setup.
        Press(gamepad3.buttonSouth);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(3));

        Assert.That(PlayerInput.all[0].splitScreenIndex, Is.EqualTo(0));
        Assert.That(PlayerInput.all[1].splitScreenIndex, Is.EqualTo(1));
        Assert.That(PlayerInput.all[2].splitScreenIndex, Is.EqualTo(2));

        // Player #1: Upper Left.
        Assert.That(PlayerInput.all[0].camera.rect.x, Is.EqualTo(0));
        Assert.That(PlayerInput.all[0].camera.rect.y, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[0].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[0].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #2: Upper Right.
        Assert.That(PlayerInput.all[1].camera.rect.x, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.y, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #3: Lower Left.
        Assert.That(PlayerInput.all[2].camera.rect.x, Is.EqualTo(0));
        Assert.That(PlayerInput.all[2].camera.rect.y, Is.EqualTo(0));
        Assert.That(PlayerInput.all[2].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[2].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Join one more player and make sure we got a fully filled 2x2 setup.
        Press(gamepad4.buttonSouth);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(4));

        Assert.That(PlayerInput.all[0].splitScreenIndex, Is.EqualTo(0));
        Assert.That(PlayerInput.all[1].splitScreenIndex, Is.EqualTo(1));
        Assert.That(PlayerInput.all[2].splitScreenIndex, Is.EqualTo(2));
        Assert.That(PlayerInput.all[3].splitScreenIndex, Is.EqualTo(3));

        // Player #1: Upper Left.
        Assert.That(PlayerInput.all[0].camera.rect.x, Is.EqualTo(0));
        Assert.That(PlayerInput.all[0].camera.rect.y, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[0].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[0].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #2: Upper Right.
        Assert.That(PlayerInput.all[1].camera.rect.x, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.y, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #3: Lower Left.
        Assert.That(PlayerInput.all[2].camera.rect.x, Is.EqualTo(0));
        Assert.That(PlayerInput.all[2].camera.rect.y, Is.EqualTo(0));
        Assert.That(PlayerInput.all[2].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[2].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #4: Lower Right.
        Assert.That(PlayerInput.all[3].camera.rect.x, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[3].camera.rect.y, Is.EqualTo(0));
        Assert.That(PlayerInput.all[3].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[3].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Unjoin the player in the upper right and make sure the other players stay where they are.
        Object.DestroyImmediate(PlayerInput.all[1].gameObject);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(3));

        Assert.That(PlayerInput.all[0].splitScreenIndex, Is.EqualTo(0));
        Assert.That(PlayerInput.all[1].splitScreenIndex, Is.EqualTo(2));
        Assert.That(PlayerInput.all[2].splitScreenIndex, Is.EqualTo(3));

        // Player #1: Upper Left.
        Assert.That(PlayerInput.all[0].camera.rect.x, Is.EqualTo(0));
        Assert.That(PlayerInput.all[0].camera.rect.y, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[0].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[0].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #3: Lower Left.
        Assert.That(PlayerInput.all[1].camera.rect.x, Is.EqualTo(0));
        Assert.That(PlayerInput.all[1].camera.rect.y, Is.EqualTo(0));
        Assert.That(PlayerInput.all[1].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #4: Lower Right.
        Assert.That(PlayerInput.all[2].camera.rect.x, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[2].camera.rect.y, Is.EqualTo(0));
        Assert.That(PlayerInput.all[2].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[2].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Join a new player and make sure the upper right slot gets filled.
        Press(gamepad2.buttonSouth);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(4));

        Assert.That(PlayerInput.all[0].splitScreenIndex, Is.EqualTo(0));
        Assert.That(PlayerInput.all[1].splitScreenIndex, Is.EqualTo(1));
        Assert.That(PlayerInput.all[2].splitScreenIndex, Is.EqualTo(2));
        Assert.That(PlayerInput.all[3].splitScreenIndex, Is.EqualTo(3));

        // Player #1: Upper Left.
        Assert.That(PlayerInput.all[0].camera.rect.x, Is.EqualTo(0));
        Assert.That(PlayerInput.all[0].camera.rect.y, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[0].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[0].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #2: Upper Right.
        Assert.That(PlayerInput.all[1].camera.rect.x, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.y, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #3: Lower Left.
        Assert.That(PlayerInput.all[2].camera.rect.x, Is.EqualTo(0));
        Assert.That(PlayerInput.all[2].camera.rect.y, Is.EqualTo(0));
        Assert.That(PlayerInput.all[2].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[2].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #4: Lower Right.
        Assert.That(PlayerInput.all[3].camera.rect.x, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[3].camera.rect.y, Is.EqualTo(0));
        Assert.That(PlayerInput.all[3].camera.rect.width, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[3].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Join yet another player and make sure the split-screen setup goes to 3x2.
        var gamepad5 = InputSystem.AddDevice<Gamepad>();
        Press(gamepad5.buttonSouth);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(5));

        Assert.That(PlayerInput.all[0].splitScreenIndex, Is.EqualTo(0));
        Assert.That(PlayerInput.all[1].splitScreenIndex, Is.EqualTo(1));
        Assert.That(PlayerInput.all[2].splitScreenIndex, Is.EqualTo(2));
        Assert.That(PlayerInput.all[3].splitScreenIndex, Is.EqualTo(3));
        Assert.That(PlayerInput.all[4].splitScreenIndex, Is.EqualTo(4));

        // Player #1: Upper Left.
        Assert.That(PlayerInput.all[0].camera.rect.x, Is.EqualTo(0));
        Assert.That(PlayerInput.all[0].camera.rect.y, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[0].camera.rect.width, Is.EqualTo(1 / 3.0).Within(0.00001));
        Assert.That(PlayerInput.all[0].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #2: Upper Middle.
        Assert.That(PlayerInput.all[1].camera.rect.x, Is.EqualTo(1 / 3.0).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.y, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.width, Is.EqualTo(1 / 3.0).Within(0.00001));
        Assert.That(PlayerInput.all[1].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #3: Upper Right.
        Assert.That(PlayerInput.all[2].camera.rect.x, Is.EqualTo(2 * (1 / 3.0)).Within(0.00001));
        Assert.That(PlayerInput.all[2].camera.rect.y, Is.EqualTo(0.5).Within(0.00001));
        Assert.That(PlayerInput.all[2].camera.rect.width, Is.EqualTo(1 / 3.0).Within(0.00001));
        Assert.That(PlayerInput.all[2].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));

        // Player #4: Lower Left.
        Assert.That(PlayerInput.all[3].camera.rect.x, Is.EqualTo(0));
        Assert.That(PlayerInput.all[3].camera.rect.y, Is.EqualTo(0));
        Assert.That(PlayerInput.all[3].camera.rect.width, Is.EqualTo(1 / 3.0).Within(0.00001));
        Assert.That(PlayerInput.all[3].camera.rect.height, Is.EqualTo(0.5).Within(0.00001));
    }

    [Test]
    [Category("PlayerInput")]
    [Ignore("TODO")]
    public void TODO_PlayerInput_CanSetUpSplitScreen_AndMaintainAspectRatio()
    {
        Assert.Fail();
    }

    [Test]
    [Category("PlayerInput")]
    [Ignore("TODO")]
    public void TODO_PlayerInput_CanSetUpSplitScreen_AndMaintainFixedNumberOfScreens()
    {
        Assert.Fail();
    }

    [Test]
    [Category("PlayerInput")]
    [Ignore("TODO")]
    public void TODO_PlayerInput_CanSetUpSplitScreen_AndManuallyAllocatePlayersToScreens()
    {
        Assert.Fail();
    }

    [Test]
    [Category("PlayerInput")]
    [Ignore("TODO")]
    public void TODO_PlayerInput_TriggeringAction_DoesNotAllocate()
    {
        Assert.Fail();
    }

    private const string kActions = @"
        {
            ""maps"" : [
                {
                    ""name"" : ""gameplay"",
                    ""actions"" : [
                        { ""name"" : ""fire"" },
                        { ""name"" : ""look"" },
                        { ""name"" : ""move"" }
                    ],
                    ""bindings"" : [
                        { ""path"" : ""<Gamepad>/buttonSouth"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" },
                        { ""path"" : ""<Gamepad>/leftStick"", ""action"" : ""move"", ""groups"" : ""Gamepad"" },
                        { ""path"" : ""<Gamepad>/rightStick"", ""action"" : ""look"", ""groups"" : ""Gamepad"" },
                        { ""path"" : ""dpad"", ""action"" : ""move"", ""groups"" : ""Gamepad"", ""isComposite"" : true },
                        { ""path"" : ""<Keyboard>/a"", ""name"" : ""left"", ""groups"" : ""Keyboard&Mouse"", ""isPartOfComposite"" : true },
                        { ""path"" : ""<Keyboard>/d"", ""name"" : ""right"", ""groups"" : ""Keyboard&Mouse"", ""isPartOfComposite"" : true },
                        { ""path"" : ""<Keyboard>/w"", ""name"" : ""up"", ""groups"" : ""Keyboard&Mouse"", ""isPartOfComposite"" : true },
                        { ""path"" : ""<Keyboard>/s"", ""name"" : ""down"", ""groups"" : ""Keyboard&Mouse"", ""isPartOfComposite"" : true },
                        { ""path"" : ""<Mouse>/delta"", ""action"" : ""look"", ""groups"" : ""Keyboard&Mouse"" }
                    ]
                },
                {
                    ""name"" : ""ui"",
                    ""actions"" : [
                        { ""name"" : ""navigate"" }
                    ],
                    ""bindings"" : [
                        { ""path"" : ""<Gamepad>/dpad"", ""action"" : ""navigate"", ""groups"" : ""Gamepad"" }
                    ]
                },
                {
                    ""name"" : ""other"",
                    ""actions"" : [
                        { ""name"" : ""action"" }
                    ],
                    ""bindings"" : [
                        { ""path"" : ""<Gamepad>/leftTrigger"", ""action"" : ""action"", ""groups"" : ""Gamepad"" }
                    ]
                }
            ],
            ""controlSchemes"" : [
                {
                    ""name"" : ""Gamepad"",
                    ""bindingGroup"" : ""Gamepad"",
                    ""devices"" : [
                        { ""devicePath"" : ""<Gamepad>"" }
                    ]
                },
                {
                    ""name"" : ""Keyboard&Mouse"",
                    ""bindingGroup"" : ""Keyboard&Mouse"",
                    ""devices"" : [
                        { ""devicePath"" : ""<Keyboard>"" },
                        { ""devicePath"" : ""<Mouse>"" }
                    ]
                }
            ]
        }
    ";

    private struct Message : IEquatable<Message>
    {
        public string name;
        public object value;

        public Message(string name, object value = null)
        {
            this.name = name;
            this.value = value;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(name))
                return base.ToString();

            return $"{name}({value})";
        }

        public bool Equals(Message other)
        {
            return string.Equals(name, other.name) && Equals(value, other.value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Message && Equals((Message)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((name != null ? name.GetHashCode() : 0) * 397) ^ (value != null ? value.GetHashCode() : 0);
            }
        }
    }

    private class MessageListener : MonoBehaviour
    {
        public List<Message> messages = new List<Message>();

        public void SetUpEvents(PlayerInput player)
        {
            var fireAction = player.actions.FindAction("gameplay/fire");
            var lookAction = player.actions.FindAction("gameplay/look");
            var moveAction = player.actions.FindAction("gameplay/move");

            var fireEvent = new PlayerInput.ActionEvent(fireAction);
            var lookEvent = new PlayerInput.ActionEvent(lookAction);
            var moveEvent = new PlayerInput.ActionEvent(moveAction);

            fireEvent.AddListener(OnFireEvent);
            lookEvent.AddListener(OnLookEvent);
            moveEvent.AddListener(OnMoveEvent);

            player.actionEvents = new[]
            {
                fireEvent,
                lookEvent,
                moveEvent,
            };
        }

        public void OnFireEvent(InputAction.CallbackContext context)
        {
            messages.Add(new Message { name = "gameplay/fire", value = context.ReadValue<float>() });
        }

        public void OnLookEvent(InputAction.CallbackContext context)
        {
            messages.Add(new Message { name = "gameplay/look", value = context.ReadValue<Vector2>() });
        }

        public void OnMoveEvent(InputAction.CallbackContext context)
        {
            messages.Add(new Message { name = "gameplay/move", value = context.ReadValue<Vector2>() });
        }

        // ReSharper disable once UnusedMember.Local
        public void OnFire(InputValue value)
        {
            messages.Add(new Message { name = "OnFire", value = value.Get<float>() });
        }

        // ReSharper disable once UnusedMember.Local
        public void OnLook(InputValue value)
        {
            messages.Add(new Message { name = "OnLook", value = value.Get<Vector2>() });
        }

        // ReSharper disable once UnusedMember.Local
        public void OnMove(InputValue value)
        {
            messages.Add(new Message { name = "OnMove", value = value.Get<Vector2>() });
        }

        // ReSharper disable once UnusedMember.Local
        public void OnDeviceLost(PlayerInput player)
        {
            Debug.Log("OnDeviceLost");
            messages.Add(new Message { name = "OnDeviceLost", value = player});
        }

        // ReSharper disable once UnusedMember.Local
        public void OnDeviceRegained(PlayerInput player)
        {
            messages.Add(new Message { name = "OnDeviceRegained", value = player});
        }

        // ReSharper disable once UnusedMember.Local
        public void OnPlayerJoined(PlayerInput player)
        {
            messages.Add(new Message { name = "OnPlayerJoined", value = player});
        }

        // ReSharper disable once UnusedMember.Local
        public void OnPlayerLeft(PlayerInput player)
        {
            messages.Add(new Message { name = "OnPlayerLeft", value = player});
        }
    }

    private class EventListener : MonoBehaviour
    {
        public List<Message> messages = new List<Message>();

        public void OnEnable()
        {
            var playerInput = GetComponent<PlayerInput>();
            Debug.Assert(playerInput != null, "Must have PlayerInput component");

            foreach (var item in playerInput.actionEvents)
                item.AddListener(OnAction);

            playerInput.deviceLostEvent.AddListener(OnDeviceLost);
        }

        public void OnAction(InputAction.CallbackContext context)
        {
            messages.Add(new Message {name = context.action.ToString()});
        }

        public void OnDeviceLost()
        {
            messages.Add(new Message {name = "OnDeviceLost"});
        }
    }
}
