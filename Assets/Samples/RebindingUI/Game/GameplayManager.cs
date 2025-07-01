using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

// This sample can be optimized with pooled explosions.

public class GameplayManager : MonoBehaviour
{
    [Tooltip("The game camera")]
    public Camera gameCamera;

    [Tooltip("The enemy spawn rate")]
    public float enemySpawnRate = 1.0f;

    [Tooltip("The enemy spawn distance from center")]
    public float spawnDistance = 10.0f;

    [Tooltip("The enemy prefab for the mini game")]
    public GameObject enemy;

    [Tooltip("The explosion prefab for the mini game")]
    public GameObject enemyExplosion;

    [Tooltip("The player prefab for the mini game")]
    public GameObject player;

    [Tooltip("The explosion prefab for the mini game")]
    public GameObject playerExplosion;

    public GameObject explosion;

    [Tooltip("The message service.")]
    public GameObject messageService;

    private double m_TimeToNextSpawn;
    private GameObject m_Player;
    private ObjectPool<Enemy> m_EnemyPool;

    private float m_ShakeForce;
    private float m_ShakeMaxForce;
    private float m_ShakeDuration;
    private double m_ShakeTime;
    private Vector3 m_CameraPosition;

    private IShowMessages m_Messages;

    private int m_Round;
    private int m_RemainingEnemiesRound;
    private int m_EnemySpawnCount;

    private FeedbackController m_FeedbackController;

    public void KillEnemy()
    {
        --m_RemainingEnemiesRound;
    }

    public void GameOver()
    {
        m_Player.SetActive(false); // <--- TODO This collides with haptic effect sitting on player
        m_Messages.ShowMessage("GAME OVER", TimeSpan.FromSeconds(2), ResetGame);
    }

    private void Shake(float duration, float amplitude)
    {
        m_ShakeMaxForce = amplitude;
        m_ShakeForce = amplitude;
        m_ShakeDuration = duration;
        m_ShakeTime = Time.timeAsDouble;
    }

    public void Explosion(Transform target, Vector3 position, float amplitude, Color color, Material material = null)
    {
        var obj = Instantiate(enemyExplosion);
        obj.transform.position = target.position;
        obj.transform.rotation = target.rotation;

        // If we are provided a material, use that for all debris
        if (material != null)
        {
            var renderers = obj.GetComponentsInChildren<MeshRenderer>();
            foreach (var childRenderer in renderers)
                childRenderer.sharedMaterial = material;
        }

        // Set explosion position
        var exp = obj.GetComponent<Explosion>();
        exp.explosionPosition = position;

        // Modify the particle color
        var particles = exp.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.startColor = color;

        Shake(duration: 0.4f, amplitude: amplitude);
    }

    private static void WrapAround(ref float x, float min, float max)
    {
        if (x <= min)
            x = max;
        else if (x >= max)
            x = min;
    }

    private static bool TryTeleportOrthographicExtents(Camera camera, Vector3 position,
        out Vector3 result, float margin = 0.8f)
    {
        // Wrap around constraint x, y and teleport player if outside orthographic camera bounds
        if (camera && camera.orthographic)
        {
            var orthoSize = camera.orthographicSize;
            var horizontalExtent = orthoSize * camera.aspect;
            var newPosition = position;
            WrapAround(ref newPosition.x, -horizontalExtent - margin, horizontalExtent + margin);
            WrapAround(ref newPosition.y, -orthoSize - margin, orthoSize + margin);
            if (newPosition != position)
            {
                result = newPosition;
                return true;
            }
        }

        result = position;
        return false;
    }

    internal bool TryTeleportOrthographicExtents(Vector3 position, out Vector3 result, float margin = 0.8f)
    {
        return TryTeleportOrthographicExtents(gameCamera, position, out result, margin);
    }

    private void Awake()
    {
        m_FeedbackController = GetComponent<FeedbackController>();

        m_EnemyPool = new ObjectPool<Enemy>(
            createFunc: () =>
            {
                var enemyComponent = Instantiate(enemy).GetComponent<Enemy>();
                enemyComponent.pool = m_EnemyPool;
                enemyComponent.target = m_Player.transform;
                enemyComponent.manager = this;
                return enemyComponent;
            },
            actionOnGet: (obj) => obj.gameObject.SetActive(true),
            actionOnRelease: (obj) => obj.gameObject.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj.gameObject));

        m_CameraPosition = gameCamera.transform.position;

        var components = messageService.GetComponents(typeof(IShowMessages));
        foreach (var component in components)
        {
            var messages = component as IShowMessages;
            if (messages == null)
                continue;
            m_Messages = messages;
            break;
        }
        Debug.Assert(m_Messages != null);
    }

    private void Start()
    {
        // Instantiate and initialize the player
        m_Player = Instantiate(player, transform, worldPositionStays: true);
        var playerComponent = m_Player.GetComponent<Player>();
        playerComponent.manager = this;

        // Setup feedback controller
        var playerController = m_Player.GetComponent<PlayerController>();
        playerController.feedbackController = m_FeedbackController;

        // Delay first spawn so player has a chance to get ready
        m_TimeToNextSpawn = 3.0f;

        // Make sure no message is visible
        m_Messages.HideMessage();
    }

    private void OnEnable()
    {
        messageService.gameObject.SetActive(true);

        Application.focusChanged += OnApplicationFocusChanged;
        if (Application.isFocused)
            ResumeGame();
        else
            m_Messages.ShowMessage("PAUSED");
    }

    private void OnDisable()
    {
        //messageService.gameObject.SetActive(false); // TODO May sometimes be destroyed, fix, reference survices scene load
        PauseGame();
    }

    private void OnApplicationFocusChanged(bool focus)
    {
        // If application looses focus, pause the game, else resume the game
        if (focus)
            ResumeGame();
        else
            PauseGame();
    }

    void ResetGame()
    {
        messageService.gameObject.SetActive(false);
        m_Messages = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ResumeGame()
    {
        Time.timeScale = 1.0f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        m_Messages?.HideMessage(); // <-- issue also here
    }

    void PauseGame()
    {
        Time.timeScale = 0.0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        m_Messages?.ShowMessage("PAUSED");
    }

    void SpawnEnemy()
    {
        if (m_EnemySpawnCount == 0)
            return;

        m_TimeToNextSpawn -= Time.deltaTime;
        if (m_TimeToNextSpawn > 0.0f)
            return;

        m_TimeToNextSpawn += enemySpawnRate;
        --m_EnemySpawnCount;

        // Rent an enemy from the enemy pool
        var enemyComponent = m_EnemyPool.Get();

        // Make the enemy spawn on border of visible game area
        var orthoSize = gameCamera.orthographicSize;
        var horizontalExtent = orthoSize * gameCamera.aspect;
        var axis = Random.Range(-1.0f, 1.0f);
        var margin = 0.5f;
        var random = Random.Range(0, 4);
        switch (random)
        {
            case 0:
                enemyComponent.transform.position = new Vector3(axis * horizontalExtent, orthoSize + margin, 0.0f);
                break;
            case 1:
                enemyComponent.transform.position = new Vector3(axis * horizontalExtent, -orthoSize - margin, 0.0f);
                break;
            case 2:
                enemyComponent.transform.position = new Vector3(-horizontalExtent - margin, axis * orthoSize, 0.0f);
                break;
            case 3:
                enemyComponent.transform.position = new Vector3(horizontalExtent + margin, axis * orthoSize, 0.0f);
                break;
        }
    }

    void AnimateCameraShake()
    {
        var time = Time.timeAsDouble;
        var elapsed = (time - m_ShakeTime);
        var t = m_ShakeDuration <= 0.0f ? 1.0f : elapsed / m_ShakeDuration;
        m_ShakeForce = Mathf.Lerp(m_ShakeMaxForce, 0.0f, (float)t);

        var cameraShakeOffset = new Vector3(
            m_ShakeForce * Mathf.Sin((float)time * 71.0f),
            m_ShakeForce * Mathf.Sin((float)time * 53.0f + Mathf.PI / 3.0f),
            0f);

        gameCamera.transform.position = m_CameraPosition + cameraShakeOffset;

        // Apply shake to feedback controller if available
        if (m_FeedbackController != null)
            m_FeedbackController.rumble = m_ShakeForce;
    }

    void AnimateHapticShake()
    {
        // Abort if there is no gamepad available or if the player is not currently using a gamepad
        // var gamepad = Gamepad.current;
        // if (gamepad == null)
        //     return;

        // Animate motor speeds uniformly according to shake for a simple immersive effect
        // var frequency = 0.0f;
        // if (m_ShakeDuration > 0.0f)
        //     frequency = 1.0f - m_ShakeDuration;
        // if (frequency > 0.0f)
        //     gamepad.SetMotorSpeeds(lowFrequency: 0.2f, highFrequency: 0.0f);
        // else
        //     gamepad.SetMotorSpeeds(lowFrequency: 0.0f, highFrequency: 0.0f);
        //gamepad.SetMotorSpeeds(lowFrequency: m_ShakeDuration > 0.0f ? 0.2f : 0.0f, highFrequency: 0.0f);
    }

    void NextRound()
    {
        m_Messages.ShowMessage($"ROUND {++m_Round}", TimeSpan.FromSeconds(3));
        m_EnemySpawnCount = 5 + m_Round * 2;
        m_RemainingEnemiesRound = m_EnemySpawnCount;
        enemySpawnRate *= 0.9f;
    }

    void Update()
    {
        // If all enemies have been eliminated, begin a new round
        if (m_RemainingEnemiesRound == 0)
            NextRound();

        SpawnEnemy();
        AnimateCameraShake();
        AnimateHapticShake();
    }
}
