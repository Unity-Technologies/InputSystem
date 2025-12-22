using UnityEngine;
using UnityEngine.InputSystem;

public class ResetBeatTrack : MonoBehaviour
{
    public GameManager gameManager;
    public InputActionReference resetAction;

    private void OnEnable()
    {
        if (resetAction != null)
        {
            resetAction.action.performed += ctx => gameManager.ResetSong();
            resetAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (resetAction != null)
        {
            resetAction.action.Disable();
        }
    }
}
