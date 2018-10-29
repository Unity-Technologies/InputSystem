using System;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Users;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.XR;
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

    //whenever the controls on this action change, we need to update the join help text
    public InputActionProperty joinAction;

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

    public ReadOnlyArray<DemoPlayerController> players
    {
        get { return new ReadOnlyArray<DemoPlayerController>(m_Players); }
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

    //single player: all devices owned by player (but not assigned to), automatically switches schemes as player uses different devices
    //multi player: devices assigned by players explicitly joining on them


    //instead of cubes, create some funky shapes in zbrush
    //have variety of projectiles and tint each one randomly
    //ground animated by wave shader


    //funky animal appears in random locations on the map, lingers a while and then disappears again
    //players have to shoot food into animal's mouth to get points
    //timeout on game, highest score after time is out wins
    //highscore where winning player can enter name (covers text input + IME)


    //what to show:
    // - join and device assignment logic
    // - auto-switching logic for single player
    // - cross-device input where input response code isn't aware of type of device generating the input

    private bool m_SinglePlayer;
    private int m_ActivePlayerCount;
    private DemoPlayerController[] m_Players;
    private DemoFishController m_Fish;
    private State m_State;

    public void Awake()
    {
        // In single player games we want to know when the player switches to a device
        // that isn't among the ones currently assigned to the player so that we can
        // detect when to switch to a different control scheme.
        InputUser.onUnassignedDeviceUsed += OnUnassignedDeviceUsed;
    }

    public void OnDestroy()
    {
        InputUser.onUnassignedDeviceUsed -= OnUnassignedDeviceUsed;
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
        var player = SpawnPlayer(0);

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

        //nuke joinAction and rather listen to event stream; any button press anywhere on a device that isn't
        //assigned yet should be a join
        // Start listening for joins.
        joinAction.action.Enable();
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

    /// <summary>
    /// Called when a player triggers the join action on a device.
    /// </summary>
    /// <param name="context"></param>
    /// <remarks>
    /// </remarks>
    private void OnJoin(InputAction.CallbackContext context)
    {
        // Find first unused player index.
        var playerIndex = 0;
        if (m_Players != null)
        {
            for (var i = 0; i < m_Players.Length; ++i)
                if (m_Players[i] == null || !m_Players[i].enabled)
                {
                    playerIndex = i;
                    break;
                }
        }

        // Spawn player.
        var player = SpawnPlayer(playerIndex);

        // Grab device that triggered the join action.
        var device = context.control.device;

        // Find control scheme involving the device.
        // NOTE: This logic depends on being able to find device combinations automatically for control
        //       schemes involving more than one device. In scenarios where players are free to choose
        //       combinations of devices explicitly, this would have to be handled with a more complicated
        //       device selection procedure (by, for example, having the player go through an additional
        //       step of pressing buttons on additional devices).
        var controlScheme = player.SelectControlSchemeBasedOnDevice(device);

        // If the control scheme involves additional devices, find unused devices.
        if (controlScheme.deviceRequirements.Count > 1)
        {
            throw new NotImplementedException();
        }
        else
        {
            // Single device only. Just assign to player.
            player.AssignInputDevice(device);
        }

        // Enable just the bindings that are part of the control scheme.
        // NOTE: This also means that the player's `controlScheme` will not change automatically
        //       as no bindings are active outside the given control scheme.
        //player.controls.Enable(controlScheme);

        // Enable control scheme on player.
        player.AssignControlScheme(controlScheme);
    }

    /// <summary>
    /// Called when an action is triggered from a device that isn't assigned to any user.
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
    private void OnUnassignedDeviceUsed(IInputUser user, InputAction action, InputControl control)
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
        user.ClearAssignedInputDevices();
        user.AssignInputDevice(device);
        user.AssignControlScheme(controlScheme)
            .AndAssignMissingDevices();
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
    /// <param name="playerIndex"></param>
    /// <returns></returns>
    private DemoPlayerController SpawnPlayer(int playerIndex)
    {
        Debug.Assert(playerIndex >= 0);

        // Create player, if need be.
        DemoPlayerController playerComponent;
        if (m_Players != null && playerIndex <= m_Players.Length && m_Players[playerIndex] != null)
        {
            // Reuse a player we've previously created. Just reactivate it and wipe its state.
            playerComponent = m_Players[playerIndex];
            playerComponent.gameObject.SetActive(true);
            playerComponent.Reset();
        }
        else
        {
            // Create a new player object.
            var playerObject = Instantiate(playerPrefab);
            playerComponent = playerObject.GetComponent<DemoPlayerController>();
            if (playerComponent == null)
                throw new Exception("Missing DemoPlayerController component on " + playerObject);
            playerComponent.PerformOneTimeInitialization(playerIndex);
            playerComponent.onLeaveGame = OnPlayerLeavesGame;

            // Add to list.
            Array.Resize(ref m_Players, playerIndex + 1);
            m_Players[playerIndex] = playerComponent;
        }

        // Register as input user with input system.
        InputUser.Add(playerComponent);

        return playerComponent;
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
