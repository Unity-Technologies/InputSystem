using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class TankShooting : MonoBehaviour
{
    public int m_PlayerNumber = 1;                  // Used to identify the different players.
    public Rigidbody m_Shell;                       // Prefab of the shell.
    public Transform m_FireTransform;               // A child of the tank where the shells are spawned.
    public Slider m_AimSlider;                      // A child of the tank that displays the current launch force.
    public AudioSource m_ShootingAudio;             // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
    public AudioClip m_ChargingClip;                // Audio that plays when each shot is charging up.
    public AudioClip m_FireClip;                    // Audio that plays when each shot is fired.
    public float m_MinLaunchForce = 15f;            // The force given to the shell if the fire button is not held.
    public float m_MaxLaunchForce = 30f;            // The force given to the shell if the fire button is held for the max charge time.
    public float m_MaxChargeTime = 0.75f;           // How long the shell can charge for before it is fired at max force.

    private float m_CurrentLaunchForce;             // The force that will be given to the shell when the fire button is released.
    private float m_ChargeSpeed;                    // How fast the launch force increases, based on the max charge time.
    private bool m_Fired;                           // Whether or not the shell has been launched with this button press.
    private bool m_FireButtonPressedThisFrame;      // Will be set to true when the fire button is initially pressed.
    private bool m_FireButtonReleasedThisFrame;     // Will be set to true when the fire button is first released.
    private bool m_FireButtonDown;                  // Will always be true while the fire button is held down.


    private void OnEnable()
    {
        // When the tank is turned on, reset the launch force and the UI
        m_CurrentLaunchForce = m_MinLaunchForce;
        m_AimSlider.value = m_MinLaunchForce;
    }

    private void Start()
    {
        // The rate that the launch force charges up is the range of possible forces by the max charge time.
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
    }

    private void Update()
    {
        // The slider should have a default value of the minimum launch force.
        m_AimSlider.value = m_MinLaunchForce;

        // If the max force has been exceeded and the shell hasn't yet been launched...
        if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
        {
            // ... use the max force and launch the shell.
            m_CurrentLaunchForce = m_MaxLaunchForce;
            Fire();
        }
        // Otherwise, if the fire button has just started being pressed...
        else if (m_FireButtonPressedThisFrame && !m_FireButtonDown)
        {
            // ... reset the fired flag and reset the launch force.
            m_Fired = false;
            m_CurrentLaunchForce = m_MinLaunchForce;

            // Change the clip to the charging clip and start it playing.
            m_ShootingAudio.clip = m_ChargingClip;
            m_ShootingAudio.Play();

            m_FireButtonDown = true;
            m_FireButtonPressedThisFrame = false;
        }
        // Otherwise, if the fire button is being held and the shell hasn't been launched yet...
        else if (!m_FireButtonPressedThisFrame && m_FireButtonDown && !m_Fired && !m_FireButtonReleasedThisFrame)
        {
            // Increment the launch force and update the slider.
            m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;

            m_AimSlider.value = m_CurrentLaunchForce;
        }
        // Otherwise, if the fire button is released and the shell hasn't been launched yet...
        else if (m_FireButtonReleasedThisFrame && !m_Fired)
        {
            // ... launch the shell.
            Fire();
        }
    }

    private void Fire()
    {
        // Set the fired flag so only Fire is only called once.
        m_Fired = true;

        // Reset the button flags.
        m_FireButtonPressedThisFrame = false;
        m_FireButtonReleasedThisFrame = false;
        m_FireButtonDown = false;

        // Create an instance of the shell and store a reference to it's rigidbody.
        Rigidbody shellInstance =
            Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

        // Set the shell's velocity to the launch force in the fire position's forward direction.
        shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward;

        // Change the clip to the firing clip and play it.
        m_ShootingAudio.clip = m_FireClip;
        m_ShootingAudio.Play();

        // Reset the launch force.  This is a precaution in case of missing button events.
        m_CurrentLaunchForce = m_MinLaunchForce;
    }

    // The callback from the TanksInputActions Player Input asset that is
    // triggered from the "Fire" action.
    private void OnFire(InputValue value)
    {
        // We have setup our button press action to be Press and Release
        // trigger behavior in the Press interaction of the Input Action asset.
        // The isPressed property will be true
        // when OnFire is called during initial button press.
        // It will be false when OnFire is called during button release.

        if (value.isPressed)
        {
            m_FireButtonPressedThisFrame = true;
            m_FireButtonReleasedThisFrame = false;
        }
        else
        {
            m_FireButtonPressedThisFrame = false;
            m_FireButtonReleasedThisFrame = true;
        }
    }
}
