#if UNITY_INPUT_SYSTEM_ENABLE_GLOBAL_ACTIONS_API

using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HighLevel;
using UnityEngine.InputSystem.Interactions;
using Input = UnityEngine.InputSystem.HighLevel.Input;

public class TestGlobalActions : MonoBehaviour
{
    void Start()
    {
        // var action = new InputAction();
        // action.AddCompositeBinding("2DVector")
        //  .With("Up", "<Keyboard>/w")
        //  .With("Down", "<Keyboard>/s")
        //  .With("Left", "<Keyboard>/a")
        //  .With("Right", "<Keyboard>/d");
        //
        // action.Enable();
        //
        // action.performed += ctx =>
        // {
        //  Debug.Log($"Binding index = {ctx.action.activeBindingIndex}");
        // };

        var input = new Input<float>(new InputAction());
        input.GetInteraction<HoldInteraction>(BindingIndex.None).SetInteractionParameter((HoldInteraction h) => h.duration, 1);
    }

    public class FireInput : Input<Vector2>
    {
        // if there are multiple similar interactions on multiple bindings...
        public Interaction<HoldInteraction, Vector2> holdOnDpad;
        public Interaction<HoldInteraction, Vector2> holdOnLeftstick;

        // doesn't really matter what binding this is on. If it performs, is that all we need to know?
        public Interaction<MultiTapInteraction, Vector2> multiTap;

        public FireInput(InputAction action) : base(action)
        {
            holdOnDpad = GetInteraction<HoldInteraction>(new BindingIndex(action, 0, BindingIndex.IndexType.SkipCompositeParts));
            // holdOnLeftstick = GetInteraction<HoldInteraction>(1);
        }
    }

    void Update()
    {
        //       if (InputActions.FPS.jump)
        // {
        //  Debug.Log($"Jump");
        // }
    }
}

// public struct HoldInteractionAccessor<TActionType> where TActionType : struct
// {
//  private readonly Interaction<HoldInteraction, TActionType> m_Interaction;
//
//  public bool wasPerformedThisFrame => m_Interaction.wasPerformedThisFrame;
//  public bool wasStartedThisFrame => m_Interaction.wasStartedThisFrame;
//  public bool wasCanceledThisFrame => m_Interaction.wasCanceledThisFrame;
//
//  public float duration
//  {
//      get => m_Interaction.m_Input.GetInteractionParameter<HoldInteraction, float>(m_Interaction.m_BindingIndex, x => x.duration);
//      set => m_Interaction.m_Input.SetInteractionParameter<HoldInteraction, float>(m_Interaction.m_BindingIndex, x => x.duration, value);
//  }
//
//  public float pressPoint
//  {
//      get => m_Interaction.m_Input.GetInteractionParameter<HoldInteraction, float>(m_Interaction.m_BindingIndex, x => x.pressPoint);
//      set => m_Interaction.m_Input.SetInteractionParameter<HoldInteraction, float>(m_Interaction.m_BindingIndex, x => x.duration, value);
//  }
//
//  public HoldInteractionAccessor(Interaction<HoldInteraction, TActionType> interaction)
//  {
//      m_Interaction = interaction;
//  }
// }
#endif
