using System;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

// This sample can be optimized with pooled explosions.

namespace UnityEngine.InputSystem.Samples.RebindUI
{
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

        /// <summary>
        /// Returns the current game level.
        /// </summary>
        public int level { get; private set; }

        /// <summary>
        /// Game state.
        /// </summary>
        public enum GameplayState
        {
            None,
            StartLevel,
            Playing,
            CompleteLevel,
            GameOver,
            ResetGame,
        }

        /// <summary>
        /// Returns the current game state.
        /// </summary>
        public GameplayState state => m_GameplayState;

        /// <summary>
        /// Event fired when game state changes.
        /// </summary>
        public event Action<GameplayState> GameplayStateChanged;

        /// <summary>
        /// Event fired when pause state changes.
        /// </summary>
        public event Action<bool> PauseChanged;

        private double m_TimeToNextSpawn;
        private GameObject m_Player;
        private ObjectPool<Enemy> m_EnemyPool;

        private float m_ShakeForce;
        private float m_ShakeMaxForce;
        private float m_ShakeDuration;
        private double m_ShakeTime;
        private Vector3 m_CameraPosition;

        private int m_RemainingEnemiesOnThisLevel;
        private int m_EnemySpawnCount;

        private FeedbackController m_FeedbackController;

        private GameplayState m_GameplayState = GameplayState.None;

        private double m_EarliestTimeToChangeState;

        /// <summary>
        /// Get/set whether the game is paused.
        /// </summary>
        public bool paused
        {
            get => Time.timeScale == 0.0f;
            set
            {
                if ((value && Time.timeScale == 0.0f) || (!value && Time.timeScale != 0.0f))
                    return;

                Time.timeScale = value ? 0.0f : 1.0f;
                UpdateCursor();

                PauseChanged?.Invoke(value);
            }
        }

        public void KillEnemy()
        {
            --m_RemainingEnemiesOnThisLevel;
        }

        public void GameOver()
        {
            m_Player.SetActive(false);
            m_NextGameplayState = GameplayState.GameOver;
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

        internal bool IsInsideGameplayArea(Vector3 position, float margin = 0.8f)
        {
            if (!gameCamera || !gameCamera.orthographic)
                return true;

            var orthoSize = gameCamera.orthographicSize;
            var horizontalExtent = orthoSize * gameCamera.aspect;
            return (position.x >= -horizontalExtent - margin) &&
                (position.x <= horizontalExtent + margin) &&
                (position.y >= -orthoSize - margin) &&
                (position.y <= orthoSize + margin);
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
            // This game is designed for landscape orientation, so make sure we use it.
            Screen.orientation = ScreenOrientation.LandscapeLeft;

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

            m_EarliestTimeToChangeState = Time.timeAsDouble;
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
        }

        private void OnEnable()
        {
            Application.focusChanged += OnApplicationFocusChanged;
            paused = !Application.isFocused;
        }

        private void OnDisable()
        {
            Application.focusChanged -= OnApplicationFocusChanged;
            paused = true;
        }

        private void OnApplicationFocusChanged(bool focus)
        {
            paused = !focus;
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

        private GameplayState m_NextGameplayState = GameplayState.StartLevel;

        void Update()
        {
            var now = Time.time;
            while (now >= m_EarliestTimeToChangeState && m_NextGameplayState != m_GameplayState)
            {
                m_EarliestTimeToChangeState = now;

                // Transition exit
                switch (m_GameplayState)
                {
                    case GameplayState.None: break;
                    case GameplayState.StartLevel: break;
                    case GameplayState.Playing: break;
                    case GameplayState.CompleteLevel: break;
                    case GameplayState.GameOver:
                    case GameplayState.ResetGame: break;
                }

                m_GameplayState = m_NextGameplayState;

                // Transition enter
                switch (m_NextGameplayState)
                {
                    case GameplayState.None:
                        m_NextGameplayState = GameplayState.StartLevel;
                        break;

                    case GameplayState.StartLevel:
                        m_EnemySpawnCount = 5 + (++level) * 2;
                        m_RemainingEnemiesOnThisLevel = m_EnemySpawnCount;
                        enemySpawnRate *= 0.9f;

                        m_EarliestTimeToChangeState += 2.0;
                        m_NextGameplayState = GameplayState.Playing;
                        break;

                    case GameplayState.Playing:
                        break;

                    case GameplayState.CompleteLevel:
                        m_NextGameplayState = GameplayState.StartLevel;
                        break;

                    case GameplayState.GameOver:
                        m_EarliestTimeToChangeState += 3.0;
                        m_NextGameplayState = GameplayState.ResetGame;
                        break;

                    case GameplayState.ResetGame:
                        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                        break;
                }

                // Notify listeners about gameplay state being updated
                GameplayStateChanged?.Invoke(m_GameplayState);
            }

            // Spawn enemies while in playing state only
            if (state == GameplayState.Playing)
            {
                if (m_RemainingEnemiesOnThisLevel == 0)
                    m_NextGameplayState = GameplayState.CompleteLevel;
                else
                    SpawnEnemy();
            }

            // Always animate regardless of state
            AnimateCameraShake();
        }

        void UpdateCursor()
        {
            if (paused)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}
