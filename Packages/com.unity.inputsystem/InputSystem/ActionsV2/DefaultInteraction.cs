using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.ActionsV2
{
	public interface IInputInteraction<TValue> where TValue : struct
	{
        void Process(ref InputInteractionContext<TValue> interaction);
	}

    internal class DefaultInteraction<TValue> : IInputInteraction<TValue> where TValue : struct
    {
	    public void Process(ref InputInteractionContext<TValue> context)
	    {
            switch (context.action.phase)
            {
                case InputActionPhase.Waiting:
                {
                    if (context.action.isPassThrough)
                    {
	                    context.action.ChangePhaseOfAction(InputActionPhase.Performed);
                        break;
                    }

                    if (context.action.isButton)
                    {
	                    var actuation = context.magnitude;
                        if (actuation > 0)
                            context.action.ChangePhaseOfAction(InputActionPhase.Started);
                        
                        var threshold = context.control is ButtonControl button ? button.pressPointOrDefault : ButtonControl.s_GlobalDefaultButtonPressPoint;
                        if (actuation >= threshold)
                            context.action.ChangePhaseOfAction(InputActionPhase.Performed, InputActionPhase.Performed);
                    }
                    else
                    {
                        // Value-type action.
                        // Ignore if the control has not crossed its actuation threshold.
                        if (context.control.IsActuated())
                        {
                            ////REVIEW: Why is it we don't stay in performed but rather go back to started all the time?

                            // Go into started, then perform and then go back to started.
                            context.action.ChangePhaseOfAction(InputActionPhase.Started);
                            context.action.ChangePhaseOfAction(InputActionPhase.Performed, InputActionPhase.Started);
                        }
                    }

                    break;
                }

                case InputActionPhase.Started:
                {
                    if (context.action.isButton)
                    {
	                    var actuation = context.magnitude;
                        var threshold = context.control is ButtonControl button ? button.pressPointOrDefault : ButtonControl.s_GlobalDefaultButtonPressPoint;
                        if (actuation >= threshold)
                        {
                            // Button crossed press threshold. Perform.
                            context.action.ChangePhaseOfAction(InputActionPhase.Performed, InputActionPhase.Performed);
                        }
                        else if (Mathf.Approximately(actuation, 0))
                        {
                            // Button is no longer actuated. Never reached threshold to perform.
                            // Cancel.
                            context.action.ChangePhaseOfAction(InputActionPhase.Canceled);
                        }
                    }
                    else
                    {
                        if (!context.control.IsActuated())
                        {
                            // Control went back to below actuation threshold. Cancel interaction.
                            context.action.ChangePhaseOfAction(InputActionPhase.Canceled);
                        }
                        else
                        {
                            // Control changed value above magnitude threshold. Perform and remain started.
                            context.action.ChangePhaseOfAction(InputActionPhase.Performed, InputActionPhase.Started);
                        }
                    }
                    break;
                }

                case InputActionPhase.Performed:
                {
                    if (context.action.isButton)
                    {
	                    var actuation = context.magnitude;
                        var pressPoint = context.control is ButtonControl button ? button.pressPointOrDefault : ButtonControl.s_GlobalDefaultButtonPressPoint;
                        if (Mathf.Approximately(0f, actuation))
                        {
                            context.action.ChangePhaseOfAction(InputActionPhase.Canceled);
                        }
                        else
                        {
                            var threshold = pressPoint * ButtonControl.s_GlobalDefaultButtonReleaseThreshold;
                            if (actuation <= threshold)
                            {
                                // Button released to below threshold but not fully released.
                                context.action.ChangePhaseOfAction(InputActionPhase.Started);
                            }
                        }
                    }
                    else if (context.action.isPassThrough)
                    {
                        ////REVIEW: even for pass-through actions, shouldn't we cancel when seeing a default value?
                        context.action.ChangePhaseOfAction(InputActionPhase.Performed, InputActionPhase.Performed);
                    }
                    else
                    {
                        Debug.Assert(false, "Value type actions should not be left in performed state");
                    }
                    break;
                }

                default:
                    Debug.Assert(false, "Should not get here");
                    break;
            }
	    }

	    public void Reset()
	    {
	    }
    }

    public struct InputInteractionEvent<TValue> where TValue:struct
    {
        
    }

    public class MultiTapInteraction<TValue> : IInputInteraction<TValue> where TValue : struct
    {
	    public event Action DoubleTap;

	    public MultiTapInteraction(InputAction<TValue> action)
	    {
		    action.OnInputEvent((InputInteractionEvent<TValue> evt) =>
		    {
			    if (evt.Type == InputEventType.Began)
			    {

			    }
			    else if(evt.Type == InputEventType.Ended)
			    {

			    }
		    });
	    }

	    public void Process(ref InputInteractionContext<TValue> interaction)
	    {
		    
	    }
    }
}
