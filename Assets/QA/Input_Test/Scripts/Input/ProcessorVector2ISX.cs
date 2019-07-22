using UnityEngine;
using UnityEngine.InputSystem;

public class ProcessorVector2ISX : ProcessorISX
{
    private Vector2 m_original = new Vector2(0, 0);
    private Vector2 m_result = new Vector2(0, 0);

    // Start is called before the first frame update
    void Start()
    {
        m_inputAction.Rename(gameObject.name);
        m_inputAction.performed += ctx => {
            InputControl<Vector2> control = ctx.control as InputControl<Vector2>;
            m_original = control.ReadValue();
            m_result = ctx.ReadValue<Vector2>();
        };
        m_inputAction.canceled += ctx => {
            m_original = m_result = new Vector2(0, 0);
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
            m_stickImage.transform.localPosition = m_result * m_stickImageOffsetFactor;
        m_originalText.text = m_original.ToString();
        m_resultText.text = m_result.ToString();
    }
}
