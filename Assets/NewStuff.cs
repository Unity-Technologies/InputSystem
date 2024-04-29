using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[Serializable]
public class InputBindingObject<T> : ScriptableObject
{
    [SerializeField] public T x;

    public void Awake()
    {
        Debug.Log("InputBindingObject: Awake");
    }

    public void OnDestroy()
    {
        Debug.Log("InputBindingObject: Destroy");
    }
}

public interface IInputBinding
{
    
}

public interface IInputBinding<T> : IInputBinding
{
    public bool Bind(InputAction action);
}

public class MyScript : MonoBehaviour
{
    [SerializeField] private InputAction move;

    public void Awake()
    {
        if (move.bindings.Count == 0)
        {
            move.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");    
        }
    }
    
    public void OnEnable()
    {
        move.Enable();
    }

    void Update()
    {
        transform.Translate(move.ReadValue<Vector2>());
    }
}

public static partial class Presets
{
    public static IInputBinding<Vector2> GenericMove = new GenericMoveInputBinding();
    
    private struct GenericMoveInputBinding : IInputBinding<Vector2>
    {
        public bool Bind(InputAction action)
        {
            // We do not want to apply a preset to an action that is already bound
            if (action.bindings.Count != 0)
                return false;
            
            action.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            return true;
        }
    }
}

public struct Input<T>
{
    private InputAction action;
    
    public Input(IInputBinding<T> defaultBinding, IInputBinding<T> binding = null)
    {
        action = new InputAction();
        if (binding == null)
            defaultBinding.Bind(action);
        else
            binding.Bind(action);
    }

    public void Enable()
    {
        action.Enable();
    }

    public void Disable()
    {
        action.Disable();
    }
}

public class OfTypeAttribute : PropertyAttribute
{
    public Type type;
 
    public OfTypeAttribute(Type type)
    {
        this.type = type;
    }
}

[CustomPropertyDrawer(typeof(IInputBinding), useForChildren: true)]
public class InputBindingPropertyDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        if (property.propertyType != SerializedPropertyType.ObjectReference
            && property.propertyType != SerializedPropertyType.ExposedReference)
        {
            throw new System.ArgumentException("This attribute is not supported on properties of this property type.", nameof(property.propertyType));
        }
        //var ofType = attribute as OfTypeAttribute;
        var objectField = new ObjectField(property.displayName);
        objectField.AddToClassList(ObjectField.alignedFieldUssClassName);
        objectField.BindProperty(property);
        //objectField.objectType = ofType.type;
        objectField.RegisterValueChangedCallback(changed =>
        {
            /*Component component;
            if (IsValid(changed.newValue))
            {
                return;
            }
            else if (changed.newValue is GameObject gameObject
                     && (component = gameObject.GetComponents<Component>().FirstOrDefault(component => IsValid(component))))
            {
                objectField.SetValueWithoutNotify(component);
                return;
            }
            else if (changed.newValue)
            {
                objectField.SetValueWithoutNotify(null);
            }
            bool IsValid(Object obj) => obj && ofType.type.IsAssignableFrom(obj.GetType());*/
        });
        return objectField;
    }
}

/*[CustomPropertyDrawer(typeof(Input<>), useForChildren: true)]
public class InputPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        base.OnGUI(position, property, label);
    }
}*/

/// <summary>
/// Attribute that require implementation of the provided interface.
/// </summary>
public class RequireInterfaceAttribute : PropertyAttribute
{
    // Interface type.
    public System.Type requiredType { get; private set; }
    /// <summary>
    /// Requiring implementation of the <see cref="T:RequireInterfaceAttribute"/> interface.
    /// </summary>
    /// <param name="type">Interface type.</param>
    public RequireInterfaceAttribute(System.Type type)
    {
        this.requiredType = type;
    }
}

public class NewStuff : MonoBehaviour
{
    // https://forum.unity.com/threads/serialized-interface-fields.1238785/
    // https://forum.unity.com/threads/reference-interfaces-in-editor-workaround.1347686/
    // https://www.patrykgalach.com/2020/01/27/assigning-interface-in-unity-inspector/
    
    [SerializeField]
    public InputBindingAsset moveBinding;
    
    [SerializeField] 
    public IInputBinding<Vector2> m_MoveInputBinding;
    //[SerializeField] public BindingObject m_MXxx;
    //[SerializeField, RequireInterface(typeof(IInputBinding))] 
    //public Object m_Binding; // TODO Maybe instead use a specific scriptableobject derived type?
    [SerializeField] 
    public Object m_Binding;
    
    // Create a data-driven input, note that data type is explicitly defined which puts constraints on applicable bindings.
    // Also notice that this is not a serialized type and is required to be constructed in code.
    private Input<Vector2> m_Move;

    // Create a re-bindable input, not that the data type is explicitly defined which puts constraints on applicable bindings.
    // Also notice that this is a serialized type. 
    // TODO [SerializeField] private RebindableInput<Vector2> m_Move;
    
    // TODO Figure out how to approach project and scene objects. We might have assets and we might have instances
    //      and we basically want to create inputs that bind to assets. But let the objects own the slot to be
    //      configured.
    
    // Concept overview:
    // Binding assets - Is
    
    private void Awake()
    {
        m_Move = new Input<Vector2>(Presets.GenericMove); // TODO Could pass non-typed interface here and resolve? But ideally typed and sorted out at editor stage?

        //if (!(m_Binding is IInputBinding<Vector2>))
        //    m_Binding = null;
        //m_Binding = ScriptableObject.CreateInstance<InputBindingObject<Vector2>>();
    }

    private void OnEnable()
    {
        m_Move.Enable();
    }

    private void OnDisable()
    {
        m_Move.Disable();
    }
}
