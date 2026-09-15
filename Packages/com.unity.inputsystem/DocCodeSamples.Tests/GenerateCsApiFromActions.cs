namespace DocCodeSamples.Tests
{
    #region generate-cs-api
    using UnityEngine;
    using UnityEngine.InputSystem;

    // IGameplayActions is an interface generated from the newly added "gameplay"
    // action map, triggered by the "Generate Interfaces" checkbox. Note that if
    // you change the default values for the action map, the name of the interface
    // will be different.

    /// <summary>
    /// Example script showing how to consume a C# class generated from an
    /// .inputactions asset via the "Generate C# Class" option.
    /// </summary>
    public class MyPlayerScript : MonoBehaviour, MyPlayerControls.IGameplayActions
    {
        // MyPlayerControls is the C# class that Unity generated.
        // It encapsulates the data from the .inputactions asset we created
        // and automatically looks up all the maps and actions for us.
        MyPlayerControls controls;

        /// <summary>
        /// Called by Unity when the component is enabled.
        /// </summary>
        public void OnEnable()
        {
            if (controls == null)
            {
                controls = new MyPlayerControls();
                // Tell the "gameplay" action map that we want to be
                // notified when actions get triggered.
                controls.gameplay.SetCallbacks(this);
            }
            controls.gameplay.Enable();
        }

        /// <summary>
        /// Called by Unity when the component is disabled.
        /// </summary>
        public void OnDisable()
        {
            controls.gameplay.Disable();
        }

        /// <summary>
        /// Called when the "use" action is triggered.
        /// </summary>
        /// <param name="context">Context for the triggered action.</param>
        public void OnUse(InputAction.CallbackContext context)
        {
            // 'Use' code here.
        }

        /// <summary>
        /// Called when the "move" action is triggered.
        /// </summary>
        /// <param name="context">Context for the triggered action.</param>
        public void OnMove(InputAction.CallbackContext context)
        {
            // 'Move' code here.
        }
    }
    #endregion
}
