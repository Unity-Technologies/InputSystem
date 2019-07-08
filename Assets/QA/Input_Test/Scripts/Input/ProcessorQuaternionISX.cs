using UnityEngine;
using UnityEngine.InputSystem;

public class ProcessorQuaternionISX : ProcessorISX
{
    private Quaternion m_original = new Quaternion(0, 0, 0, 0);
    private Quaternion m_result = new Quaternion(0, 0, 0, 0);

    // Start is called before the first frame update
    void Start()
    {
        m_inputAction.Rename(gameObject.name);
        m_inputAction.performed += ctx => {
            InputControl<Quaternion> control = ctx.control as InputControl<Quaternion>;
            m_original = control.ReadValue();
            m_result = ctx.ReadValue<Quaternion>();
        };
        m_inputAction.canceled += ctx => {
            m_original = m_result = new Quaternion(0, 0, 0, 0);
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
        m_originalText.text = m_original.ToString();
        m_resultText.text = m_result.ToString();
    }
}
