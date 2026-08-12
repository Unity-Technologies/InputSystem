using UnityEngine;
using UnityEngine.InputSystem;

public class AboutProjectWideActions : MonoBehaviour
{
    void Start()
    {
        #region about-project-wide-actions
        InputSystem.actions.FindAction("Move");
        #endregion
    }
}
