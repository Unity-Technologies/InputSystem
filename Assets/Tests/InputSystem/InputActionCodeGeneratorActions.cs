//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.8.1
//     from Assets/Tests/InputSystem/InputActionCodeGeneratorActions.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine;

public partial class @InputActionCodeGeneratorActions: IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @InputActionCodeGeneratorActions()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""InputActionCodeGeneratorActions"",
    ""maps"": [
        {
            ""name"": ""gameplay"",
            ""id"": ""aa98ff33-553d-437c-ba63-5185c02a9073"",
            ""actions"": [
                {
                    ""name"": ""action1"",
                    ""type"": ""Button"",
                    ""id"": ""8a0f5356-7f9b-43fc-86cb-9059960b3b66"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""action2"",
                    ""type"": ""Button"",
                    ""id"": ""ef3f7fb0-f559-470a-a691-220540d5d27b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""f52d4a3b-ecab-439b-8741-183efb24f71f"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""action1"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""07c9ecc4-3c52-44fe-9148-4b66948e9210"",
                    ""path"": ""<Keyboard>/enter"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""action2"",
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
        m_gameplay_action1 = m_gameplay.FindAction("action1", throwIfNotFound: true);
        m_gameplay_action2 = m_gameplay.FindAction("action2", throwIfNotFound: true);
    }

    ~@InputActionCodeGeneratorActions()
    {
        Debug.Assert(!m_gameplay.enabled, "This will cause a leak and performance issues, InputActionCodeGeneratorActions.gameplay.Disable() has not been called.");
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // gameplay
    private readonly InputActionMap m_gameplay;
    private List<IGameplayActions> m_GameplayActionsCallbackInterfaces = new List<IGameplayActions>();
    private readonly InputAction m_gameplay_action1;
    private readonly InputAction m_gameplay_action2;
    public struct GameplayActions
    {
        private @InputActionCodeGeneratorActions m_Wrapper;
        public GameplayActions(@InputActionCodeGeneratorActions wrapper) { m_Wrapper = wrapper; }
        public InputAction @action1 => m_Wrapper.m_gameplay_action1;
        public InputAction @action2 => m_Wrapper.m_gameplay_action2;
        public InputActionMap Get() { return m_Wrapper.m_gameplay; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }
        public void AddCallbacks(IGameplayActions instance)
        {
            if (instance == null || m_Wrapper.m_GameplayActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_GameplayActionsCallbackInterfaces.Add(instance);
            @action1.started += instance.OnAction1;
            @action1.performed += instance.OnAction1;
            @action1.canceled += instance.OnAction1;
            @action2.started += instance.OnAction2;
            @action2.performed += instance.OnAction2;
            @action2.canceled += instance.OnAction2;
        }

        private void UnregisterCallbacks(IGameplayActions instance)
        {
            @action1.started -= instance.OnAction1;
            @action1.performed -= instance.OnAction1;
            @action1.canceled -= instance.OnAction1;
            @action2.started -= instance.OnAction2;
            @action2.performed -= instance.OnAction2;
            @action2.canceled -= instance.OnAction2;
        }

        public void RemoveCallbacks(IGameplayActions instance)
        {
            if (m_Wrapper.m_GameplayActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(IGameplayActions instance)
        {
            foreach (var item in m_Wrapper.m_GameplayActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_GameplayActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public GameplayActions @gameplay => new GameplayActions(this);
    public interface IGameplayActions
    {
        void OnAction1(InputAction.CallbackContext context);
        void OnAction2(InputAction.CallbackContext context);
    }
}
