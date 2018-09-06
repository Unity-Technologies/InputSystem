using System;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Users;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public GameObject fishPrefab;
    public InputActionReference joinAction;

    public Canvas mainMenuCanvas;
    public Camera mainMenuCamera;


    //single player: all devices owned by player (but not assigned to), automatically switches schemes as player uses different devices
    //multi player: devices assigned by players explicitly joining on them


    //instead of cubes, create some funky shapes in zbrush
    //have variety of projectiles and tint each one randomly
    //ground animated by wave shader


    //funky animal appears in random locations on the map, lingers a while and then disappears again
    //players have to shoot food into animal's mouth to get points
    //timeout on game, highest score after time is out wins


    //what to show:
    // - join and device assignment logic
    // - auto-switching logic for single player
    // - cross-device input where input response code isn't aware of type of device generating the input


    private int m_ActivePlayerCount;
    private DemoPlayerController[] m_Players;
    private DemoFishController m_Fish;

    /// <summary>
    /// Start the game.
    /// </summary>
    /// <remarks>
    /// Shows main menu which allows choosing between single and multi-player.
    /// </remarks>
    public void Start()
    {
        // We want to use the user management feature of the input system which
        // is not initialized by default. Tell the system we want it.
        InputUserSupport.Initialize();

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
        // Spawn a player with the default input user.
        var player = SpawnPlayer(user: InputUser.first);

        //enable bindings to devices from all control schemes
        //give player the devices from the first scheme that has any devices available
    }

    /// <summary>
    /// Start a game with multiple players.
    /// </summary>
    /// <remarks>
    /// In this mode, players join explicitly on specific devices which are then assigned to them. The screen
    /// is subdivided as players join and unsubdivided as players leave.
    /// </remarks>
    public void StartMultiPlayerGame()
    {
        //listen for join action on any applicable device
        //search for control scheme applicable for the device
        //fulfill any addition additional device needs for the scheme
        //spawn player who joined on the device and give him the device(s)
    }

    private void StartGame()
    {
    }

    /// <summary>
    /// Create a new player GameObject.
    /// </summary>
    /// <param name="playerIndex"></param>
    /// <param name="user"></param>
    /// <returns></returns>
    private DemoPlayerController SpawnPlayer(int playerIndex = 0, InputUser user = null)
    {
        Debug.Assert(playerIndex >= 0);

        // If we don't have an associated input user, create one.
        if (user == null)
            user = InputUser.Add();

        // Create player, if need be.
        DemoPlayerController playerComponent;
        if (m_Players != null && playerIndex <= m_Players.Length)
        {
            playerComponent = m_Players[playerIndex];
            playerComponent.gameObject.SetActive(true);
        }
        else
        {
            var playerObject = Instantiate(playerPrefab);
            playerComponent = playerObject.GetComponent<DemoPlayerController>();
            if (playerComponent == null)
                throw new Exception("Missing DemoPlayerController component on " + playerObject);
        }
        playerComponent.Initialize(user);

        // Add to list.
        ++m_ActivePlayerCount;
        if (m_Players == null || m_Players.Length < m_ActivePlayerCount)
            Array.Resize(ref m_Players, m_ActivePlayerCount);
        else
            Debug.Assert(m_Players[playerIndex] == null);
        m_Players[playerIndex] = playerComponent;

        return playerComponent;
    }

    private void ShowMainMenu(bool value = true)
    {
        mainMenuCamera.enabled = value;
        mainMenuCanvas.enabled = value;
    }
}
