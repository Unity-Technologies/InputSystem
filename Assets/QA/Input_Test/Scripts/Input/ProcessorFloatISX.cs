using UnityEngine;
using UnityEngine.InputSystem;

public class ProcessorFloatISX : ProcessorISX
{
    private float m_original = 0f;
    private float m_result = 0f;

    // Start is called before the first frame update
    void Start()
    {
        m_inputAction.Rename(gameObject.name);
        m_inputAction.performed += ctx => {
            InputControl<float> control = ctx.control as InputControl<float>;
            m_original = control.ReadValue();
            m_result = ctx.ReadValue<float>();
        };
        m_inputAction.canceled += ctx => {
            m_original = m_result = 0f;
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
            m_stickImage.transform.localPosition = new Vector2(m_result * m_stickImageOffsetFactor, 0);
        m_originalText.text = m_original.ToString("F2");
        m_resultText.text = m_result.ToString("F2");
    }
}
