namespace DocCodeSamples.Tests
{
    #region generate-cs-api
    using UnityEngine;
    using UnityEngine.InputSystem;

    // IGameplayActions is an interface generated from the newly added "gameplay"
    // action map, triggered by the "Generate Interfaces" checkbox. Note that if
    // you change the default values for the action map, the name of the interface
    // will be different.

    public class MyPlayerScript : MonoBehaviour, IGameplayActions
    {
        // MyPlayerControls is the C# class that Unity generated.
        // It encapsulates the data from the .inputactions asset we created
        // and automatically looks up all the maps and actions for us.
        MyPlayerControls controls;

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

        public void OnDisable()
        {
            controls.gameplay.Disable();
        }

        public void OnUse(InputAction.CallbackContext context)
        {
            // 'Use' code here.
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            // 'Move' code here.
        }

    }
    #endregion
}