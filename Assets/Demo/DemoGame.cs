using System;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.Users;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.XR;
using InputDevice = UnityEngine.Experimental.Input.InputDevice;

#if UNITY_EDITOR
using UnityEditor;
#endif

////WIP

/// <summary>
/// Main controller for the demo game.
/// </summary>
/// <remarks>
/// Uses <see cref="playerPrefab"/> to spawn one or more players into the scene and sets
/// up screen areas for them. When last player exits the game, cleans up and returns
/// to main menu.
///
/// While playing, players can leave and join while slots are available. Game changes
/// split-screen configuration dynamically based on the number of players that are
/// available.
///
/// Each player has its own menu which allows customizing controls or exiting the game.
/// </remarks>
public class DemoGame : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject fishPrefab; // rename 'fish' to 'creature'

    public Canvas mainMenuCanvas;
    public Camera mainMenuCamera;

    public enum State
    {
        InMainMenu,
        InGame,
        InGameOver,
        WaitingForFirstPlayerToJoin,
    }

    public State state
    {
        get { return m_State; }
    }

    /// <summary>
    /// Whether we're currently in a single-player game.
    /// </summary>
    public bool isSinglePlayer
    {
        get { return m_SinglePlayer; }
    }

    /// <summary>
    /// Whether we're currently in a multi-player game.
    /// </summary>
    public bool isMultiPlayer
    {
        get { return !m_SinglePlayer; }
    }

    /// <summary>
    /// List of players currently in the game.
    /// </summary>
    public ReadOnlyArray<DemoPlayerController> players
    {
        get { return new ReadOnlyArray<DemoPlayerController>(m_Players, 0, m_ActivePlayerCount); }
    }

    public DemoFishController fish
    {
        get { return m_Fish; }
    }

    public static RuntimePlatform platform
    {
        get
        {
            if (!s_Platform.HasValue)
                return Application.platform;
            return s_Platform.Value;
        }
        set { s_Platform = value; }
    }

    public static bool vrSupported
    {
        get
        {
            if (!s_VRSupported.HasValue)
                return XRSettings.enabled;
            return s_VRSupported.Value;
        }
        set { s_VRSupported = value; }
    }

    private static RuntimePlatform? s_Platform;
    private static bool? s_VRSupported;

    private bool m_SinglePlayer;
    private int m_ActivePlayerCount;
    private DemoPlayerController[] m_Players;
    private DemoFishController m_Fish;
    private State m_State;

    public void Awake()
    {
        InputUser.onChange += OnUserChange;

        // In single player games we want to know when the player switches to a device
        // that isn't among the ones currently assigned to the player so that we can
        // detect when to switch to a different control scheme.
        InputUser.onUnassignedDeviceUsed += OnUnassignedInputDeviceUsed;
    }

    public void OnDestroy()
    {
        InputUser.onChange -= OnUserChange;
        InputUser.onUnassignedDeviceUsed -= OnUnassignedInputDeviceUsed;
    }

    /// <summary>
    /// Start the game.
    /// </summary>
    /// <remarks>
    /// Shows main menu which allows choosing between single and multi-player.
    /// </remarks>
    public void Start()
    {
        // Start out with main menu active. No unskippable 5 minute sequence of
        // company logos.
        ShowMainMenu();
    }

    /// <summary>
    /// Leave the game.
    /// </summary>
    public void Quit()
    {
        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Start a game with a single player.
    /// </summary>
    /// <remarks>
    /// In this mode, all local devices are "owned" by the player and the player can switch between them freely.
    /// </remarks>
    public void StartSinglePlayerGame()
    {
        m_SinglePlayer = true;

        // Spawn a player at index #0.
        Debug.Assert(m_ActivePlayerCount == 0);
        var player = SpawnPlayer();

        // Let player initialize controls for single-player.
        player.StartSinglePlayerGame();

        // Run code that is shared between single- and multi-player games.
        StartGame();
    }

    /// <summary>
    /// Start a game with multiple players.
    /// </summary>
    /// <remarks>
    /// In this mode, players join explicitly on specific devices which are then assigned to them. The screen
    /// is subdivided as players join and unsubdivided as players leave.
    ///
    /// At the beginning of the game,
    /// </remarks>
    public void StartMultiPlayerGame()
    {
        m_SinglePlayer = false;

        // Listen for joins.
        InputSystem.onEvent += OnInputEventInMultiPlayer;

        ////TODO: call OnJoin when performed
        ////TODO: react when bound controls change

        StartGame();
    }

    private void StartGame()
    {
        // Create fish, if need be.
        if (m_Fish == null)
        {
            var fishObject = Instantiate(fishPrefab);
            m_Fish = fishObject.GetComponent<DemoFishController>();
            if (m_Fish == null)
                throw new Exception("Cannot find 'DemoFishController' on " + fishObject);
        }

        // Wipe state from last game.
        m_Fish.Reset();

        // Assign screen areas to players.
        UpdateSplitScreen();

        //start timer

        m_State = State.InGame;
    }

    private void EndGame()
    {
        m_ActivePlayerCount = 0;

        if (isMultiPlayer)
        {
            InputSystem.onEvent -= OnInputEventInMultiPlayer;
        }
    }

    ////REVIEW: this logic seems too low-level to be here; can we move this into the input system somehow?
    /// <summary>
    /// In multi-player, we want players to be able to join on new devices simply by pressing
    /// a button. This callback is invoked on every input event and we determine whether we have
    /// a new join.
    /// </summary>
    /// <param name="eventPtr">An input event.</param>
    /// <remarks>
    /// </remarks>
    private void OnInputEventInMultiPlayer(InputEventPtr eventPtr)
    {
        // Ignore if not a state event.
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        // Ignore if device is already assigned to a player.
        var device = InputSystem.GetDeviceById(eventPtr.deviceId);
        if (device == null)
            return;
        if (InputUser.FindUserForDevice(device) != null)
            return;

        ////REVIEW: what about devices that we can't actually bind to from any of our existing bindings?
        ////        seems like we should detect that here and not initiate a join
        ////        we could alternatively create an InputAction and put all the bindings we have on there and then do joins from that

        // See if a button was pressed on the device.
        var controls = device.allControls;
        for (var i = 0; i < controls.Count; ++i)
        {
            var control = controls[i];

            // Skip if not a button.
            var button = control as ButtonControl;
            if (button == null)
                continue;

            // If it changed from pressed to not pressed, we have a winner.
            float valueInEvent;
            if (button.ReadValue() < InputConfiguration.ButtonPressPoint &&
                button.ReadValueFrom(eventPtr, out valueInEvent) &&
                valueInEvent >= InputConfiguration.ButtonPressPoint)
            {
                OnJoin(device);
                break;
            }
        }
    }

    private void OnJoin(InputDevice device)
    {
        // Spawn player.
        var player = SpawnPlayer();

        // Give the player the device that the join was initiated from and then
        // let the player component do the initialization work from there.
        player.AssignInputDevice(device);
        player.StartMultiPlayerGame();
    }

    /// <summary>
    /// Called when an action is triggered from an input device that isn't assigned to any user.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="action"></param>
    /// <param name="control"></param>
    /// <remarks>
    /// In single-player mode, we concurrently enable all our bindings from all available control schemes.
    /// This means that the actions will bind to whatever devices are available. However, we only assign
    /// the devices actively used with the current control scheme to the user. This means that we always know
    /// how the player is currently controlling the game.
    ///
    /// When the player uses a binding that isn't part of the current control scheme, this method will
    /// be called. In here we automatically switch control schemes by looking at which control scheme is
    /// suited to the unassigned device.
    ///
    /// Note that the logic here also covers the case where there are multiple devices meant to be used
    /// with the same control scheme. For example, there may be two gamepads and the player is free to
    /// switch from one or the other. In that case, while we will stay on the Gamepad control scheme,
    /// we will still unassign the previously used gamepad from the player and assign the newly used one.
    /// </remarks>
    private void OnUnassignedInputDeviceUsed(IInputUser user, InputAction action, InputControl control)
    {
        // We only support control scheme switching in single player.
        if (!m_SinglePlayer)
            return;

        // All our IInputUsers are expected to be DemoPlayerControllers.
        var player = user as DemoPlayerController;
        if (player == null)
            return;

        ////REVIEW: should we just look at the binding that triggered and go by the binding group it is in?

        // Select a control scheme based on the device that was used.
        var device = control.device;
        var controlScheme = player.SelectControlSchemeBasedOnDevice(device);

        // Give the device to the user and then switch control schemes.
        // If the control scheme requires additional devices, we select them automatically using
        // AndAssignMissingDevices().
        player.ClearAssignedInputDevices();
        player.AssignInputDevice(device);
        player.AssignControlScheme(controlScheme)
            .AndAssignMissingDevices();
    }

    /// <summary>
    /// Called when there's a change in the input user setup in the system.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="change"></param>
    private void OnUserChange(IInputUser user, InputUserChange change)
    {
        var player = user as DemoPlayerController;
        if (player == null)
            return;

        if (change == InputUserChange.DevicesChanged)
            player.OnAssignedDevicesChanged();
    }

    /// <summary>
    /// Called when a player selects the "Exit" menu item in the player's own menu.
    /// </summary>
    /// <param name="player">Player that chose to leave the game.</param>
    /// <see cref="DemoPlayerController.onLeaveGame"/>
    private void OnPlayerLeavesGame(DemoPlayerController player)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Create a new player GameObject.
    /// </summary>
    /// <returns></returns>
    private DemoPlayerController SpawnPlayer()
    {
        // If we still have inactive player objects, use those and bring an inactive
        // player back to life.
        DemoPlayerController player = null;
        if (m_Players != null && m_ActivePlayerCount < m_Players.Length && m_Players[m_ActivePlayerCount] != null)
        {
            // Reuse a player we've previously created. Just reactivate it and wipe its state.
            player = m_Players[m_ActivePlayerCount];
            player.gameObject.SetActive(true);
            player.Reset();
        }
        else
        {
            // Otherwise create a new player.
            var playerObject = Instantiate(playerPrefab);
            player = playerObject.GetComponent<DemoPlayerController>();
            if (player == null)
                throw new Exception("Missing DemoPlayerController component on " + playerObject);
            player.PerformOneTimeInitialization(m_ActivePlayerCount == 0);
            player.onLeaveGame = OnPlayerLeavesGame;

            // Add to list.
            if (m_Players == null || m_Players.Length == m_ActivePlayerCount)
                Array.Resize(ref m_Players, m_ActivePlayerCount + 10);
            m_Players[m_ActivePlayerCount] = player;
        }

        // Register as input user with input system.
        InputUser.Add(player);

        ++m_ActivePlayerCount;
        return player;
    }

    private void UnspawnPlayer(int playerIndex)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Assign screen areas to each player based on the number of players
    /// that have joined.
    /// </summary>
    /// <remarks>
    /// Also displays the join UI on any split screens that are not currently used by any player.
    /// </remarks>
    private void UpdateSplitScreen()
    {
    }

    private void ShowMainMenu(bool value = true)
    {
        mainMenuCamera.gameObject.SetActive(value);
        mainMenuCanvas.gameObject.SetActive(value);
    }

    private void ShowJoinUI(int splitScreenIndex)
    {
    }
}
