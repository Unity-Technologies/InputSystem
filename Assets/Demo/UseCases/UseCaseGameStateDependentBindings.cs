using System;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UseCases;

public class UseCaseGameStateDependentBindings : UseCase
{
    // TODO Create an observable enum-based game state.
    
    public ScriptableInputBinding<Vector2> move;
    private IDisposable m_MoveSubscription;
    
    private void OnEnable()
    {
        // TODO This should also work:
        // var temp = move.Conditional(gamestate, GameState.Playing, Comparison.EqualTo);
        // temp.Subscribe(x => moveDirection = v);
        
        // TODO A problem arises from node not being able to reconstruct dependency chain since it doesn't have knowledge of dependency type. This could be solved with node having knowledge of struct type, but this increases number of Node types if nested.
        
        m_MoveSubscription = move.Subscribe(v => moveDirection = v); // TODO Make binding conditional on game state. We would like it to either dispose the full chain, or mute the first non-shared node to minimize overhead. Ideally we would like to Dispose() and Subscribe() again when condition changes. But that requires knowledge of full chain.
    }

    private void OnDisable()
    {
        m_MoveSubscription.Dispose();
    }
}
