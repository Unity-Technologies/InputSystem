using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.XInput;
using UnityEngine.Profiling;
using UnityEngine.TestTools.Constraints;
using Object = UnityEngine.Object;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;
using Is = UnityEngine.TestTools.Constraints.Is;

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
        Assert.That(player.currentControlScheme, Is.EqualTo("Gamepad"));
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
        Assert.That(player.currentControlScheme, Is.EqualTo("Keyboard&Mouse"));
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
    public void PlayerInput_CanLinkSpecificDeviceToUI()
    {
        var prefab = new GameObject();
        prefab.SetActive(false);
        var player = prefab.AddComponent<PlayerInput>();
        var ui = prefab.AddComponent<InputSystemUIInputModule>();
        player.uiInputModule = ui;
        player.actions = InputActionAsset.FromJson(kActions);
        ui.actionsAsset = player.actions;

        InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Keyboard>();
        InputSystem.AddDevice<Mouse>();

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var instance = PlayerInput.Instantiate(prefab, pairWithDevices: gamepad);

        Assert.That(instance.devices, Is.EquivalentTo(new[] { gamepad }));
        Assert.That(ui.actionsAsset.devices, Is.EquivalentTo(new[] { gamepad }));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanUseSameActionsForUIInputModule()
    {
        var actions = InputActionAsset.FromJson(kActions);
        var mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.AddDevice<Keyboard>();

        var eventSystemGO = new GameObject();
        eventSystemGO.SetActive(false);
        eventSystemGO.AddComponent<EventSystem>();
        var uiModule = eventSystemGO.AddComponent<InputSystemUIInputModule>();
        uiModule.actionsAsset = actions;
        uiModule.move = InputActionReference.Create(actions["UI/Navigate"]);
        uiModule.point = InputActionReference.Create(actions["UI/Point"]);
        uiModule.leftClick = InputActionReference.Create(actions["UI/Click"]);

        var playerGO = new GameObject();
        playerGO.SetActive(false);
        var player = playerGO.AddComponent<PlayerInput>();
        player.actions = actions;
        player.defaultActionMap = "Gameplay";
        player.defaultControlScheme = "Keyboard&Mouse";

        eventSystemGO.SetActive(true);
        playerGO.SetActive(true);

        Assert.That(actions.FindActionMap("Gameplay").enabled, Is.True);
        Assert.That(actions.FindActionMap("UI").enabled, Is.True);
        Assert.That(actions["UI/Navigate"].controls, Is.Empty);
        Assert.That(actions["UI/Point"].controls, Is.EquivalentTo(new[] { mouse.position }));
        Assert.That(actions["UI/Click"].controls, Is.EquivalentTo(new[] { mouse.leftButton }));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanInstantiatePlayer_WithSpecificDevice_AndAutomaticallyChooseControlScheme()
    {
        var prefab = new GameObject();
        prefab.SetActive(false);
        prefab.AddComponent<PlayerInput>();
        prefab.GetComponent<PlayerInput>().actions = InputActionAsset.FromJson(kActions);

        InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var instance = PlayerInput.Instantiate(prefab, pairWithDevices: new InputDevice[] { keyboard, mouse });

        Assert.That(instance.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));
        Assert.That(instance.currentControlScheme, Is.EqualTo("Keyboard&Mouse"));
        Assert.That(instance.actions["gameplay/fire"].controls, Has.Count.EqualTo(1));
        Assert.That(instance.actions["gameplay/fire"].controls[0], Is.SameAs(mouse.leftButton));
        Assert.That(instance.actions["gameplay/look"].controls, Has.Count.EqualTo(1));
        Assert.That(instance.actions["gameplay/look"].controls[0], Is.SameAs(mouse.delta));
        Assert.That(instance.actions["gameplay/move"].controls, Has.Count.EqualTo(4));
        Assert.That(instance.actions["gameplay/move"].controls, Has.Exactly(1).SameAs(keyboard.wKey));
        Assert.That(instance.actions["gameplay/move"].controls, Has.Exactly(1).SameAs(keyboard.sKey));
        Assert.That(instance.actions["gameplay/move"].controls, Has.Exactly(1).SameAs(keyboard.aKey));
        Assert.That(instance.actions["gameplay/move"].controls, Has.Exactly(1).SameAs(keyboard.dKey));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanHaveActionWithNoBindingsInOneControlScheme()
    {
        const string kActions = @"
            {
                ""maps"" : [
                    {
                        ""name"" : ""gameplay"",
                        ""actions"" : [
                            { ""name"" : ""Fire"", ""type"" : ""button"" }
                        ],
                        ""bindings"" : [
                            { ""path"" : ""<Gamepad>/buttonSouth"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" }
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
        var prefab = new GameObject();
        prefab.SetActive(false);
        prefab.AddComponent<PlayerInput>();
        prefab.GetComponent<PlayerInput>().actions = InputActionAsset.FromJson(kActions);
        prefab.GetComponent<PlayerInput>().defaultActionMap = "gameplay";
        prefab.AddComponent<MessageListener>();

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var instance = PlayerInput.Instantiate(prefab, pairWithDevices: new InputDevice[] { keyboard, mouse });
        var listener = instance.GetComponent<MessageListener>();

        Press(gamepad.buttonSouth);

        Assert.That(listener.messages, Is.EquivalentTo(new[]
        {
            new Message("OnControlsChanged", instance),
            new Message("OnFire", 1f)
        }));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanBeUsedWithoutControlSchemes()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.AddDevice<Mouse>(); // Noise.

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var playActions = new InputActionMap("play");
        var fireAction = playActions.AddAction("fire");
        fireAction.AddBinding("<Gamepad>/<Button>"); // Match multiple controls in single binding to make sure that isn't throwing PlayerInput off.
        fireAction.AddBinding("<Keyboard>/space");
        actions.AddActionMap(playActions);

        var go = new GameObject();
        go.SetActive(false);
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.actions = actions;
        go.SetActive(true);

        Assert.That(playerInput.devices, Has.Count.EqualTo(2));
        Assert.That(playerInput.devices, Has.Exactly(1).SameAs(gamepad));
        Assert.That(playerInput.devices, Has.Exactly(1).SameAs(keyboard));

        // Make sure that we restore pairing even if the device goes
        // away temporarily.

        InputSystem.RemoveDevice(gamepad);

        Assert.That(playerInput.devices, Has.Count.EqualTo(1));
        Assert.That(playerInput.devices, Has.Exactly(1).SameAs(keyboard));

        InputSystem.AddDevice(gamepad);

        Assert.That(playerInput.devices, Has.Count.EqualTo(2));
        Assert.That(playerInput.devices, Has.Exactly(1).SameAs(gamepad));
        Assert.That(playerInput.devices, Has.Exactly(1).SameAs(keyboard));

        // Also, if we add another device now, it should get picked up, too. Note that
        // this is special about the case of not using control schemes. When having control
        // schemes, we switch in single-player entirely based on control schemes. When *not*
        // having control schemes, we greedily grab everything that is compatible with the
        // bindings we have.
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        Assert.That(playerInput.devices, Has.Count.EqualTo(3));
        Assert.That(playerInput.devices, Has.Exactly(1).SameAs(gamepad));
        Assert.That(playerInput.devices, Has.Exactly(1).SameAs(gamepad2));
        Assert.That(playerInput.devices, Has.Exactly(1).SameAs(keyboard));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanChangeActionsWhileEnabled()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var go = new GameObject();
        go.SetActive(false);
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.actions = InputActionAsset.FromJson(kActions);
        go.SetActive(true);

        Assert.That(playerInput.devices, Is.EquivalentTo(new[] {gamepad}));

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var playActions = new InputActionMap("play");
        var fireAction = playActions.AddAction("fire");
        fireAction.AddBinding("<Keyboard>/space");
        actions.AddActionMap(playActions);

        playerInput.actions = actions;

        Assert.That(playerInput.devices, Is.EquivalentTo(new[] { keyboard }));
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
    public void PlayerInput_AssigningNewActionsToPlayer_DisablesExistingActions()
    {
        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();

        var actions1 = InputActionAsset.FromJson(kActions);
        var actions2 = InputActionAsset.FromJson(kActions);

        playerInput.defaultActionMap = "gameplay";
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
    public void PlayerInput_AssigningSameActionsToDifferentPlayers_DuplicatesOverrides()
    {
        var go1 = new GameObject();
        var playerInput1 = go1.AddComponent<PlayerInput>();

        var go2 = new GameObject();
        var playerInput2 = go2.AddComponent<PlayerInput>();

        var actions = InputActionAsset.FromJson(kActions);
        actions.actionMaps[0].actions[0].ApplyBindingOverride(0, "<Gamepad>/buttonNorth");

        playerInput1.actions = actions;
        playerInput2.actions = actions;

        Assert.That(playerInput1.actions, Is.Not.SameAs(playerInput2.actions));
        Assert.That(playerInput1.actions.actionMaps[0].actions[0].bindings[0].overridePath, Is.SameAs("<Gamepad>/buttonNorth"));
        Assert.That(playerInput2.actions.actionMaps[0].actions[0].bindings[0].overridePath, Is.SameAs("<Gamepad>/buttonNorth"));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_DuplicatingActions_AssignsNewInstanceToUI()
    {
        var go1 = new GameObject();
        var playerInput1 = go1.AddComponent<PlayerInput>();
        var ui1 = go1.AddComponent<InputSystemUIInputModule>();
        playerInput1.uiInputModule = ui1;

        var go2 = new GameObject();
        var playerInput2 = go2.AddComponent<PlayerInput>();
        var ui2 = go1.AddComponent<InputSystemUIInputModule>();
        playerInput2.uiInputModule = ui2;

        var actions = InputActionAsset.FromJson(kActions);

        ui1.actionsAsset = actions;
        playerInput1.actions = actions;
        ui2.actionsAsset = actions;
        playerInput2.actions = actions;

        Assert.That(playerInput1.actions, Is.Not.SameAs(playerInput2.actions));
        Assert.That(playerInput1.actions, Is.SameAs(ui1.actionsAsset));
        Assert.That(playerInput2.actions, Is.SameAs(ui2.actionsAsset));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanPassivateAndReactivateInputBySendingMessages()
    {
        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();

        playerInput.actions = InputActionAsset.FromJson(kActions);

        Assert.That(playerInput.inputIsActive, Is.True);

        go.SendMessage("DeactivateInput");

        Assert.That(playerInput.inputIsActive, Is.False);

        go.SendMessage("ActivateInput");

        Assert.That(playerInput.inputIsActive, Is.True);
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_PassivatingActionsWillOnlyDisableActionsPlayerInputEnabledItself()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.defaultActionMap = "gameplay";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        var moveAction = playerInput.actions.FindAction("move");
        var navigateAction = playerInput.actions.FindAction("navigate");
        var gameplayActions = playerInput.actions.FindActionMap("gameplay");
        Set(gamepad.leftTrigger, 0.234f);

        Assert.That(playerInput.inputIsActive, Is.True);
        Assert.That(gameplayActions.enabled, Is.True);
        Assert.That(moveAction.enabled, Is.True);
        Assert.That(navigateAction.enabled, Is.False);

        navigateAction.Enable();

        Assert.That(playerInput.inputIsActive, Is.True);
        Assert.That(gameplayActions.enabled, Is.True);
        Assert.That(moveAction.enabled, Is.True);
        Assert.That(navigateAction.enabled, Is.True);

        playerInput.DeactivateInput();

        Assert.That(playerInput.inputIsActive, Is.False);
        Assert.That(gameplayActions.enabled, Is.False);
        Assert.That(moveAction.enabled, Is.False);
        Assert.That(navigateAction.enabled, Is.True);
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_SwitchingActionMapWillOnlyDisableActionsPlayerInputEnabledItself()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.defaultActionMap = "gameplay";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        var moveAction = playerInput.actions.FindAction("move");
        var navigateAction = playerInput.actions.FindAction("navigate");
        var gameplayActions = playerInput.actions.FindActionMap("gameplay");
        var otherActions = playerInput.actions.FindActionMap("other");

        Set(gamepad.leftTrigger, 0.234f);

        Assert.That(playerInput.inputIsActive, Is.True);
        Assert.That(gameplayActions.enabled, Is.True);
        Assert.That(moveAction.enabled, Is.True);
        Assert.That(otherActions.enabled, Is.False);
        Assert.That(navigateAction.enabled, Is.False);

        navigateAction.Enable();

        Assert.That(playerInput.inputIsActive, Is.True);
        Assert.That(gameplayActions.enabled, Is.True);
        Assert.That(moveAction.enabled, Is.True);
        Assert.That(otherActions.enabled, Is.False);
        Assert.That(navigateAction.enabled, Is.True);

        playerInput.currentActionMap = otherActions;

        Assert.That(playerInput.inputIsActive, Is.True);
        Assert.That(gameplayActions.enabled, Is.False);
        Assert.That(moveAction.enabled, Is.False);
        Assert.That(otherActions.enabled, Is.True);
        Assert.That(navigateAction.enabled, Is.True);
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
        go.SetActive(false);
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.defaultControlScheme = "Keyboard&Mouse";
        playerInput.actions = InputActionAsset.FromJson(kActions);
        go.SetActive(true);

        Assert.That(playerInput.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));
        Assert.That(playerInput.actions.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));
        Assert.That(playerInput.user, Is.Not.Null);
        Assert.That(playerInput.user.pairedDevices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));
        Assert.That(playerInput.actions["gameplay/fire"].controls, Has.Count.EqualTo(1));
        Assert.That(playerInput.actions["gameplay/fire"].controls[0], Is.SameAs(mouse.leftButton));
        Assert.That(playerInput.actions["gameplay/look"].controls, Has.Count.EqualTo(1));
        Assert.That(playerInput.actions["gameplay/look"].controls[0], Is.SameAs(mouse.delta));
        Assert.That(playerInput.actions["gameplay/move"].controls, Has.Count.EqualTo(4));
        Assert.That(playerInput.actions["gameplay/move"].controls, Has.Exactly(1).SameAs(keyboard.wKey));
        Assert.That(playerInput.actions["gameplay/move"].controls, Has.Exactly(1).SameAs(keyboard.sKey));
        Assert.That(playerInput.actions["gameplay/move"].controls, Has.Exactly(1).SameAs(keyboard.aKey));
        Assert.That(playerInput.actions["gameplay/move"].controls, Has.Exactly(1).SameAs(keyboard.dKey));
    }

    // If PlayerInputManager has joining disabled (or does not even exist) and there is
    // only a single PlayerInput, then automatic control scheme switching is enabled and the
    // player can freely switch from device to device as long as a device is supported by
    // the actions on the player.
    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanAutoSwitchControlSchemesInSinglePlayer()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var go = new GameObject();
        var listener = go.AddComponent<MessageListener>();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.defaultControlScheme = "Keyboard&Mouse";
        playerInput.defaultActionMap = "gameplay";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        Assert.That(playerInput.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));

        Press(gamepad.buttonSouth);

        Assert.That(playerInput.devices, Is.EquivalentTo(new[] { gamepad }));
        Assert.That(playerInput.user.controlScheme, Is.Not.Null);
        Assert.That(playerInput.user.controlScheme.Value.name, Is.EqualTo("Gamepad"));
        Assert.That(listener.messages, Is.EquivalentTo(new[]
        {
            ////TODO: reduce the steps in which PlayerInput updates the data to result in fewer re-resolves
            new Message("OnControlsChanged", playerInput), // Initial resolve.
            new Message("OnControlsChanged", playerInput), // Control scheme switch.
            new Message("OnFire", 1f)
        }));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanAutoSwitchControlSchemesInSinglePlayer_WithSomeDevicesSharedBetweenSchemes()
    {
        InputSystem.AddDevice<Touchscreen>(); // Noise.
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.AddDevice<Gamepad>(); // Noise.

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var actionMap = actions.AddActionMap("gameplay");
        actionMap.AddAction("action", binding: "<Gamepad>/buttonSouth");

        actions.AddControlScheme(new InputControlScheme("KeyboardMouse").WithRequiredDevice("<Keyboard>").WithRequiredDevice("<Mouse>"));
        actions.AddControlScheme(new InputControlScheme("GamepadMouse").WithRequiredDevice("<Gamepad>").WithRequiredDevice("<Mouse>"));

        var go = new GameObject();
        go.SetActive(false);
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.defaultActionMap = "gameplay";
        playerInput.defaultControlScheme = "KeyboardMouse";
        playerInput.actions = actions;
        go.SetActive(true);

        Assert.That(playerInput.currentControlScheme, Is.EqualTo("KeyboardMouse"));
        Assert.That(playerInput.devices, Has.Count.EqualTo(2));
        Assert.That(playerInput.devices, Has.Exactly(1).SameAs(keyboard));
        Assert.That(playerInput.devices, Has.Exactly(1).SameAs(mouse));

        Press(gamepad.buttonSouth);

        Assert.That(playerInput.currentControlScheme, Is.EqualTo("GamepadMouse"));
        Assert.That(playerInput.devices, Has.Count.EqualTo(2));
        Assert.That(playerInput.devices, Has.Exactly(1).SameAs(gamepad));
        Assert.That(playerInput.devices, Has.Exactly(1).SameAs(mouse));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanAutoSwitchControlSchemesInSinglePlayer_WithDevicePluggedInAfterStart()
    {
        var go = new GameObject();
        go.SetActive(false);
        var listener = go.AddComponent<MessageListener>();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.defaultActionMap = "gameplay";
        playerInput.actions = InputActionAsset.FromJson(kActions);
        go.SetActive(true);

        Assert.That(playerInput.devices, Is.Empty);

        var gamepad1 = InputSystem.AddDevice<Gamepad>();

        // Just plugging in the device shouldn't result in a switch.
        Assert.That(playerInput.devices, Is.Empty);

        // But moving the stick should.
        Set(gamepad1.leftStick, new Vector2(0.234f, 0.345f));

        Assert.That(playerInput.devices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(playerInput.user.controlScheme, Is.Not.Null);
        Assert.That(playerInput.user.controlScheme.Value.name, Is.EqualTo("Gamepad"));
        Assert.That(listener.messages,
            Is.EquivalentTo(new[]
            {
                new Message("OnControlsChanged", playerInput),
                new Message("OnMove", new StickDeadzoneProcessor().Process(new Vector2(0.234f, 0.345f)))
            }));

        listener.messages.Clear();

        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        // We need to reach *higher* actuation than gamepad1's left stick. This is a bit of an
        // artificial thing. In reality, the player would let go of the stick on the first gamepad
        // and then pick up the second gamepad and actuate the stick there. However, even if the
        // situation arises where both are actuated, we still do the "right" thing and stick to the
        // one that is actuated more strongly.
        Set(gamepad2.leftStick, new Vector2(0.345f, 0.456f));

        Assert.That(playerInput.devices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(playerInput.user.controlScheme, Is.Not.Null);
        Assert.That(playerInput.user.controlScheme.Value.name, Is.EqualTo("Gamepad"));
        Assert.That(listener.messages,
            Is.EquivalentTo(new[]
                // This looks counter-intuitive but what happens is that when switching from gamepad1 to gamepad2,
                // the system will first cancel ongoing actions. So it'll cancel "Move" which, given how PlayerInput
                // sends messages, will simply come out as another "Move" with a zero value.
            {
                new Message("OnMove", new StickDeadzoneProcessor().Process(new Vector2(0.0f, 0.0f))),
                new Message("OnControlsChanged", playerInput),
                new Message("OnMove", new StickDeadzoneProcessor().Process(new Vector2(0.345f, 0.456f)))
            }));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_AutoSwitchingControlSchemesInSinglePlayer_CanBeDisabled()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.neverAutoSwitchControlSchemes = true;
        playerInput.defaultControlScheme = "Keyboard&Mouse";
        playerInput.defaultActionMap = "gameplay";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        Assert.That(playerInput.currentControlScheme, Is.EqualTo("Keyboard&Mouse"));
        Assert.That(playerInput.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));

        Press(gamepad.buttonSouth);

        Assert.That(playerInput.currentControlScheme, Is.EqualTo("Keyboard&Mouse"));
        Assert.That(playerInput.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanSwitchControlSchemesManually()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.neverAutoSwitchControlSchemes = true;
        playerInput.defaultControlScheme = "Keyboard&Mouse";
        playerInput.defaultActionMap = "gameplay";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        Assert.That(playerInput.currentControlScheme, Is.EqualTo("Keyboard&Mouse"));
        Assert.That(playerInput.devices, Is.EquivalentTo(new InputDevice[] { keyboard, mouse }));

        var result = playerInput.SwitchCurrentControlScheme(gamepad);
        Assert.That(result, Is.True);

        Assert.That(playerInput.currentControlScheme, Is.EqualTo("Gamepad"));
        Assert.That(playerInput.devices, Is.EquivalentTo(new InputDevice[] { gamepad }));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_ByDefaultChoosesMostSpecificControlSchemeAvailable()
    {
        InputSystem.AddDevice<XInputController>();

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        actions.AddControlScheme("GenericGamepad").WithRequiredDevice("<Gamepad>");
        actions.AddControlScheme("XboxGamepad").WithRequiredDevice("<XInputController>");
        actions.AddControlScheme("PS4Gamepad").WithRequiredDevice("<DualShockGamepad>");

        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.actions = actions;

        Assert.That(playerInput.currentControlScheme, Is.EqualTo("XboxGamepad"));

        var ps4Gamepad = InputSystem.AddDevice<DualShockGamepad>();
        Press(ps4Gamepad.buttonSouth);

        Assert.That(playerInput.currentControlScheme, Is.EqualTo("PS4Gamepad"));
    }

    // https://fogbugz.unity3d.com/f/cases/1214519/
    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanHaveSpacesAndSpecialCharactersInActionNames()
    {
        InputSystem.AddDevice<Gamepad>();

        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.defaultActionMap = "gameplay";
        playerInput.notificationBehavior = PlayerNotifications.SendMessages;
        playerInput.actions = InputActionAsset.FromJson(kActions);
        var listener = go.AddComponent<MessageListener>();

        Press((ButtonControl)playerInput.actions["Action With Spaces!!"].controls[0]);

        Assert.That(listener.messages, Has.Exactly(1).With.Property("name").EqualTo("OnActionWithSpaces"));
    }

    // Test setup where two players both use the keyboard but with two different control
    // schemes.
    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanSetUpSplitKeyboardPlay()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // We add a gamepad device and scheme just to add noise and make sure
        // this isn't throwing the thing off the rails.
        InputSystem.AddDevice<Gamepad>();

        const string kActions = @"
        {
            ""maps"" : [
                {
                    ""name"" : ""gameplay"",
                    ""actions"" : [
                        { ""name"" : ""fire"", ""type"" : ""button"" }
                    ],
                    ""bindings"" : [
                        { ""path"" : ""<Gamepad>/buttonSouth"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" },
                        { ""path"" : ""<Keyboard>/leftCtrl"", ""action"" : ""fire"", ""groups"" : ""KeyboardWASD"" },
                        { ""path"" : ""<Keyboard>/rightCtrl"", ""action"" : ""fire"", ""groups"" : ""KeyboardArrows"" }
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
                    ""name"" : ""Keyboard WASD"",
                    ""bindingGroup"" : ""KeyboardWASD"",
                    ""devices"" : [
                        { ""devicePath"" : ""<Keyboard>"" }
                    ]
                },
                {
                    ""name"" : ""Keyboard Arrows"",
                    ""bindingGroup"" : ""KeyboardArrows"",
                    ""devices"" : [
                        { ""devicePath"" : ""<Keyboard>"" }
                    ]
                }
            ]
        }";

        var prefab = new GameObject();
        prefab.SetActive(false);
        prefab.AddComponent<MessageListener>();
        prefab.AddComponent<PlayerInput>();
        prefab.GetComponent<PlayerInput>().actions = InputActionAsset.FromJson(kActions);
        prefab.GetComponent<PlayerInput>().defaultActionMap = "gameplay";

        var player1 = PlayerInput.Instantiate(prefab, controlScheme: "Keyboard WASD", pairWithDevice: keyboard);
        var player2 = PlayerInput.Instantiate(prefab, controlScheme: "Keyboard Arrows", pairWithDevice: keyboard);

        Assert.That(player1.devices, Is.EquivalentTo(new[] { keyboard }));
        Assert.That(player2.devices, Is.EquivalentTo(new[] { keyboard }));
        Assert.That(player1.currentControlScheme, Is.EqualTo("Keyboard WASD"));
        Assert.That(player2.currentControlScheme, Is.EqualTo("Keyboard Arrows"));
        Assert.That(player1.actions["fire"].controls, Is.EquivalentTo(new[] { keyboard.leftCtrlKey }));
        Assert.That(player2.actions["fire"].controls, Is.EquivalentTo(new[] { keyboard.rightCtrlKey }));

        Press(keyboard.leftCtrlKey);

        Assert.That(player1.GetComponent<MessageListener>().messages,
            Is.EquivalentTo(new[] {new Message { name = "OnFire", value = 1f }}));
        Assert.That(player2.GetComponent<MessageListener>().messages, Is.Empty);

        Release(keyboard.leftCtrlKey);
        player1.GetComponent<MessageListener>().messages.Clear();

        Press(keyboard.rightCtrlKey);

        Assert.That(player1.GetComponent<MessageListener>().messages, Is.Empty);
        Assert.That(player2.GetComponent<MessageListener>().messages,
            Is.EquivalentTo(new[] {new Message { name = "OnFire", value = 1f }}));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanSetDefaultActionMap()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var go = new GameObject();
        var listener = go.AddComponent<MessageListener>();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.defaultActionMap = "Other";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        Set(gamepad.leftTrigger, 0.234f);

        Assert.That(playerInput.actions.FindActionMap("gameplay").enabled, Is.False);
        Assert.That(playerInput.actions.FindActionMap("other").enabled, Is.True);
        Assert.That(listener.messages, Is.EquivalentTo(new[]
        {
            new Message("OnControlsChanged", playerInput),
            new Message("OnOtherAction", 0.234f)
        }));
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanSwitchActionsWithMessage()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var go = new GameObject();
        var listener = go.AddComponent<MessageListener>();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.defaultActionMap = "gameplay";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        Set(gamepad.leftTrigger, 0.234f);

        Assert.That(playerInput.actions.FindActionMap("gameplay").enabled, Is.True);
        Assert.That(playerInput.actions.FindActionMap("other").enabled, Is.False);
        Assert.That(listener.messages, Is.EquivalentTo(new[]
        {
            new Message("OnControlsChanged", playerInput),
            new Message("OnFire", 0.234f)
        }));

        listener.messages.Clear();

        go.SendMessage("SwitchCurrentActionMap", "other");

        Set(gamepad.leftTrigger, 0.345f);

        Assert.That(playerInput.actions.FindActionMap("gameplay").enabled, Is.False);
        Assert.That(playerInput.actions.FindActionMap("other").enabled, Is.True);
        Assert.That(listener.messages, Is.EquivalentTo(
            new[]
            {
                new Message("OnOtherAction", 0.234f), // otherAction is a value action which implies an initial state check
                new Message("OnOtherAction", 0.345f)
            }));
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
    [TestCase(PlayerNotifications.SendMessages, typeof(MessageListener))]
    [TestCase(PlayerNotifications.BroadcastMessages, typeof(MessageListener))]
    [TestCase(PlayerNotifications.InvokeUnityEvents, typeof(PlayerInputEventListener), true)]
    [TestCase(PlayerNotifications.InvokeCSharpEvents, typeof(PlayerInputCSharpEventListener), true)]
    public void PlayerInput_CanReceiveNotificationWhenActionIsTriggered(PlayerNotifications notificationBehavior, Type listenerType, bool receivesAllPhases = false)
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var go = new GameObject();
        go.SetActive(false);
        IListener listener;
        if (notificationBehavior == PlayerNotifications.BroadcastMessages)
        {
            var child = new GameObject();
            child.transform.parent = go.transform;
            listener = (IListener)child.AddComponent(listenerType);
        }
        else
        {
            listener = (IListener)go.AddComponent(listenerType);
        }
        var playerInput = go.AddComponent<PlayerInput>();

        playerInput.notificationBehavior = notificationBehavior;
        playerInput.defaultActionMap = "gameplay";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        go.SetActive(true);

        Press(gamepad.buttonSouth);

        if (receivesAllPhases)
        {
            Assert.That(listener.messages, Is.EquivalentTo(new[] { new Message("Fire Started", 1f), new Message("Fire Performed", 1f) }));
        }
        else
        {
            Assert.That(listener.messages, Is.EquivalentTo(new[] {new Message("OnFire", 1f)}));
        }

        listener.messages.Clear();

        Release(gamepad.buttonSouth);

        if (receivesAllPhases)
        {
            Assert.That(listener.messages, Is.EquivalentTo(new[] {new Message("Fire Canceled", 0f)}));
        }
        else
        {
            // 'Fire' is a button action. Unlike with value actions, PlayerInput should not
            // send a message on button release (i.e. when the action cancels).
            Assert.That(listener.messages, Is.Empty);
        }
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
        playerInput.defaultActionMap = "gameplay";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        Press(gamepad.buttonSouth);

        Assert.That(listener.messages, Is.EquivalentTo(new[]
        {
            new Message("OnControlsChanged", playerInput),
            new Message("OnFire", 1f)
        }));

        listener.messages.Clear();

        // 'Fire' is a button action. Unlike with value actions, PlayerInput should not
        // send a message on button release (i.e. when the action cancels).
        Release(gamepad.buttonSouth);

        Assert.That(listener.messages, Is.Empty);
    }

    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanReceiveMessageWhenValueActionIsCanceled()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var go = new GameObject();
        var listener = go.AddComponent<MessageListener>();
        var playerInput = go.AddComponent<PlayerInput>();

        playerInput.notificationBehavior = PlayerNotifications.SendMessages;
        playerInput.defaultActionMap = "gameplay";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        Set(gamepad.leftStick, new Vector2(0.123f, 0.234f));

        Assert.That(listener.messages,
            Is.EquivalentTo(new[]
            {
                new Message("OnControlsChanged", playerInput),
                new Message("OnMove", new StickDeadzoneProcessor().Process(new Vector2(0.123f, 0.234f)))
            }));

        listener.messages.Clear();

        Set(gamepad.leftStick, Vector2.zero);

        Assert.That(listener.messages,
            Is.EquivalentTo(new[]
            {
                new Message("OnMove", Vector2.zero)
            }));
    }

    [Test]
    [Category("PlayerInput")]
    [TestCase(PlayerNotifications.SendMessages, typeof(MessageListener))]
    [TestCase(PlayerNotifications.BroadcastMessages, typeof(MessageListener))]
    [TestCase(PlayerNotifications.InvokeUnityEvents, typeof(PlayerInputEventListener))]
    [TestCase(PlayerNotifications.InvokeCSharpEvents, typeof(PlayerInputCSharpEventListener))]
    [Retry(2)] // Warm up JIT.
    public void PlayerInput_TriggeringAction_DoesNotAllocateGCMemory(PlayerNotifications notificationBehavior, Type listenerType)
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var go = new GameObject();
        var playerInput = go.AddComponent<PlayerInput>();
        playerInput.notificationBehavior = notificationBehavior;
        playerInput.defaultActionMap = "gameplay";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        var listener = (IListener)go.AddComponent(listenerType);
        // We don't want the listener to actually record messages. They have an object field which will
        // box values and thus allocate GC garbage. We *do* want the listener to actually read values
        // to make sure that doesn't allocate anything.
        listener.messages = null;

        // First message is allowed to perform initialization work and thus allocate.
        PressAndRelease(gamepad.buttonSouth);

        var kProfilerRegion = "PlayerInput_TriggeringAction_DoesNotAllocateGCMemory";

        Assert.That(() =>
        {
            Profiler.BeginSample(kProfilerRegion);
            PressAndRelease(gamepad.buttonSouth);
            Profiler.EndSample();
        }, Is.Not.AllocatingGCMemory());
    }

    [Test]
    [Category("PlayerInput")]
    [TestCase(PlayerNotifications.SendMessages, typeof(MessageListener))]
    [TestCase(PlayerNotifications.BroadcastMessages, typeof(MessageListener))]
    [TestCase(PlayerNotifications.InvokeUnityEvents, typeof(PlayerInputEventListener))]
    [TestCase(PlayerNotifications.InvokeCSharpEvents, typeof(PlayerInputCSharpEventListener))]
    public void PlayerInput_CanReceiveNotificationWhenDeviceIsLostAndRegained(PlayerNotifications notificationBehavior, Type listenerType)
    {
        // Pretend we have a native gamepad to get the test closer to the real thing.
        var deviceId = runtime.ReportNewInputDevice<Gamepad>();
        InputSystem.Update();
        var gamepad = (Gamepad)InputSystem.GetDeviceById(deviceId);

        var go = new GameObject();
        go.SetActive(false);
        IListener listener;
        if (notificationBehavior == PlayerNotifications.BroadcastMessages)
        {
            var child = new GameObject();
            child.transform.parent = go.transform;
            listener = (IListener)child.AddComponent(listenerType);
        }
        else
        {
            listener = (IListener)go.AddComponent(listenerType);
        }
        var playerInput = go.AddComponent<PlayerInput>();

        playerInput.notificationBehavior = notificationBehavior;
        playerInput.actions = InputActionAsset.FromJson(kActions);

        go.SetActive(true);

        Assert.That(playerInput.devices, Is.EquivalentTo(new[] { gamepad }));

        runtime.ReportInputDeviceRemoved(gamepad);
        InputSystem.Update();

        Assert.That(playerInput.devices, Is.Empty);
        Assert.That(playerInput.hasMissingRequiredDevices, Is.True);
        Assert.That(listener.messages,
            Is.EquivalentTo(new[] {new Message(PlayerInput.DeviceLostMessage, playerInput)}));

        listener.messages.Clear();

        deviceId = runtime.ReportNewInputDevice<Gamepad>();
        InputSystem.Update();
        Assert.That(InputSystem.GetDeviceById(deviceId), Is.SameAs(gamepad),
            "Expected InputSystem to recover previous device instance");
        gamepad = (Gamepad)InputSystem.GetDeviceById(deviceId);

        Assert.That(playerInput.devices, Is.EquivalentTo(new[] {gamepad}));
        Assert.That(playerInput.hasMissingRequiredDevices, Is.False);
        Assert.That(listener.messages,
            Is.EquivalentTo(new[] {new Message(PlayerInput.DeviceRegainedMessage, playerInput)}));
    }

    ////TODO: should also make sure that we're only getting notifications of the specified type and not other types as well
    [Test]
    [Category("PlayerInput")]
    [TestCase(PlayerNotifications.SendMessages, typeof(MessageListener))]
    [TestCase(PlayerNotifications.BroadcastMessages, typeof(MessageListener))]
    [TestCase(PlayerNotifications.InvokeUnityEvents, typeof(PlayerManagerEventListener))]
    [TestCase(PlayerNotifications.InvokeCSharpEvents, typeof(PlayerManagerCSharpEventListener))]
    public void PlayerInput_CanReceiveNotificationWhenPlayerJoinsOrLeaves(PlayerNotifications notificationBehavior, Type listenerType)
    {
        var go1 = new GameObject();
        var playerInput1 = go1.AddComponent<PlayerInput>();

        var manager = new GameObject();
        manager.SetActive(false); // Delay OnEnable() until we have all components.
        IListener listener;
        if (notificationBehavior == PlayerNotifications.BroadcastMessages)
        {
            // Put listener on child object to make sure we get a broadcast.
            var child = new GameObject();
            child.transform.parent = manager.transform;
            listener = (IListener)child.AddComponent(listenerType);
        }
        else
        {
            listener = (IListener)manager.AddComponent(listenerType);
        }
        var managerComponent = manager.AddComponent<PlayerInputManager>();
        managerComponent.notificationBehavior = notificationBehavior;
        manager.SetActive(true);

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

        listener.messages.Clear();

        Object.DestroyImmediate(go1);

        Assert.That(listener.messages, Is.EquivalentTo(new[] { new Message("OnPlayerLeft", playerInput1)}));
    }

    [Test]
    [Category("PlayerInput")]
    [TestCase(PlayerNotifications.SendMessages, typeof(MessageListener))]
    [TestCase(PlayerNotifications.BroadcastMessages, typeof(MessageListener))]
    [TestCase(PlayerNotifications.InvokeUnityEvents, typeof(PlayerInputEventListener))]
    [TestCase(PlayerNotifications.InvokeCSharpEvents, typeof(PlayerInputCSharpEventListener))]
    public void PlayerInput_CanReceiveNotificationWhenControlsAreModified(PlayerNotifications notificationBehavior, Type listenerType)
    {
        InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var go = new GameObject();
        go.SetActive(false);
        IListener listener;
        if (notificationBehavior == PlayerNotifications.BroadcastMessages)
        {
            var child = new GameObject();
            child.transform.parent = go.transform;
            listener = (IListener)child.AddComponent(listenerType);
        }
        else
        {
            listener = (IListener)go.AddComponent(listenerType);
        }
        var playerInput = go.AddComponent<PlayerInput>();

        playerInput.notificationBehavior = notificationBehavior;
        playerInput.defaultControlScheme = "Gamepad";
        playerInput.defaultActionMap = "Gameplay";
        playerInput.actions = InputActionAsset.FromJson(kActions);

        // NOTE: No message when controls are first enabled. This means that, for example, when rebinding happens in a UI
        //       while the component is disabled and we then enable the component, there will *NOT* be an OnControlsChanged call.
        go.SetActive(true);

        // Rebind fire button.
        playerInput.actions["fire"].ApplyBindingOverride("<Gamepad>/leftTrigger", group: "Gamepad");

        Assert.That(listener.messages, Is.EquivalentTo(new[] { new Message("OnControlsChanged", playerInput)}));

        listener.messages.Clear();

        // Switch control scheme.
        playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", keyboard, mouse);

        Assert.That(listener.messages, Is.EquivalentTo(new[]
        {
            new Message("OnControlsChanged", playerInput)
        }));

        listener.messages.Clear();

        // Switch keyboard layout.
        SetKeyboardLayout("Other");

        Assert.That(listener.messages, Is.EquivalentTo(new[] { new Message("OnControlsChanged", playerInput)}));
    }

    [Test]
    [Category("PlayerInput")]
    [TestCase("Gamepad", "buttonSouth", "<Gamepad>/leftStick/x", "buttonNorth")]
    [TestCase("Keyboard", "space", "<Mouse>/position/x", "b", "Mouse")]
    public void PlayerInput_CanJoinPlayersThroughButtonPress(string deviceLayout, string buttonControl, string nonButtonControl, string anotherButtonControl, string secondDeviceLayout = null)
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

        var device = InputSystem.AddDevice(deviceLayout);

        var secondDevice = default(InputDevice);
        if (!string.IsNullOrEmpty(secondDeviceLayout))
            secondDevice = InputSystem.AddDevice(secondDeviceLayout);

        var devices = secondDevice == null ? new[] { device } : new[] { device, secondDevice };

        // First actuate non-button control and make sure it does NOT result in a join.
        Set((InputControl<float>)InputSystem.FindControl(nonButtonControl), 1f);

        Assert.That(PlayerInput.all, Is.Empty);
        Assert.That(listener.messages, Is.Empty);

        // Now press button and make sure it DOES result in a join.
        Press((ButtonControl)device[buttonControl]);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(1));
        Assert.That(PlayerInput.all[0].devices, Is.EquivalentTo(devices));
        Assert.That(PlayerInput.all[0].user.valid, Is.True);
        Assert.That(listener.messages, Is.EquivalentTo(new[] { new Message("OnPlayerJoined", PlayerInput.all[0])}));

        // Press another button and make sure it does NOT result in anything.

        listener.messages.Clear();

        Release((ButtonControl)device[buttonControl]);
        Press((ButtonControl)device[anotherButtonControl]);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(1));
        Assert.That(listener.messages, Is.Empty);
    }

    // https://fogbugz.unity3d.com/f/cases/1226920/
    [Test]
    [Category("PlayerInput")]
    public void PlayerInput_CanJoinPlayersThroughButtonPress_WithMultipleDevicesOfTypePresent()
    {
        var playerPrefab = new GameObject();
        playerPrefab.SetActive(false);
        playerPrefab.AddComponent<PlayerInput>();
        playerPrefab.GetComponent<PlayerInput>().actions = InputActionAsset.FromJson(kActions);

        var manager = new GameObject();
        var managerComponent = manager.AddComponent<PlayerInputManager>();
        managerComponent.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
        managerComponent.playerPrefab = playerPrefab;

        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        var gamepad3 = InputSystem.AddDevice<Gamepad>();

        InputSystem.AddDevice<Keyboard>(); // Noise.
        InputSystem.AddDevice<Mouse>(); // Noise.

        Press(gamepad2.buttonSouth);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(1));
        Assert.That(PlayerInput.all[0].devices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(PlayerInput.all[0].currentControlScheme, Is.EqualTo("Gamepad"));

        Press(gamepad1.buttonSouth);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(2));
        Assert.That(PlayerInput.all[0].devices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(PlayerInput.all[0].currentControlScheme, Is.EqualTo("Gamepad"));
        Assert.That(PlayerInput.all[1].devices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(PlayerInput.all[1].currentControlScheme, Is.EqualTo("Gamepad"));

        Press(gamepad3.buttonSouth);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(3));
        Assert.That(PlayerInput.all[0].devices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(PlayerInput.all[0].currentControlScheme, Is.EqualTo("Gamepad"));
        Assert.That(PlayerInput.all[1].devices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(PlayerInput.all[1].currentControlScheme, Is.EqualTo("Gamepad"));
        Assert.That(PlayerInput.all[2].devices, Is.EquivalentTo(new[] { gamepad3 }));
        Assert.That(PlayerInput.all[2].currentControlScheme, Is.EqualTo("Gamepad"));
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

        // Also try with just a keyboard. Without having a mouse present, this should
        // fail the join.
        var keyboard = InputSystem.AddDevice<Keyboard>();
        Press(keyboard.spaceKey);

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
        managerComponent.joinAction = new InputActionProperty(joinAction);
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

        Assert.That(InputUser.GetUnpairedInputDevices().ToArray(dispose: true),
            Is.EquivalentTo(new[] { gamepad1, gamepad2, gamepad3, gamepad4 }));

        // Join two players and make sure we get two screen side-by-side.
        Press(gamepad1.buttonSouth);
        Press(gamepad2.buttonSouth);

        Assert.That(PlayerInput.all, Has.Count.EqualTo(2));

        Assert.That(PlayerInput.all[0].devices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(PlayerInput.all[1].devices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(InputUser.GetUnpairedInputDevices().ToArray(dispose: true),
            Is.EquivalentTo(new[] { gamepad3, gamepad4 }));

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

        Assert.That(PlayerInput.all[0].devices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(PlayerInput.all[1].devices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(PlayerInput.all[2].devices, Is.EquivalentTo(new[] { gamepad3 }));
        Assert.That(InputUser.GetUnpairedInputDevices().ToArray(dispose: true),
            Is.EquivalentTo(new[] { gamepad4 }));

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

        Assert.That(PlayerInput.all[0].devices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(PlayerInput.all[1].devices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(PlayerInput.all[2].devices, Is.EquivalentTo(new[] { gamepad3 }));
        Assert.That(PlayerInput.all[3].devices, Is.EquivalentTo(new[] { gamepad4 }));
        Assert.That(InputUser.GetUnpairedInputDevices().ToArray(dispose: true), Is.Empty);

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

        Assert.That(PlayerInput.all[0].devices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(PlayerInput.all[1].devices, Is.EquivalentTo(new[] { gamepad3 }));
        Assert.That(PlayerInput.all[2].devices, Is.EquivalentTo(new[] { gamepad4 }));
        Assert.That(InputUser.GetUnpairedInputDevices().ToArray(dispose: true),
            Is.EquivalentTo(new[] { gamepad2 }));

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

        // PlayerInput.all is sorted by playerIndex so the player we just joined popped up in slot #1.
        Assert.That(PlayerInput.all[0].devices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(PlayerInput.all[1].devices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(PlayerInput.all[2].devices, Is.EquivalentTo(new[] { gamepad3 }));
        Assert.That(PlayerInput.all[3].devices, Is.EquivalentTo(new[] { gamepad4 }));
        Assert.That(InputUser.GetUnpairedInputDevices().ToArray(dispose: true), Is.Empty);

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

        Assert.That(PlayerInput.all[0].devices, Is.EquivalentTo(new[] { gamepad1 }));
        Assert.That(PlayerInput.all[1].devices, Is.EquivalentTo(new[] { gamepad2 }));
        Assert.That(PlayerInput.all[2].devices, Is.EquivalentTo(new[] { gamepad3 }));
        Assert.That(PlayerInput.all[3].devices, Is.EquivalentTo(new[] { gamepad4 }));
        Assert.That(PlayerInput.all[4].devices, Is.EquivalentTo(new[] { gamepad5 }));
        Assert.That(InputUser.GetUnpairedInputDevices().ToArray(dispose: true), Is.Empty);

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

    // An action is either
    //   (a) button-like, or
    //   (b) axis-like, or
    //   (c) undefined in behavior.
    //
    // (c) is used for actions that are usually chained to lots of inputs (e.g. "bind to all keys on keyboard")
    // where the action thus becomes a simple input collector.
    //
    // (a) and (b) are what is "normal" usage of actions. This is the type of stuff that game actions are made
    // of.
    //
    // (a) acts as a trigger whereas (b) acts
    //
    //
    // isn't (b) and (c) kinda the same in practice??

    private const string kActions = @"
        {
            ""maps"" : [
                {
                    ""name"" : ""gameplay"",
                    ""actions"" : [
                        { ""name"" : ""Fire"", ""type"" : ""button"" },
                        { ""name"" : ""Look"", ""type"" : ""value"" },
                        { ""name"" : ""Move"", ""type"" : ""value"" },
                        { ""name"" : ""Action With Spaces!!"", ""type"" : ""value"" }
                    ],
                    ""bindings"" : [
                        { ""path"" : ""<Gamepad>/buttonSouth"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" },
                        { ""path"" : ""<Gamepad>/leftTrigger"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" },
                        { ""path"" : ""<Gamepad>/leftStick"", ""action"" : ""move"", ""groups"" : ""Gamepad"" },
                        { ""path"" : ""<Gamepad>/rightStick"", ""action"" : ""look"", ""groups"" : ""Gamepad"" },
                        { ""path"" : ""dpad"", ""action"" : ""move"", ""groups"" : ""Gamepad"", ""isComposite"" : true },
                        { ""path"" : ""<Keyboard>/a"", ""name"" : ""left"", ""action"" : ""move"", ""groups"" : ""Keyboard&Mouse"", ""isPartOfComposite"" : true },
                        { ""path"" : ""<Keyboard>/d"", ""name"" : ""right"", ""action"" : ""move"", ""groups"" : ""Keyboard&Mouse"", ""isPartOfComposite"" : true },
                        { ""path"" : ""<Keyboard>/w"", ""name"" : ""up"", ""action"" : ""move"", ""groups"" : ""Keyboard&Mouse"", ""isPartOfComposite"" : true },
                        { ""path"" : ""<Keyboard>/s"", ""name"" : ""down"", ""action"" : ""move"", ""groups"" : ""Keyboard&Mouse"", ""isPartOfComposite"" : true },
                        { ""path"" : ""<Mouse>/delta"", ""action"" : ""look"", ""groups"" : ""Keyboard&Mouse"" },
                        { ""path"" : ""<Mouse>/leftButton"", ""action"" : ""fire"", ""groups"" : ""Keyboard&Mouse"" },
                        { ""path"" : ""<Gamepad>/buttonNorth"", ""action"" : ""Action With Spaces!!"", ""groups"" : ""Gamepad"" }
                    ]
                },
                {
                    ""name"" : ""ui"",
                    ""actions"" : [
                        { ""name"" : ""navigate"", ""type"" : ""PassThrough"" },
                        { ""name"" : ""point"", ""type"" : ""PassThrough"" },
                        { ""name"" : ""click"", ""type"" : ""PassThrough"" }
                    ],
                    ""bindings"" : [
                        { ""path"" : ""<Gamepad>/dpad"", ""action"" : ""navigate"", ""groups"" : ""Gamepad"" },
                        { ""path"" : ""<Mouse>/position"", ""action"" : ""point"", ""groups"" : ""Keyboard&Mouse"" },
                        { ""path"" : ""<Mouse>/leftButton"", ""action"" : ""click"", ""groups"" : ""Keyboard&Mouse"" }
                    ]
                },
                {
                    ""name"" : ""other"",
                    ""actions"" : [
                        { ""name"" : ""otherAction"" }
                    ],
                    ""bindings"" : [
                        { ""path"" : ""<Gamepad>/leftTrigger"", ""action"" : ""otherAction"", ""groups"" : ""Gamepad"" }
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
        public string name { get; set; }
        public object value { get; set; }

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

    private interface IListener
    {
        List<Message> messages { get; set; }
    }

    private class MessageListener : MonoBehaviour, IListener
    {
        public List<Message> messages { get; set; } = new List<Message>();

        // ReSharper disable once UnusedMember.Local
        public void OnFire(InputValue value)
        {
            messages?.Add(new Message { name = "OnFire", value = value.Get<float>() });
        }

        // ReSharper disable once UnusedMember.Local
        public void OnLook(InputValue value)
        {
            messages?.Add(new Message { name = "OnLook", value = value.Get<Vector2>() });
        }

        // ReSharper disable once UnusedMember.Local
        public void OnMove(InputValue value)
        {
            messages?.Add(new Message { name = "OnMove", value = value.Get<Vector2>() });
        }

        // ReSharper disable once UnusedMember.Local
        public void OnOtherAction(InputValue value)
        {
            messages?.Add(new Message { name = "OnOtherAction", value = value.Get<float>() });
        }

        // ReSharper disable once UnusedMember.Local
        public void OnActionWithSpaces(InputValue value)
        {
            messages?.Add(new Message { name = "OnActionWithSpaces", value = value.Get<float>() });
        }

        // ReSharper disable once UnusedMember.Local
        public void OnDeviceLost(PlayerInput player)
        {
            messages?.Add(new Message { name = "OnDeviceLost", value = player});
        }

        // ReSharper disable once UnusedMember.Local
        public void OnDeviceRegained(PlayerInput player)
        {
            messages?.Add(new Message { name = "OnDeviceRegained", value = player});
        }

        // ReSharper disable once UnusedMember.Local
        public void OnControlsChanged(PlayerInput player)
        {
            messages?.Add(new Message { name = "OnControlsChanged", value = player});
        }

        // ReSharper disable once UnusedMember.Local
        public void OnPlayerJoined(PlayerInput player)
        {
            messages?.Add(new Message { name = "OnPlayerJoined", value = player});
        }

        // ReSharper disable once UnusedMember.Local
        public void OnPlayerLeft(PlayerInput player)
        {
            messages?.Add(new Message { name = "OnPlayerLeft", value = player});
        }
    }


    // il2cpp internally crashes if this gets stripped. Need to investigate, but for now we preserve it.
    [Preserve]
    private class PlayerInputEventListener : MonoBehaviour, IListener
    {
        public List<Message> messages { get; set; } = new List<Message>();

        public void OnEnable()
        {
            var playerInput = GetComponent<PlayerInput>();
            Debug.Assert(playerInput != null, "Must have PlayerInput component");

            SetUpActionEvents(playerInput);

            playerInput.deviceLostEvent.AddListener(OnDeviceLost);
            playerInput.deviceRegainedEvent.AddListener(OnDeviceRegained);
            playerInput.controlsChangedEvent.AddListener(OnControlsChanged);
        }

        private void SetUpActionEvents(PlayerInput player)
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

        // We have separate methods for these rather than one that we reuse for each listener in order to
        // guarantee that PlayerInput is indeed calling the right method.

        private void OnFireEvent(InputAction.CallbackContext context)
        {
            messages?.Add(new Message($"Fire {context.phase}", context.ReadValueAsObject()));
        }

        private void OnLookEvent(InputAction.CallbackContext context)
        {
            messages?.Add(new Message($"Look {context.phase}", context.ReadValueAsObject()));
        }

        private void OnMoveEvent(InputAction.CallbackContext context)
        {
            messages?.Add(new Message($"Move {context.phase}", context.ReadValueAsObject()));
        }

        private void OnDeviceLost(PlayerInput player)
        {
            messages?.Add(new Message("OnDeviceLost", player));
        }

        private void OnDeviceRegained(PlayerInput player)
        {
            messages?.Add(new Message("OnDeviceRegained", player));
        }

        private void OnControlsChanged(PlayerInput player)
        {
            messages?.Add(new Message("OnControlsChanged", player));
        }
    }

    // il2cpp internally crashes if this gets stripped. Need to investigate, but for now we preserve it.
    [Preserve]
    private class PlayerInputCSharpEventListener : MonoBehaviour, IListener
    {
        public List<Message> messages { get; set; } = new List<Message>();

        public void OnEnable()
        {
            var playerInput = GetComponent<PlayerInput>();
            Debug.Assert(playerInput != null, "Must have PlayerInput component");

            playerInput.onActionTriggered += OnAction;
            playerInput.onDeviceLost += OnDeviceLost;
            playerInput.onDeviceRegained += OnDeviceRegained;
            playerInput.onControlsChanged += OnControlsChanged;
        }

        private void OnAction(InputAction.CallbackContext context)
        {
            messages?.Add(new Message($"{context.action.name} {context.phase}", context.ReadValueAsObject()));
        }

        private void OnDeviceLost(PlayerInput player)
        {
            messages?.Add(new Message("OnDeviceLost", player));
        }

        private void OnDeviceRegained(PlayerInput player)
        {
            messages?.Add(new Message("OnDeviceRegained", player));
        }

        private void OnControlsChanged(PlayerInput player)
        {
            messages?.Add(new Message("OnControlsChanged", player));
        }
    }

    // il2cpp internally crashes if this gets stripped. Need to investigate, but for now we preserve it.
    [Preserve]
    private class PlayerManagerEventListener : MonoBehaviour, IListener
    {
        public List<Message> messages { get; set; } = new List<Message>();

        public void OnEnable()
        {
            var manager = GetComponent<PlayerInputManager>();
            Debug.Assert(manager != null, "Must have PlayerInputManager component");

            manager.playerJoinedEvent.AddListener(OnPlayerJoined);
            manager.playerLeftEvent.AddListener(OnPlayerLeft);
        }

        private void OnPlayerJoined(PlayerInput player)
        {
            messages.Add(new Message("OnPlayerJoined", player));
        }

        private void OnPlayerLeft(PlayerInput player)
        {
            messages.Add(new Message("OnPlayerLeft", player));
        }
    }

    // il2cpp internally crashes if this gets stripped. Need to investigate, but for now we preserve it.
    [Preserve]
    private class PlayerManagerCSharpEventListener : MonoBehaviour, IListener
    {
        public List<Message> messages { get; set; } = new List<Message>();

        public void OnEnable()
        {
            var manager = GetComponent<PlayerInputManager>();
            Debug.Assert(manager != null, "Must have PlayerInputManager component");

            manager.onPlayerJoined += OnPlayerJoined;
            manager.onPlayerLeft += OnPlayerLeft;
        }

        private void OnPlayerJoined(PlayerInput player)
        {
            messages?.Add(new Message("OnPlayerJoined", player));
        }

        private void OnPlayerLeft(PlayerInput player)
        {
            messages?.Add(new Message("OnPlayerLeft", player));
        }
    }
}
