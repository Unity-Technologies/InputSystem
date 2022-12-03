using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[DisplayName("Check")]
public class CheckGestureInteraction : IInputInteraction<Vector2>
{
    public float duration;

    private Vector2 firstDirection = new Vector2(1f, -1f);
    public float minLengthFirstDirection = 0.2f;
    public float maxLengthFirstDirection = 1f;
    private float lengthFirstMovement;

    private Vector2 secondDirection = new Vector2(1f, 1f);
    public float minLengthSecondDirection = 0.3f;
    public float maxLengthSecondDirection = 1.5f;
    private float lengthSecondMovement;

    private bool isFirstMoveComplete;
    private bool isSecondMoveComplete => lengthSecondMovement >= minLengthSecondDirection && lengthSecondMovement <= maxLengthSecondDirection;

    private bool isGestureComplete => isFirstMoveComplete && isSecondMoveComplete;

    private Vector2 lastInputPosition;

    public void Process(ref InputInteractionContext context)
    {
        var controlValue = context.control;
        if (!(controlValue is Vector2Control value))
        {
            Debug.LogWarning("Received input type: "+context.control.ReadValueAsObject().GetType()+" does not match the expected Input type of Vector2");
            return;
        }
        if (context.timerHasExpired)
        {
            context.Canceled();
            return;
        }

        switch (context.phase)
        {
            case InputActionPhase.Waiting:
                context.Started();
                context.SetTimeout(duration);
                lastInputPosition = value.ReadValue();
                break;
            case InputActionPhase.Started:
                if (!IsInteractionInProgress(value.ReadValue()))
                {
                    context.Canceled();
                    return;
                }
                lastInputPosition = value.ReadValue();
                if (isGestureComplete)
                    context.PerformedAndStayPerformed();
                
                break;
        }
    }

    private bool IsInteractionInProgress(Vector2 inputPosition)
    {
        if (!isFirstMoveComplete && IsInFirstMoveDirection(inputPosition))
            return true;
        
        if (isFirstMoveComplete && IsInSecondMoveDirection(inputPosition))
            return true;
        
        return false;
    }

    private bool IsInFirstMoveDirection(Vector2 inputPosition)
    {
        var isMoveInFirstDirection = Vector2.Angle(inputPosition-lastInputPosition, firstDirection) < 10f;
        if (isMoveInFirstDirection)
        {
            lengthFirstMovement += Vector2.Distance(lastInputPosition, inputPosition);
            return true;
        }
        var firstMoveLongEnough = lengthFirstMovement >= minLengthFirstDirection && lengthFirstMovement <= maxLengthFirstDirection;
        isFirstMoveComplete = firstMoveLongEnough;
        return false;
    }

    private bool IsInSecondMoveDirection(Vector2 inputPosition)
    {
        var isMoveInSecondDirection = Vector2.Angle(inputPosition-lastInputPosition, secondDirection) < 10f;
        if (isMoveInSecondDirection)
            lengthSecondMovement += Vector2.Distance(lastInputPosition, inputPosition);
            
        return isMoveInSecondDirection;
    }

    public void Reset()
    {
        Debug.Log("do");
    }
}
