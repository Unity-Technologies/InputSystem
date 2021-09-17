using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Generator : MonoBehaviour
{
    public GameObject LetterBoxPrefab;
    public float Gravity = -1f;
    public float InitialAverageSpawnPeriod = 1f;
    public float AverageSpawnTimeFactor = 0.95f;
    public float Margin = 1.0f;
    public bool UseLegacyInput = false;
    public bool Invincible = true;

    private float nextSpawnTime;
    private float averageSpawnPeriod;

    private Vector3 upperLeft, upperRight, lowerLeft, lowerRight;

    public InputActionReference A;
    public InputActionReference B;
    public InputActionReference C;
    public InputActionReference D;

    private class LetterObject
    {
        public GameObject Object;
        public char Character;
        public KeyCode LegacyKeyCode;
        public Key Key;
    }

    private List<LetterObject> activeLetters = new List<LetterObject>();

    private void Awake()
    {
        A.action.performed += Action_performed;
    }

    // Start is called before the first frame update
    void Start()
    {
        ResetGame();
    }

    private void Action_performed(InputAction.CallbackContext obj)
    {
        Debug.Log("TRIGGER A");
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCamera();

        var now = Time.realtimeSinceStartup;
        if (now > nextSpawnTime)
            Spawn(now);

        CheckIfOutside();

        if (UseLegacyInput)
            LegacyInput();
        else
            HandleInput();

        var letterBoxWasOutsideScreen = CheckIfOutside();
        if (letterBoxWasOutsideScreen)
            GameOver();
    }

    public void ResetGame()
    {
        Physics.gravity = new Vector3(0, Gravity, 0);

        averageSpawnPeriod = InitialAverageSpawnPeriod;
        nextSpawnTime = Time.realtimeSinceStartup;

        foreach (var obj in activeLetters)
            Destroy(obj.Object);
        activeLetters.Clear();
    }

    private void LegacyInput()
    {
        for (var i=activeLetters.Count-1; i >= 0; --i)
        {
            if (Input.GetKeyDown(activeLetters[i].LegacyKeyCode))
            {
                Destroy(activeLetters[i].Object);
                activeLetters.RemoveAt(i);
            }
        }
    }

    private void HandleInput()
    {
        for (var i=activeLetters.Count-1; i >= 0; --i)
        {
            if (Keyboard.current[activeLetters[i].Key].wasPressedThisFrame)
            {
                Destroy(activeLetters[i].Object);
                activeLetters.RemoveAt(i);
            }
        }
    }

    private void GameOver()
    {
        ResetGame();
    }

    private bool CheckIfOutside()
    {
        var result = false;
        for (var i = activeLetters.Count-1; i >= 0; --i)
        {
            if (activeLetters[i].Object.transform.position.y + Margin < lowerLeft.y)
            {
                Destroy(activeLetters[i].Object);
                activeLetters.RemoveAt(i);
                result = true;
            }
        }
        return result;
    }

    private void UpdateCamera()
    {
        var depth = 0;
        var upperLeftScreen = new Vector3(0, Screen.height, depth);
        var upperRightScreen = new Vector3(Screen.width, Screen.height, depth);
        var lowerLeftScreen = new Vector3(0, 0, depth);
        var lowerRightScreen = new Vector3(Screen.width, 0, depth);

        upperLeft = Camera.main.ScreenToWorldPoint(upperLeftScreen);
        upperRight = Camera.main.ScreenToWorldPoint(upperRightScreen);
        lowerLeft = Camera.main.ScreenToWorldPoint(lowerLeftScreen);
        lowerRight = Camera.main.ScreenToWorldPoint(lowerRightScreen);
    }

    private void Spawn(float now)
    {
        averageSpawnPeriod *= AverageSpawnTimeFactor;
        nextSpawnTime = now + averageSpawnPeriod;

        // Instantiate
        var letterObject = Instantiate(LetterBoxPrefab);
        letterObject.SetActive(true);

        // Setup random letter
        var textMeshes = letterObject.GetComponentsInChildren<TextMeshPro>();
        var letter = (char)Random.Range(65, 90); // capital letter ascii range
        foreach (var textMesh in textMeshes)
            textMesh.text = letter.ToString();

        // Setup random position
        var marginX = 0.1f;
        var normalizedX = Random.value * (1.0f - 2 * marginX) + marginX;
        var x = Mathf.Lerp(upperLeft.x, upperRight.x, normalizedX);
        letterObject.transform.position = new Vector3(x, upperLeft.y + Margin, 0);

        // Add some random torque to make it a bit more interesting
        var rigidbody = letterObject.GetComponent<Rigidbody>();
        rigidbody.AddTorque(Random.insideUnitCircle);

        // Compute legacy key code
        var legacyKeyCodeDiff = (int)KeyCode.A - 65;
        var legacyKeyCode = (KeyCode)(letter + legacyKeyCodeDiff);

        // Compute Input System key code
        var keyCodeDiff = (int)Key.A - 65;
        var keyCode = (Key)(letter + keyCodeDiff);

        activeLetters.Add(new LetterObject() { Object = letterObject, Character = letter, LegacyKeyCode = legacyKeyCode, Key = keyCode });
    }
}
