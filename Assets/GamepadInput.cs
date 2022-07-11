using UnityEngine;
using UnityEngine.InputSystem;
using Input = UnityEngine.InputSystem.Input;

public class GamepadInput : MonoBehaviour
{
	public void Update()
	{
		var leftStick = Input.GetAxis(GamepadAxis.LeftStick);
		var rightStick = Input.GetAxis(GamepadAxis.RightStick);
	}
}