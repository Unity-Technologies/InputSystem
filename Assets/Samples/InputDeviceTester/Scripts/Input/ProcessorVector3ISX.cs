using UnityEngine;
using UnityEngine.InputSystem;

public class ProcessorVector3ISX : ProcessorISX
{
    private Vector3 m_original = new Vector3(0, 0, 0);
    private Vector3 m_result = new Vector3(0, 0, 0);

    // Start is called before the first frame update
    void Start()
    {
        m_inputAction.Rename(gameObject.name);
        m_inputAction.performed += ctx => {
            InputControl<Vector3> control = ctx.control as InputControl<Vector3>;
            m_original = control.ReadValue();
            m_result = ctx.ReadValue<Vector3>();
        };
        m_inputAction.canceled += ctx => {
            m_original = m_result = new Vector3(0, 0, 0);
        };
    }

    void OnEnable()
    {
        m_inputAction?.Enable();
    }

    void OnDisable()
    {
        m_inputAction?.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateResult();
    }

    protected override void UpdateResult()
    {
        if (m_stickImage != null)
        {
            m_stickImage.transform.localPosition = new Vector2(m_result.x, m_result.y) * m_stickImageOffsetFactor;
            Vector3 angles = m_stickImage.transform.localEulerAngles;
            angles.z = m_result.z * 180;
            m_stickImage.transform.localEulerAngles = angles;
        }

        m_originalText.text = m_original.ToString();
        m_resultText.text = m_result.ToString();
    }
}
