using System.Linq;
using ISX;
using UnityEngine;

public class GamepadInputFromLegacy : MonoBehaviour
{
    private Gamepad m_Gamepad;
    
    public void Start()
    {
        var gamepad = InputSystem.devices.FirstOrDefault(x => x is Gamepad);
        if (gamepad == null)
            m_Gamepad = (Gamepad) InputSystem.AddDevice("Gamepad");
    }
    
    public void Update()
    {
    }
}

