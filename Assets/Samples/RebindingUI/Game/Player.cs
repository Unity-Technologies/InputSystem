using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Pool;

// Note: Error handling has been excluded from this script since not the main focus of example.

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    [RequireComponent(typeof(MeshRenderer))]
    public class Player : MonoBehaviour
    {
        public GameObject target;
        public GameObject fire;
        public GameObject omniFire;
        public InputActionReference move;
        public InputActionReference look;
        public InputActionReference interact;
        public InputActionReference use;
        public InputActionReference menu;

        public GameObject particle;
        public GameObject belt;
        public GameObject barrel;

        public float movementSpeed = 10.0f;
        public float fireRate = 0.25f;
        public float omniFireRate = 1.0f;

        public new Camera camera;
        public GameplayManager manager;

        private Material m_Material;
        private Vector3 m_TargetPosition;
        private Vector3 m_TargetEulerAngles;
        private Color m_TargetColor;
        private float m_TargetScale;

        private static readonly Color[] Colors = { Color.red, Color.green, new Color(0.2f, 0.2f, 1.0f), Color.yellow };
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
            m_Material = GetComponent<Renderer>().sharedMaterial;
            m_TargetColor = Colors[m_ColorIndex];

            m_ObjectPool = new ObjectPool<Bullet>(
                createFunc: () => Instantiate(particle).GetComponent<Bullet>(),
                actionOnGet: (bullet) =>  bullet.gameObject.SetActive(true),
                actionOnRelease: (bullet) =>  bullet.gameObject.SetActive(false),
                actionOnDestroy: (bullet) => Destroy(bullet.gameObject));
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

        private void OldUpdate()
        {
            var deltaTime = Time.deltaTime;

            // When we "Move" we add to the target position
            /*if (move != null && move.action != null)
                m_TargetPosition += (Vector3)(move.action.ReadValue<Vector2>() * Time.deltaTime * movementSpeed);*/

            // When we "Look" we rotate the target object relative to its current orientation.
            if (look != null && look.action != null && target != null)
            {
                // If the underlying control is a relative control we should not scale with time.
                // If the underlying control is absolute, we sample magnitude with elapsed time
                // to convert absolute movement to movement per time unit.
                var timeInvariant = (look.action.activeControl is DeltaControl);
                var scale = timeInvariant ? 1.0f : deltaTime * 300.0f;

                //target.transform.Rotate(Vector3.up, look.action.ReadValue<Vector2>().x * -1.0f * scale, Space.World);
                //target.transform.Rotate(Vector3.right, look.action.ReadValue<Vector2>().y * 1.0f * scale, Space.World);
                target.transform.Rotate(Vector3.forward, look.action.ReadValue<Vector2>().x * -1.0f * scale, Space.World);
            }

            // When we "Move" we add to the target position
            if (move != null && move.action != null)
            {
                var moveValue = move.action.ReadValue<Vector2>();
                m_TargetPosition += transform.up * (moveValue.y * (deltaTime * movementSpeed))
                    + transform.right * (moveValue.x * (deltaTime * movementSpeed * 0.33f));
            }

            // When we "Interact", we move to the next target color
            m_TimeUntilNextFire -= deltaTime;
            if (interact.action.IsPressed() && m_TimeUntilNextFire <= 0.0f)
            {
                m_TargetColor = Colors[(++m_ColorIndex % Colors.Length)];

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

            // When we "Use", we toggle scale of secondary object
            m_TimeUntilNextOmniFire -= deltaTime;
            if (use.action.WasPerformedThisFrame())
            {
                //OmniFire();
                m_OmniFire = !m_OmniFire;
                m_TargetScale = m_OmniFire ? 1.0f : 0.0f;
            }

            // Animate towards target position
            if (target != null)
                target.transform.position = Vector3.Lerp(target.transform.position, m_TargetPosition, Time.deltaTime * movementSpeed);

            // Animate material
            //if (m_Material != null)
            //    m_Material.SetColor(Color1, Color.Lerp(m_Material.color, m_TargetColor, Time.deltaTime * 2.0f));

            // Animate scale of fire vs omni-fire to be the inverse of each other
            var omniFireScale = Mathf.Lerp(omniFire.transform.localScale.x, m_TargetScale, Time.deltaTime * 10.0f);
            fire.transform.localScale = new Vector3(1.0f - omniFireScale, 1.0f - omniFireScale, 1.0f - omniFireScale);
            omniFire.transform.localScale = new Vector3(omniFireScale, omniFireScale, omniFireScale);
        }

        private void Fire(float deltaTime)
        {
            // When we "Interact", we move to the next target color
            m_TimeUntilNextFire -= deltaTime;
            if (interact.action.IsPressed() && m_TimeUntilNextFire <= 0.0f)
            {
                m_TargetColor = Colors[(++m_ColorIndex % Colors.Length)];

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
            var timeInvariant = (look.action.activeControl is DeltaControl);
            var scale = timeInvariant ? 1.0f : deltaTime * 300.0f;
            target.transform.Rotate(Vector3.forward, look.action.ReadValue<Vector2>().x * -1.0f * scale, Space.World);
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
            var moveValue = move.action.ReadValue<Vector2>();
            var y = moveValue.y;
            if (y < 0.0f)
                y *= 0.33f;
            if (m_Rigidbody.linearVelocity.magnitude < 10.0f)
                m_Rigidbody.AddRelativeForce(10.0f * Vector3.up * y + 5.0f * Vector3.right * moveValue.x, ForceMode.Acceleration);
            //m_Rigidbody.AddRelativeForce(10.0f * Vector3.up * x, ForceMode.Acceleration);
            //m_Rigidbody.AddForce(10.0f * transform.right * x, ForceMode.Acceleration);


            //m_Rigidbody.AddForce(transform.up * 0.1f, ForceMode.Acceleration); // thrust
            // Debug.Log(scale);
            // if (m_Rigidbody.angularVelocity.magnitude < 1.0f)
            //     m_Rigidbody.AddRelativeTorque(0.0f, 0.0f, scale * 30.0f * -look.action.ReadValue<Vector2>().x, ForceMode.Acceleration); // left


            //m_Rigidbody.AddRelativeTorque(0.0f, 0.0f, 5.0f * -look.action.ReadValue<Vector2>().x, ForceMode.VelocityChange); // left
        }

        private void OmniFire()
        {
            // Fire in all directions with 45 degree offset for each bullet
            Fire(transform.up);
            Fire(Quaternion.AngleAxis(45.0f, Vector3.forward) * transform.up);
            Fire(Quaternion.AngleAxis(90.0f, Vector3.forward) * transform.up);
            Fire(Quaternion.AngleAxis(135.0f, Vector3.forward) * transform.up);
            Fire(Quaternion.AngleAxis(180.0f, Vector3.forward) * transform.up);
            Fire(Quaternion.AngleAxis(225.0f, Vector3.forward) * transform.up);
            Fire(Quaternion.AngleAxis(270.0f, Vector3.forward) * transform.up);
            Fire(Quaternion.AngleAxis(315.0f, Vector3.forward) * transform.up);
        }

        private void Fire(Vector3 direction)
        {
            // Fire a single bullet in the direction of the player
            var bulletOffset = 0.8f;
            var bullet = m_ObjectPool.Get();
            bullet.direction = direction;
            bullet.transform.position = transform.position + direction.normalized * bulletOffset;
            bullet.pool = m_ObjectPool;

            m_BeltAngle += 45.0f;

            var pos = barrel.transform.localPosition;
            barrel.transform.localPosition = new Vector3(pos.x, m_BarrelPosition - 0.2f, pos.z);
        }
    }
}
