using UnityEngine;
using UnityEngine.InputSystem;

public class CreateControls_Test : MonoBehaviour
{
    private Gamepad m_Gamepad;
    void Start()
    {
        m_Gamepad = InputSystem.AddDevice<Gamepad>();
    }

    private float val;
    void Update()
    {
        val = Mathf.Min(1f, val + Time.deltaTime * 10f);
        if (val.Equals(1f)) val = 0f;
        InputSystem.QueueDeltaStateEvent(m_Gamepad.leftStick, new Vector2(0f, val));
    }
}
