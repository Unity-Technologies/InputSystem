namespace DocCodeSamples.Tests
{
    #region declaration
    using UnityEngine;
    using UnityEngine.InputSystem;

    /// <summary>
    /// Example script exposing serialized action references.
    /// </summary>
    public class ExampleScript : MonoBehaviour
    {
        /// <summary>
        /// Reference to the "move" action.
        /// </summary>
        public InputAction move;

        /// <summary>
        /// Reference to the "jump" action.
        /// </summary>
        public InputAction jump;
    }
    #endregion

    class ConfigureInputfromCode : MonoBehaviour
    {
        const string json = @"
        {
            ""maps"" : [
                {
                    ""name"" : ""gameplay"",
                    ""actions"" : [
                        { ""name"" : ""fire"", ""type"" : ""button"" }
                    ]
                }
            ]
        }";

        void ConfigureFromJsonExample()
        {
            #region configurefromjson
            // Load a set of action maps from JSON.
            var maps = InputActionMap.FromJson(json);

            // Load an entire InputActionAsset from JSON.
            var asset = InputActionAsset.FromJson(json);
            #endregion
        }

        void Start()
        {
            #region configurefromcode
            {
                // Create free-standing actions.
                var lookAction = new InputAction("look", binding: "<Gamepad>/leftStick");
                var moveAction = new InputAction("move", binding: "<Gamepad>/rightStick");

                moveAction.AddCompositeBinding("1DAxis")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
            }

            {
                // Create an action map with actions.
                var map = new InputActionMap("Gameplay");
                var lookAction = map.AddAction("look");
                lookAction.AddBinding("<Gamepad>/leftStick");
            }

            {
                // Create an action asset.
                var asset = ScriptableObject.CreateInstance<InputActionAsset>();
                var gameplayMap = new InputActionMap("gameplay");
                asset.AddActionMap(gameplayMap);
                var lookAction = gameplayMap.AddAction("look", binding: "<Gamepad>/leftStick");
            }
            #endregion
        }
    }
}
