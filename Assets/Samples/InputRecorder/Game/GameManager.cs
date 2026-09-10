using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Audio & Notes")]
    public AudioSource musicSource;
    public NoteScroller noteScroller;
    public static GameManager instance;

    [Header("Input System")]
    public InputActionReference startAction;

    [HideInInspector]
    public bool startPlaying;

    public NoteObject[] allNotes;

    private bool _songLoopResetTriggered = false;
    public float loopResetOffsetSeconds = 0.5f;

    [Header("Note Colors")]
    public Color perfectHitColor = Color.green;
    public Color goodHitColor = Color.cyan;
    public Color normalHitColor = Color.yellow;
    public Color missColor = Color.red;
    public Color holdStartedColor = Color.blue;
    public Color holdPerfectColor = Color.magenta;
    public Color holdMissedEarlyColor = Color.red;

    [Header("Scoreboard Data")]
    public int perfectHits;
    public int goodHits;
    public int normalHits;
    public int misses;
    public int holdStarts;
    public int holdPerfects;
    public int holdMissedEarlies;
    public int totalScore;

    [Header("UI")]
    public Text scoreboardText;
    public Text statusText;

    public InputRecorder inputRecorder;

    void Start()
    {
        instance = this;
        if (musicSource != null)
        {
            musicSource.loop = true;
        }
        ResetScore();
        UpdateStatusText("");

        if (inputRecorder != null)
        {
            inputRecorder.changeEvent.AddListener(OnRecorderChange);
        }
        else
        {
            Debug.LogError("InputRecorder reference not set in GameManager!", this);
        }
    }

    private void OnEnable()
    {
        if (startAction != null)
        {
            startAction.action.performed += OnStartPressed;
            startAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (startAction != null)
        {
            startAction.action.performed -= OnStartPressed;
            startAction.action.Disable();
        }
        if (inputRecorder != null)
        {
            inputRecorder.changeEvent.RemoveListener(OnRecorderChange);
        }
    }

    private void OnStartPressed(InputAction.CallbackContext context)
    {
        if (!startPlaying)
        {
            StartGame(); 
            UpdateStatusText("");
        }
    }

    private void OnRecorderChange(InputRecorder.Change change)
    {
        switch (change)
        {
            case InputRecorder.Change.CaptureStarted:
                ResetGameAndPrepareForRecording();
                UpdateStatusText("Now Recording...");
                break;
            case InputRecorder.Change.ReplayStarted:
                ResetGameAndPrepareForPlayback();
                UpdateStatusText("Replaying...");
                break;
            case InputRecorder.Change.CaptureStopped:
            case InputRecorder.Change.ReplayStopped:
                ResetSong();
                UpdateStatusText("");
                break;
        }
    }

    private void ResetGameAndPrepareForRecording()
    {
        if (startPlaying || musicSource.isPlaying || noteScroller.hasStarted)
        {
            ResetSong();
        }
        StartGame(); 
    }

    private void ResetGameAndPrepareForPlayback()
    {
        ResetSong();
        double scheduledStartTime = AudioSettings.dspTime;
        StartGame(scheduledStartTime); 
    }

    public void StartGame(double scheduledStartTime = -1) 
    {
        startPlaying = true;

        if (musicSource != null)
        {
            musicSource.time = 0f; 
            if (scheduledStartTime >= 0)
            {
                musicSource.PlayScheduled(scheduledStartTime);
            }
            else
            {
                musicSource.Play();
            }
        }

        if (noteScroller != null)
            noteScroller.SetScrollerStarted(true);

        _songLoopResetTriggered = false;
        ResetScore();
        Debug.Log("Game Started!");
    }

    public void ResetSong()
    {
        if (musicSource != null)
            musicSource.Stop();

        if (noteScroller != null)
            noteScroller.FullResetScroller();

        startPlaying = false;
        _songLoopResetTriggered = false;
        ResetScore();
        UpdateStatusText("");
        Debug.Log("Full Game Reset!");
    }

    private void HandleLoopReset()
    {
        if (noteScroller != null)
            noteScroller.LoopResetScroller();

        Debug.Log("Song loop detected! Resetting NoteScroller for next loop.");
    }

    void Update()
    {
        if (startPlaying && musicSource != null && musicSource.clip != null)
        {
            float timeRemainingInLoop = musicSource.clip.length - musicSource.time;

            if (timeRemainingInLoop <= loopResetOffsetSeconds && !_songLoopResetTriggered)
            {
                _songLoopResetTriggered = true;
                HandleLoopReset();
            }
            else if (timeRemainingInLoop > loopResetOffsetSeconds && _songLoopResetTriggered)
            {
                _songLoopResetTriggered = false;
            }
        }
    }

    public void ResetScore()
    {
        perfectHits = 0;
        goodHits = 0;
        normalHits = 0;
        misses = 0;
        holdStarts = 0;
        holdPerfects = 0;
        holdMissedEarlies = 0;
        totalScore = 0;
        DisplayScore();
    }

    public void DisplayScore()
    {
        if (scoreboardText != null)
        {
            string scoreDisplay =
                $"Perfect: {perfectHits}\n" +
                $"Good: {goodHits}\n" +
                $"Normal: {normalHits}\n" +
                $"Missed: {misses}\n" +
                $"Hold Started: {holdStarts}\n" +
                $"Hold Perfect: {holdPerfects}\n" +
                $"Hold Missed Early: {holdMissedEarlies}\n" +
                $"Total Score: {totalScore}";

            scoreboardText.text = scoreDisplay;
        }
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public void NotePerfectHit(NoteObject note)
    {
        perfectHits++;
        totalScore += 100;
        Debug.Log("Perfect Hit!");
        note.DisplayHitResult(perfectHitColor);
        DisplayScore();
    }
    public void NoteGoodHit(NoteObject note)
    {
        goodHits++;
        totalScore += 50;
        Debug.Log("Good Hit!");
        note.DisplayHitResult(goodHitColor);
        DisplayScore();
    }
    public void NoteNormalHit(NoteObject note)
    {
        normalHits++;
        totalScore += 20;
        Debug.Log("Normal Hit!");
        note.DisplayHitResult(normalHitColor);
        DisplayScore();
    }
    public void NoteMissed(NoteObject note)
    {
        misses++;
        totalScore -= 10;
        Debug.Log("Missed!");
        note.DisplayHitResult(missColor);
        DisplayScore();
    }
    public void NoteHoldStarted(NoteObject note)
    {
        holdStarts++;
        Debug.Log("Hold Note Started!");
        note.DisplayHoldStarted(holdStartedColor);
        DisplayScore();
    }
    public void NoteHoldPerfect(NoteObject note)
    {
        holdPerfects++;
        totalScore += 150;
        Debug.Log("Hold Note Perfect!");
        note.DisplayHitResult(holdPerfectColor);
        DisplayScore();
    }
    public void NoteHoldMissedEarly(NoteObject note)
    {
        holdMissedEarlies++;
        totalScore -= 20;
        Debug.Log("Hold Note Missed Early!");
        note.DisplayHitResult(holdMissedEarlyColor);
        DisplayScore();
    }
}
