using System;
using UnityEngine.Pool;

// Note: Error handling has been excluded from this script since not the main focus of example.

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    public class Player : MonoBehaviour
    {
        // Since since its expected to be assigned at run-time
        [HideInInspector]
        [Tooltip("The gameplay manager")]
        public GameplayManager manager;

        [Tooltip("The fire object")]
        public GameObject fireObject;

        [Tooltip("The omni-fire object")]
        public GameObject omniFireObject;

        [Tooltip("The bullet/particle object")]
        public GameObject particle;

        [Tooltip("The cannon belt")]
        public GameObject belt;

        [Tooltip("The cannon barrel")]
        public GameObject barrel;

        [Tooltip("The regular fire rate")]
        public float fireRate = 0.25f;

        [Tooltip("The omni-fire rate")]
        public float omniFireRate = 1.0f;

        [Tooltip("The change rate")]
        public float changeRate = 1.0f;

        [Tooltip("List of color animation targets")]
        public Renderer[] animatedRenderers;

        /// <summary>
        /// Specifies whether the player is firing or not.
        /// </summary>
        public bool firing { get; set; }

        /// <summary>
        /// The move vector of the player that specifies movement direction and magnitude.
        /// </summary>
        public Vector2 move {  get; set; }

        /// <summary>
        /// Get the current color of the player.
        /// </summary>
        /// <returns>Current color.</returns>
        public Color GetColor() => GetColor(m_OmniFire); //m_Material != null ? m_Material.GetColor(Color1) : Color.black;

        /// <summary>
        /// Request mode change.
        /// </summary>
        public void Change()
        {
            m_ChangeRequested = true;
        }

        /// <summary>
        /// Rotate the player by the given angle.
        /// </summary>
        /// <param name="angle">Angle in degrees (additive).</param>
        public void Rotate(float angle)
        {
            m_RotationAngle += angle;
        }

        private static readonly int Color1 = Shader.PropertyToID("_Color");

        private Material m_Material;
        private Vector3 m_TargetEulerAngles;
        private Color m_TargetColor;
        private Color m_Color;
        private float m_TargetScale;

        private int m_ColorIndex;
        private float m_TimeUntilNextFire;
        private float m_TimeUntilNextChange;
        private bool m_OmniFire;
        private bool m_ChangeRequested;

        private float m_TargetBeltAngle;
        private float m_BeltAngle;
        private float m_BarrelPosition;
        private float m_RotationAngle;

        private ObjectPool<Bullet> m_ObjectPool;

        private Rigidbody m_Rigidbody;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();

            m_BarrelPosition = barrel.transform.localPosition.y;

            fireObject.transform.localScale = m_OmniFire ? Vector3.zero : Vector3.one;
            omniFireObject.transform.localScale = m_OmniFire ? Vector3.one : Vector3.zero;

            m_TargetColor = GetColor(m_OmniFire);
        }

        private void Start()
        {
            #if UNITY_EDITOR
            // Note that this creates a instance (copy) of the material we want to animate.
            // When then assign the instance to all tagged child renderers to benefit from
            // batching and allow animating color without affecting the asset in editor.
            foreach (var animatedRenderer in animatedRenderers)
            {
                if (animatedRenderer == null)
                    continue;
                if (m_Material == null)
                    m_Material = animatedRenderer.material;
                else
                    animatedRenderer.sharedMaterial = m_Material;
            }
            #else
            // When not in editor we can safely modify the shared material without
            // indirectly changing the source material.
            m_Material = animatedRenderers[0].sharedMaterial;
            #endif

            // Create an object pool for bullets/projectiles
            m_ObjectPool = new ObjectPool<Bullet>(
                createFunc: () =>
                {
                    var bullet = Instantiate(particle).GetComponent<Bullet>();
                    bullet.Initialize(manager, m_ObjectPool);
                    return bullet;
                },
                actionOnGet: (bullet) =>  bullet.gameObject.SetActive(true),
                actionOnRelease: (bullet) =>  bullet.gameObject.SetActive(false),
                actionOnDestroy: (bullet) => Destroy(bullet.gameObject));
        }

        private void OnEnable()
        {
            m_TimeUntilNextFire = 0.0f;
            m_TimeUntilNextChange = 0.0f;
        }

        private void UpdateFire(float deltaTime)
        {
            if (Throttle(ref m_TimeUntilNextFire, firing, deltaTime, m_OmniFire ? omniFireRate : fireRate))
                return;

            // Fire in all directions with 45 degree offset for each bullet
            if (m_OmniFire)
            {
                for (var i = 0; i < 8; ++i)
                    FireBullet(Quaternion.AngleAxis(i * 45.0f, Vector3.forward) * transform.up);
                return;
            }

            // Else: Fire in forward direction
            FireBullet(transform.up);
        }

        private static bool Throttle(ref float remainingTime, bool condition, float deltaTime, float timeUntilNextEvent)
        {
            remainingTime -= deltaTime;
            if (remainingTime > 0.0f)
                return true; // Enough time has not elapsed
            if (condition)
                remainingTime += timeUntilNextEvent;
            if (remainingTime < 0.0f)
                remainingTime = 0.0f;
            return !condition;
        }

        private void FireBullet(Vector3 direction)
        {
            // Fire a single bullet in the direction of the player, approximately originating from the muzzle.
            var bullet = m_ObjectPool.Get();
            bullet.direction = direction;
            bullet.transform.position = transform.position + direction.normalized * (1.6f * transform.lossyScale.y);

            // Animate barrel to simulate recoil
            var pos = barrel.transform.localPosition;
            barrel.transform.localPosition = new Vector3(pos.x, m_BarrelPosition - 0.2f, pos.z);

            // Rotate the belt for each fired round, simulated a reload
            m_BeltAngle += 45.0f;
        }

        private void UpdateChangeWeapon(float deltaTime)
        {
            if (Throttle(ref m_TimeUntilNextChange, m_ChangeRequested, deltaTime, changeRate))
                return;
            m_ChangeRequested = false;

            // Change weapon, animate change, belt rotation, color
            m_OmniFire = !m_OmniFire;
            m_TargetScale = m_OmniFire ? 1.0f : 0.0f;
            m_BeltAngle += 360.0f;
            m_TargetColor = GetColor(m_OmniFire);
        }

        private void UpdateRotate()
        {
            // We do not want to use physics for this rotation to give a more direct feel.
            transform.Rotate(Vector3.forward, m_RotationAngle, Space.World);

            // Reset rotation angle and let it accumulate until next update.
            m_RotationAngle = 0;
        }

        private void OnCollisionEnter(Collision other)
        {
            // If we collide with an enemy
            if (other.gameObject.GetComponent<Enemy>())
            {
                // Create an explosion matching our current color
                Color.RGBToHSV(GetColor(), out float h, out float s, out float v);
                var explosionColor = Color.HSVToRGB(h, s * 0.5f, v);
                manager.Explosion(transform, other.GetContact(0).point, 0.5f, explosionColor, m_Material);

                // End the game
                manager.GameOver();
            }
        }

        private void Update()
        {
            // Update game logic
            var deltaTime = Time.deltaTime;
            UpdateFire(deltaTime);
            UpdateChangeWeapon(deltaTime);
            UpdateRotate();
            if (manager.TryTeleportOrthographicExtents(transform.position, out var result))
                transform.position = result;

            // Animate
            AnimateChangeWeapon(deltaTime);
            AnimateFireWeapon(deltaTime);
            AnimateColors(deltaTime);
        }

        private void FixedUpdate()
        {
            // Use physics to animate player movement to get a feeling of inertia.
            //var moveValue = move.action.ReadValue<Vector2>();
            var y = move.y;
            if (y < 0.0f)
                y *= 0.33f;
            #if UNITY_6000_0_OR_NEWER
            var velocityMagnitude = m_Rigidbody.linearVelocity.magnitude;
            #else
            var velocityMagnitude = m_Rigidbody.velocity.magnitude;
            #endif
            if (velocityMagnitude < 10.0f)
                m_Rigidbody.AddRelativeForce(Vector3.up * (10.0f * y) + Vector3.right * (5.0f * move.x), ForceMode.Acceleration);
        }

        private void AnimateChangeWeapon(float deltaTime)
        {
            // Animate scale of fire vs omni-fire to be the inverse of each other
            var omniFireScale = Mathf.Lerp(omniFireObject.transform.localScale.x, m_TargetScale, deltaTime * 10.0f);
            fireObject.transform.localScale = new Vector3(1.0f - omniFireScale, 1.0f - omniFireScale, 1.0f - omniFireScale);
            omniFireObject.transform.localScale = new Vector3(omniFireScale, omniFireScale, omniFireScale);
        }

        private void AnimateFireWeapon(float deltaTime)
        {
            // Animate belt angle to simulate bullet reload
            m_BeltAngle = Mathf.Lerp(m_BeltAngle, m_TargetBeltAngle, deltaTime * 10.0f);
            belt.transform.localEulerAngles = new Vector3(0, m_BeltAngle, 0);

            // Animate barrel back to rest position after bullet has been fired
            var localPosition = barrel.transform.localPosition;
            barrel.transform.localPosition = new Vector3(
                localPosition.x,
                Mathf.Lerp(localPosition.y, m_BarrelPosition, deltaTime * 10.0f),
                localPosition.z);
        }

        private void AnimateColors(float deltaTime)
        {
            var color = Color.Lerp(m_Material.color, m_TargetColor, deltaTime * 2.0f);
            if (color != GetColor())
            {
                // Update material
                m_Material.SetColor(Color1, color);
            }
        }

        private static Color GetColor(bool omniFire)
        {
            return omniFire ? Color.yellow : Color.red;
        }
    }
}
