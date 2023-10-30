using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/*public interface IBindingPreset
{
    void Apply(InputAction action);

    //IReadOnlyList<InputBinding> Bindings();
}*/

// DOCUMENT: BINDING PRESET SHOULD NOT BE AN OBJECT OR SERIALIZED SINCE IT SHOULD NOT BE ALLOWED TO CHANGE. IT MUST BE INACCESSABLE CODE.
/// <summary>
/// A binding preset that may be stored outside a GameObject.
/// </summary>
/*[CreateAssetMenu()]
public class BindingPreset : ScriptableObject, IBindingPreset
{
    /// <summary>
    /// Set of bindings.
    /// </summary>
    [SerializeField] public InputBinding[] bindings = {};

    /// <summary>
    /// Returns a read-only representation of the associated bindings.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<InputBinding> Bindings()
    {
        return bindings;
    }

    public void OnEnable()
    {
        Debug.Log("Binding Preset OnEnable");
    }

    public void Apply(InputAction action)
    {
        action.AddBinding("<Keyboard>/space"); // TODO FIX
    }
}*/

/*
public struct BindingPresetFactory : IBindingPreset
{
    public void Apply(InputAction action)
    {
        foreach (var binding in Bindings())
            action.AddBinding(binding);
    }

    public IReadOnlyList<InputBinding> Bindings()
    {
        return new InputBinding[]
        {
            new("<Keyboard>/space")
        };
    }
}*/

// https://forum.unity.com/threads/serialized-interface-fields.1238785/

[Serializable]
struct BindingPreset
{
    public BindingPreset(string name, Action<InputAction> applyPreset)
    {
        m_Name = name;
        m_ApplyPreset = applyPreset;
    }

    public string Name => m_Name;
    
    public void Apply(InputAction action)
    {
        m_ApplyPreset(action);
    }
    
    [SerializeField] private readonly Action<InputAction> m_ApplyPreset;
    [SerializeField] private readonly string m_Name;
}



namespace BindingPresets
{
    static partial class Platformer2D
    {
        public static readonly Action<InputAction> jump = (action) =>
            {
                action.AddBinding(new InputBinding("<Keyboard>/space"));
            };
        /*public static readonly BindingPreset jump = new BindingPreset(
            "Platformer2D.Jump",
            (action) =>
            {
                action.AddBinding(new InputBinding("<Keyboard>/space"));
            });*/
    }
}