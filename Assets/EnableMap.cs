using UnityEngine;
using UnityEngine.InputSystem;

public class EnableMap : MonoBehaviour
{
    void Update()
    {
        InputSystem.actions.FindActionMap("Gameplay").Enable();
    }
}
