using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine;

// Tapping event stream.
public class SimpleController_UsingEvents : MonoBehaviour
{
    public float moveSpeed;
    public float rotateSpeed;

    private Vector2 m_Move;
    private Vector2 m_Look;

    private Vector2 m_Rotation;

    public void OnEnable()
    {
        InputSystem.onEvent +=
            eventPtr =>
        {
            var gamepad = InputSystem.TryGetDeviceById(eventPtr.deviceId) as Gamepad;
            if (gamepad == null)
                return;

            if (eventPtr.IsA<StateEvent>())
            {
                var leftStick = gamepad.leftStick.ReadValueFrom(eventPtr);
                var rightStick = gamepad.rightStick.ReadValueFrom(eventPtr);

                m_Move = leftStick;
                m_Look = rightStick;
            }
        };
    }

    public void Update()
    {
        Move(m_Move);
        Look(m_Look);
    }

    private void Move(Vector2 direction)
    {
        var scaledMoveSpeed = moveSpeed * Time.deltaTime;
        var move = transform.TransformDirection(direction.x, 0, direction.y);
        transform.localPosition += move * scaledMoveSpeed;
    }

    private void Look(Vector2 rotate)
    {
        var scaledRotateSpeed = rotateSpeed * Time.deltaTime;
        m_Rotation.y += rotate.x * scaledRotateSpeed;
        m_Rotation.x = Mathf.Clamp(m_Rotation.x - rotate.y * scaledRotateSpeed, -89, 89);
        transform.localEulerAngles = m_Rotation;
    }
}
