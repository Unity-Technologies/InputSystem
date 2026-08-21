namespace DocCodeSamples.Tests
{
    #region interactions
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.Interactions;

    public class InteractionsExampleScript : MonoBehaviour
    {
        InputAction jumpAction;

        private void Start()
        {
            jumpAction = InputSystem.actions.FindAction("Jump");


            jumpAction.started += context =>
            {
                if (context.interaction is SlowTapInteraction)
                {
                    // Show "charging" UI
                }
            };

            jumpAction.performed += context =>
            {
                if (context.interaction is SlowTapInteraction)
                {
                    // call "charged jump" code
                }
                else
                {
                    // call "regular jump" code
                };
            };

            jumpAction.canceled += context =>
            {
                // Hide "charging" UI
            };

        }
    }
    #endregion

    class ExampleScript2 : MonoBehaviour
    {
        public PlayerInput playerInput;

        private void Start()
        {
            #region timeout
            // Returns a value between 0 (inclusive) and 1 (inclusive).
            var warpActionCompletion = playerInput.actions["warp"].GetTimeoutCompletionPercentage();
            #endregion

            #region interactionactions
            var Action = new InputAction(interactions: "tap(duration=0.8)");
            #endregion
        }

        private void ConfigureBindingInteractions()
        {
            #region interactionbindings
            var Action = new InputAction();
            Action.AddBinding("<Gamepad>/leftStick").WithInteractions("tap(duration=0.8)");
            #endregion
        }
    }

    #region custominteraction
    // Interaction which performs when you quickly move an
    // axis all the way from extreme to the other.
    public class MyExampleInteraction : IInputInteraction
    {
        public float duration = 0.2f;

        public void Process(ref InputInteractionContext context)
        {
            if (context.timerHasExpired)
            {
                context.Canceled();
                return;
            }

            switch (context.phase)
            {
                case InputActionPhase.Waiting:
                    if (context.ReadValue<float>() == 1)
                    {
                        context.Started();
                        context.SetTimeout(duration);
                    }
                    break;

                case InputActionPhase.Started:
                    if (context.ReadValue<float>() == -1)
                        context.Performed();
                    break;
            }
        }

        // Unlike processors, Interactions can be stateful, meaning that you can keep a
        // local state that changes over time as input is received. The system might
        // invoke the Reset() method to ask Interactions to reset to the local state
        // at certain points.
        public void Reset()
        {
        }

        void Start()
        {
            #region registerinteraction
            InputSystem.RegisterInteraction<MyExampleInteraction>();
            #endregion

            #region useinteraction
            var Action = new InputAction(interactions: "MyExample(duration=0.5)");
            #endregion
        }
    }
    #endregion
}


