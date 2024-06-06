using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;
using UnityEngine;

namespace UnityEngine.InputSystem.Interactions
{
    public class SwipeInteraction : IInputInteraction
    {
        [RuntimeInitializeOnLoadMethod]
        public static void Init()
        {
            Debug.Log("SwipeInteraction Init");
            InputSystem.RegisterInteraction<SwipeInteraction>();
        }

        public void Process(ref InputInteractionContext context)
        {
            Debug.Log("Swipe.Process");
        }

        public void Reset()
        {
        }
    }
}
