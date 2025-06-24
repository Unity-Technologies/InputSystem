using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Note: Error handling has been excluded from this script since not the main focus of example.

[RequireComponent(typeof(MeshRenderer))]
public class RebindingUISampleController : MonoBehaviour
{
    public GameObject target;
    public GameObject secondaryTarget;
    public InputActionReference move;
    public InputActionReference look;
    public InputActionReference interact;
    public InputActionReference use;
    public InputActionReference menu;

    public float movementSpeed = 10.0f;

    private Material m_Material;
    private Vector3 m_TargetPosition;
    private Vector3 m_TargetEulerAngles;
    private Color m_TargetColor;
    private float m_TargetScale = 1.0f;

    private static readonly Color[] Colors = { Color.red, Color.green, new Color(0.2f, 0.2f, 1.0f), Color.yellow };
    private static readonly int Color1 = Shader.PropertyToID("_Color");

    private int m_ColorIndex;

    private void Start()
    {
        m_Material = GetComponent<Renderer>().sharedMaterial;
        m_TargetColor = Colors[m_ColorIndex];
    }

    private void OnEnable()
    {
        if (target != null)
            m_TargetPosition = target.transform.position;

        move?.action?.Enable();
        look?.action?.Enable();
        interact?.action?.Enable();
        use?.action?.Enable();
    }

    private void OnDisable()
    {
        move?.action?.Disable();
        look?.action?.Disable();
        interact?.action?.Disable();
        use?.action?.Disable();
    }

    private void Update()
    {
        // When we "Move" we add to the target position
        if (move != null && move.action != null)
            m_TargetPosition += (Vector3)(move.action.ReadValue<Vector2>() * Time.deltaTime * movementSpeed);

        // When we "Look" we rotate the target object relative to its current orientation.
        if (look != null && look.action != null && target != null)
        {
            // If the underlying control is a relative control we should not scale with time.
            // If the underlying control is absolute, we sample magnitude with elapsed time
            // to convert absolute movement to movement per time unit.
            var timeInvariant = (look.action.activeControl is DeltaControl);
            var scale = timeInvariant ? 1.0f : Time.deltaTime * 300.0f;

            target.transform.Rotate(Vector3.up, look.action.ReadValue<Vector2>().x * -1.0f * scale, Space.World);
            target.transform.Rotate(Vector3.right, look.action.ReadValue<Vector2>().y * 1.0f * scale, Space.World);
        }

        // When we "Interact", we move to the next target color
        if (interact.action.WasPressedThisFrame())
            m_TargetColor = Colors[(++m_ColorIndex % Colors.Length)];

        // When we "Use", we toggle scale of secondary object
        if (use.action.WasPerformedThisFrame())
            m_TargetScale = (Mathf.Approximately(m_TargetScale, 0.0f) ? 1.0f : 0.0f);

        // Animate towards target position, target material color
        if (target != null)
            target.transform.position = Vector3.Lerp(target.transform.position, m_TargetPosition, Time.deltaTime * movementSpeed);
        if (m_Material != null)
            m_Material.SetColor(Color1, Color.Lerp(m_Material.color, m_TargetColor, Time.deltaTime * 2.0f));

        // Animate scale of secondary object
        if (secondaryTarget != null)
        {
            var scale = Mathf.Lerp(secondaryTarget.transform.localScale.x, m_TargetScale,
                Time.deltaTime * 10.0f);
            secondaryTarget.transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
