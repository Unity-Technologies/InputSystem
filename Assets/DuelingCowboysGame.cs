using UnityEngine;

public class DuelingCowboysGame : MonoBehaviour
{
    public GameState state;

    private float startTime;
    private int countDownElapsedSeconds;
    private bool player1Fired;
    private bool player2Fired;

    void OnEnable()
    {
        state = GameState.Initializing;
    }
    void Update()
    {
        var elapsed = Time.realtimeSinceStartup - startTime;
        
        switch (state)
        {
            case GameState.Initializing:
                state = GameState.StartCountDown;
                break;
            case GameState.StartCountDown:
                state = GameState.CountDown;
                startTime = Time.realtimeSinceStartup;
                countDownElapsedSeconds = 0;
                player1Fired = false;
                player2Fired = false;
                break;
            case GameState.CountDown:
                var elapsedSeconds = (int)Mathf.Floor(elapsed);
                if (elapsedSeconds == countDownElapsedSeconds)
                    break;
                countDownElapsedSeconds = elapsedSeconds;
                if (countDownElapsedSeconds < 4)
                    Debug.Log($"Countdown: {countDownElapsedSeconds}");
                else
                {
                    state = GameState.Duel;
                    Debug.Log("FIGHT!");
                }
                break;
            case GameState.StartDuel:
                Debug.Log("Duel");
                break;
            case GameState.Duel:
                break;
            case GameState.Player1Wins:
                Debug.Log("Player 1 wins");
                break;
            case GameState.Player2Wins:
                Debug.Log("Player 2 wins");
                break;
            default:
                break;
        }
    }
    
    public void FirePlayer1()
    {
        if (state != GameState.Duel)
            return;

        player1Fired = true;
        Debug.Log("Player 1 Fired");
    }

    public void FirePlayer2()
    {
        if (state != GameState.Duel)
            return;

        player2Fired = true;
        Debug.Log("Player 2 Fired");
    }
}