using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Pool;

// Note: Error handling has been excluded from this script since not the main focus of example.

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    public class Player : MonoBehaviour
    {
        [Header("Input Bindings")]
        public InputActionReference move;
        public InputActionReference look;
        public InputActionReference interact;
        public InputActionReference use;

        [Header("Gameplay")]
        [Tooltip("The gameplay manager")]
        public GameplayManager manager;

        public GameObject target;
        public GameObject fire;
        public GameObject omniFire;

        public GameObject particle;
        public GameObject belt;
        public GameObject barrel;

        public float movementSpeed = 10.0f;
        public float fireRate = 0.25f;
        public float omniFireRate = 1.0f;

        public Renderer[] animatedRenderers;

        private Material m_Material;
        private Vector3 m_TargetPosition;
        private Vector3 m_TargetEulerAngles;
        private Color m_TargetColor;
        private float m_TargetScale;

        private static readonly Color[] Colors = { Color.red, Color.yellow };
        private static readonly int Color1 = Shader.PropertyToID("_Color");

        private int m_ColorIndex;
        private double m_TimeUntilNextFire = 0.0f;
        private double m_TimeUntilNextOmniFire = 0.0f;
        private bool m_OmniFire = false;

        private float m_TargetBeltAngle;
        private float m_BeltAngle;
        private float m_BarrelPosition;

        private ObjectPool<Bullet> m_ObjectPool;

        private Rigidbody m_Rigidbody;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();

            m_BarrelPosition = barrel.transform.localPosition.y;
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

            // Initialize color and target color
            m_TargetColor = GetColor(m_OmniFire);

            // Create an object pool for bullets/projectiles
            m_ObjectPool = new ObjectPool<Bullet>(
                createFunc: () => Instantiate(particle).GetComponent<Bullet>(),
                actionOnGet: (bullet) =>  bullet.gameObject.SetActive(true),
                actionOnRelease: (bullet) =>  bullet.gameObject.SetActive(false),
                actionOnDestroy: (bullet) => Destroy(bullet.gameObject));
        }

        private static Color GetColor(bool omniFire)
        {
            return omniFire ? Color.yellow : Color.red;
        }

        private void OnEnable()
        {
            if (target != null)
                m_TargetPosition = target.transform.position;

            move?.action?.Enable();
            look?.action?.Enable();
            interact?.action?.Enable();
            use?.action?.Enable();

            m_TimeUntilNextFire = 0.0f;
            m_TimeUntilNextOmniFire = 0.0f;

            // fire.transform.localScale = omniFire ? Vector3.zero : Vector3.one;
            // omniFire.transform.localScale = omniFire ? Vector3.one : Vector3.zero;
        }

        private void OnDisable()
        {
            move?.action?.Disable();
            look?.action?.Disable();
            interact?.action?.Disable();
            use?.action?.Disable();
        }

        private void Fire(float deltaTime)
        {
            // When we "Interact", we move to the next target color
            m_TimeUntilNextFire -= deltaTime;
            if (interact.action.IsPressed() && m_TimeUntilNextFire <= 0.0f)
            {
                if (m_OmniFire)
                {
                    OmniFire();
                    m_TimeUntilNextFire += omniFireRate;
                }
                else
                {
                    Fire(transform.up);
                    m_TimeUntilNextFire += fireRate;
                }
            }
            if (m_TimeUntilNextFire < 0.0f)
                m_TimeUntilNextFire = 0.0f;
        }

        private void ChangeWeapon(float deltaTime)
        {
            m_TimeUntilNextOmniFire -= deltaTime;
            if (use.action.WasPressedThisFrame())
            {
                m_OmniFire = !m_OmniFire;
                m_TargetScale = m_OmniFire ? 1.0f : 0.0f;
                m_BeltAngle += 360.0f;
                m_TargetColor = m_OmniFire ? Color.yellow : Color.red;
            }
        }

        private void AnimateChangeWeapon(float deltaTime)
        {
            // Animate scale of fire vs omni-fire to be the inverse of each other
            var omniFireScale = Mathf.Lerp(omniFire.transform.localScale.x, m_TargetScale, deltaTime * 10.0f);
            fire.transform.localScale = new Vector3(1.0f - omniFireScale, 1.0f - omniFireScale, 1.0f - omniFireScale);
            omniFire.transform.localScale = new Vector3(omniFireScale, omniFireScale, omniFireScale);
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

        private void Rotate(float deltaTime)
        {
            // If the underlying control is a relative control we should not scale with time.
            // If the underlying control is absolute, we scale magnitude with elapsed time.
            // We do not want to use physics for this rotation and hence use an object without rigidbody.
            var timeInvariant = (look.action.activeControl is DeltaControl);
            var scale = timeInvariant ? 1.0f : deltaTime * 300.0f;
            var angle = look.action.ReadValue<Vector2>().x * -1.0f * scale;
            target.transform.Rotate(Vector3.forward, angle, Space.World);
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            Fire(deltaTime);
            ChangeWeapon(deltaTime);
            Rotate(deltaTime);

            AnimateChangeWeapon(deltaTime);
            AnimateFireWeapon(deltaTime);

            if (manager.TryTeleportOrthographicExtents(transform.position, out var result))
                transform.position = result;

            // Animate material
            if (m_Material != null)
                m_Material.SetColor(Color1, Color.Lerp(m_Material.color, m_TargetColor, Time.deltaTime * 2.0f));
        }

        private void OnCollisionEnter(Collision other)
        {
            // If we are hit by a bullet apply force
            if (other.gameObject.GetComponent<Enemy>())
            {
                //manager.Explosion(animationTarget.transform, other.GetContact(0).point);
                //pool.Release(this);
                manager.GameOver(); //Destroy(gameObject); // TODO End game
            }
        }

        private void FixedUpdate()
        {
            // Use physics to animate player movement to get a feeling of inertia.
            var moveValue = move.action.ReadValue<Vector2>();
            var y = moveValue.y;
            if (y < 0.0f)
                y *= 0.33f;
            if (m_Rigidbody.linearVelocity.magnitude < 10.0f)
                m_Rigidbody.AddRelativeForce(Vector3.up * (10.0f * y) + Vector3.right * (5.0f * moveValue.x), ForceMode.Acceleration);
        }

        private void OmniFire()
        {
            // Fire in all directions with 45 degree offset for each bullet
            for (var i = 0; i < 8; ++i)
                Fire(Quaternion.AngleAxis(i * 45.0f, Vector3.forward) * transform.up);
        }

        private void Fire(Vector3 direction)
        {
            // Fire a single bullet in the direction of the player, approximately originating from the muzzle.
            var bullet = m_ObjectPool.Get();
            bullet.direction = direction;
            bullet.transform.position = transform.position + direction.normalized * (1.6f * transform.lossyScale.y);
            bullet.pool = m_ObjectPool;

            // Animate barrel to simulate recoil
            var pos = barrel.transform.localPosition;
            barrel.transform.localPosition = new Vector3(pos.x, m_BarrelPosition - 0.2f, pos.z);

            // Rotate the belt for each fired round, simulated a reload
            m_BeltAngle += 45.0f;
        }
    }
}
