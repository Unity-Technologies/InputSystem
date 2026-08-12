using UnityEngine;
#region using
using UnityEngine.InputSystem;
#endregion

namespace DocCodeSamples.Tests
{
    internal class UsingActionsWorkflowExamples : MonoBehaviour
    {
        #region InputAction_variables
        InputAction moveAction;
        InputAction jumpAction;
        #endregion

        private void Start() {
        #region FindAction
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        #endregion
        }

        private void Update() {
        #region ReadActionValues
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        bool jumpValue = jumpAction.IsPressed();
        #endregion
        }
    }
}
