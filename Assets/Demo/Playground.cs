using System;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;

namespace UseCases
{
    public class Playground : UseCase
    {
        public ScriptableInputBinding<Vector2> move;
        public ScriptableInputBinding<InputEvent> jump;
        
        public void OnEnable()
        {
            move.Subscribe(x => moveDirection = x);
            jump.Subscribe(evt => Debug.Log("Jump"));
        }

        public void OnDisable()
        {
            
        }
    }
}
