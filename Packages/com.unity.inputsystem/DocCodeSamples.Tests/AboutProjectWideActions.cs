using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Example script demonstrating how to look up a project-wide action.
/// </summary>
public class AboutProjectWideActions : MonoBehaviour
{
    void Start()
    {
        #region about-project-wide-actions
        InputSystem.actions.FindAction("Move");
        #endregion
    }
}
