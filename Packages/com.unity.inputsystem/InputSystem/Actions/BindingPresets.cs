using System;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace UnityEngine.InputSystem
{
    public static partial class BindingPresets
    {
        public static partial class ByGenre
        {
            public static partial class Platformer2D
            {
                // Corresponds to default control bindings for the following games:
                // "Ori and the blind forest", https://www.alilfoxz.com/ori-game-play-controls/
                
                public static readonly Action<InputAction> Move = (action) =>
                    {
                        if (action.type != InputActionType.Value)
                            throw new ArgumentException("Invalid type");
                        // TODO Check type    
                        
                        action.AddCompositeBinding("2DVector")
                            .With("Up", "<Keyboard>/upArrow")
                            .With("Down", "<Keyboard>/downArrow")
                            .With("Left", "<Keyboard>/leftArrow")
                            .With("Right", "<Keyboard>/rightArrow");
                        
                        action.AddBinding("<Gamepad>/leftStick");
                    };
                
                public static readonly Action<InputAction> Jump = (action) =>
                    {
                        // TODO Restrict to button
                        
                        action.AddBinding(new InputBinding("<Keyboard>/space"));
                        
                        action.AddBinding(new InputBinding(path: "<Gamepad>/buttonSouth"));
                    };
            }

            public static partial class FirstPersonShooter
            {
                // Corresponds to default control bindings for the following games:
                // "Doom Eternal", see https://www.shacknews.com/article/117015/doom-eternal-controls-and-keybindings
                // "Call of Duty: Modern Warfare", see: https://blog.activision.com/call-of-duty/2019-10/Getting-Started-in-Modern-Warfare-Controls-and-Settings
                
                public static readonly Action<InputAction> Move = (action) =>
                {
                    // TODO Restrict to Vector2
                    
                    action.AddCompositeBinding("2DVector")
                        .With("Up", "<Keyboard>/w")
                        .With("Down", "<Keyboard>/s")
                        .With("Left", "<Keyboard>/a")
                        .With("Right", "<Keyboard>/d");
                    action.AddBinding("<Gamepad>/leftStick");
                };

                public static readonly Action<InputAction> jump = (action) =>
                {
                    // TODO Restrict to button
                    
                    action.AddBinding(new InputBinding("<Keyboard>/space"));
                    action.AddBinding(new InputBinding(path: "<Gamepad>/buttonSouth"));
                };
            }
        }
    }
}
