// This file mimics the C# class that Unity's Input Action Code Generator
// would produce for an .inputactions asset with a "gameplay" action map
// containing "use" and "move" actions. It exists purely so that
// GenerateCsApiFromActions.cs has a real MyPlayerControls/IGameplayActions
// pair to compile against for the "Generate C# Class" documentation sample.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace DocCodeSamples.Tests
{
    /// <summary>
    /// Example generated wrapper class for an .inputactions asset, as produced by
    /// Unity's Input Action Code Generator when "Generate C# Class" is enabled.
    /// </summary>
    public partial class MyPlayerControls : IInputActionCollection2, IDisposable
    {
        /// <summary>
        /// The underlying <see cref="InputActionAsset"/> wrapped by this class.
        /// </summary>
        public InputActionAsset asset { get; }

        /// <summary>
        /// Constructs the action asset and looks up its maps and actions.
        /// </summary>
        public MyPlayerControls()
        {
            asset = InputActionAsset.FromJson(@"{
    ""name"": ""MyPlayerControls"",
    ""maps"": [
        {
            ""name"": ""gameplay"",
            ""id"": ""d55be63c-61eb-47ef-92dd-eef1248d601e"",
            ""actions"": [
                {
                    ""name"": ""move"",
                    ""type"": ""Value"",
                    ""id"": ""8387a17d-aedd-4411-9931-6a855a8299fb"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""use"",
                    ""type"": ""Button"",
                    ""id"": ""b5f08480-c03b-4654-8475-9c94e8ccccaf"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""2c541328-ed00-4524-817c-97599bac7de5"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""41041dd1-570a-487c-856e-d58cfa06509a"",
                    ""path"": ""<Gamepad>/buttonNorth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""use"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
            // gameplay
            m_gameplay = asset.FindActionMap("gameplay", throwIfNotFound: true);
            m_gameplay_move = m_gameplay.FindAction("move", throwIfNotFound: true);
            m_gameplay_use = m_gameplay.FindAction("use", throwIfNotFound: true);
        }

        /// <summary>
        /// Destroys the underlying action asset.
        /// </summary>
        public void Dispose()
        {
            UnityEngine.Object.Destroy(asset);
        }

        /// <summary>
        /// The binding mask applied to the underlying action asset.
        /// </summary>
        public InputBinding? bindingMask
        {
            get => asset.bindingMask;
            set => asset.bindingMask = value;
        }

        /// <summary>
        /// The devices the underlying action asset is restricted to, if any.
        /// </summary>
        public ReadOnlyArray<InputDevice>? devices
        {
            get => asset.devices;
            set => asset.devices = value;
        }

        /// <summary>
        /// The control schemes defined on the underlying action asset.
        /// </summary>
        public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

        /// <summary>
        /// Checks whether <paramref name="action"/> belongs to this asset.
        /// </summary>
        /// <param name="action">Action to check.</param>
        /// <returns>True if the action belongs to this asset.</returns>
        public bool Contains(InputAction action)
        {
            return asset.Contains(action);
        }

        /// <summary>
        /// Returns an enumerator over all actions in the asset.
        /// </summary>
        /// <returns>An enumerator over all actions in the asset.</returns>
        public IEnumerator<InputAction> GetEnumerator()
        {
            return asset.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Enables all action maps in the asset.
        /// </summary>
        public void Enable()
        {
            asset.Enable();
        }

        /// <summary>
        /// Disables all action maps in the asset.
        /// </summary>
        public void Disable()
        {
            asset.Disable();
        }

        /// <summary>
        /// All bindings in the asset.
        /// </summary>
        public IEnumerable<InputBinding> bindings => asset.bindings;

        /// <summary>
        /// Finds an action by name or ID.
        /// </summary>
        /// <param name="actionNameOrId">Name or ID of the action to find.</param>
        /// <param name="throwIfNotFound">If true, throws instead of returning null when not found.</param>
        /// <returns>The found action, or null if not found and <paramref name="throwIfNotFound"/> is false.</returns>
        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
        {
            return asset.FindAction(actionNameOrId, throwIfNotFound);
        }

        /// <summary>
        /// Finds a binding that matches <paramref name="bindingMask"/>.
        /// </summary>
        /// <param name="bindingMask">Mask to match bindings against.</param>
        /// <param name="action">The action the found binding belongs to.</param>
        /// <returns>The index of the found binding, or -1 if not found.</returns>
        public int FindBinding(InputBinding bindingMask, out InputAction action)
        {
            return asset.FindBinding(bindingMask, out action);
        }

        // gameplay
        private readonly InputActionMap m_gameplay;
        private IGameplayActions m_GameplayActionsCallbackInterface;
        private readonly InputAction m_gameplay_move;
        private readonly InputAction m_gameplay_use;
        /// <summary>
        /// Accessor struct for the actions in the "gameplay" action map.
        /// </summary>
        public struct GameplayActions
        {
            private MyPlayerControls m_Wrapper;

            /// <summary>
            /// Constructs the accessor for the given <paramref name="wrapper"/>.
            /// </summary>
            /// <param name="wrapper">The <see cref="MyPlayerControls"/> instance to wrap.</param>
            public GameplayActions(MyPlayerControls wrapper) { m_Wrapper = wrapper; }

            /// <summary>
            /// The "move" action.
            /// </summary>
            public InputAction @move => m_Wrapper.m_gameplay_move;

            /// <summary>
            /// The "use" action.
            /// </summary>
            public InputAction @use => m_Wrapper.m_gameplay_use;

            /// <summary>
            /// Returns the underlying "gameplay" action map.
            /// </summary>
            /// <returns>The underlying action map.</returns>
            public InputActionMap Get() { return m_Wrapper.m_gameplay; }

            /// <summary>
            /// Enables the "gameplay" action map.
            /// </summary>
            public void Enable() { Get().Enable(); }

            /// <summary>
            /// Disables the "gameplay" action map.
            /// </summary>
            public void Disable() { Get().Disable(); }

            /// <summary>
            /// Whether the "gameplay" action map is currently enabled.
            /// </summary>
            public bool enabled => Get().enabled;

            /// <summary>
            /// Implicitly converts to the underlying action map.
            /// </summary>
            /// <param name="set">Accessor to convert.</param>
            /// <returns>The underlying "gameplay" action map.</returns>
            public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }

            /// <summary>
            /// Registers <paramref name="instance"/> to receive callbacks for the actions
            /// in the "gameplay" action map, replacing any previously registered instance.
            /// </summary>
            /// <param name="instance">Instance to register, or null to only unregister the current one.</param>
            public void SetCallbacks(IGameplayActions instance)
            {
                if (m_Wrapper.m_GameplayActionsCallbackInterface != null)
                {
                    @move.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMove;
                    @move.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMove;
                    @move.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnMove;
                    @use.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnUse;
                    @use.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnUse;
                    @use.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnUse;
                }
                m_Wrapper.m_GameplayActionsCallbackInterface = instance;
                if (instance != null)
                {
                    @move.started += instance.OnMove;
                    @move.performed += instance.OnMove;
                    @move.canceled += instance.OnMove;
                    @use.started += instance.OnUse;
                    @use.performed += instance.OnUse;
                    @use.canceled += instance.OnUse;
                }
            }
        }
        /// <summary>
        /// Accessor for the actions in the "gameplay" action map.
        /// </summary>
        public GameplayActions @gameplay => new GameplayActions(this);

        /// <summary>
        /// Callback interface for the actions in the "gameplay" action map.
        /// </summary>
        public interface IGameplayActions
        {
            /// <summary>
            /// Called when the "move" action is triggered.
            /// </summary>
            /// <param name="context">Context for the triggered action.</param>
            void OnMove(InputAction.CallbackContext context);

            /// <summary>
            /// Called when the "use" action is triggered.
            /// </summary>
            /// <param name="context">Context for the triggered action.</param>
            void OnUse(InputAction.CallbackContext context);
        }
    }
}
