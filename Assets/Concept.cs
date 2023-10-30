using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Cinemachine.Editor;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public static class BindingHint
{
    public const int Unspecified = 0;
    
    public static class Platformer2D
    {
        public const int Fire = 1;
        public const int Jump = 2;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class InputBindable : System.Attribute
{
    public InputBindable(int hint = BindingHint.Unspecified)
    {
        Hint = hint;
    }

    public InputBindable(Type hintClass)
    {
        
    }

    public int Hint { get; private set; }
}

[Serializable]
public class BindingContext
{
    private static void Bind(object obj, MethodInfo method, InputBindable attribute)
    {
        var hint = attribute.Hint;
        switch (hint)
        {
            case BindingHint.Platformer2D.Fire:
            {
                InputAction action = new InputAction("fire", InputActionType.Button);
                action.AddBinding("<Keyboard>/space");
                action.Enable();
            }
                break;
            case BindingHint.Platformer2D.Jump:
                break;
            case BindingHint.Unspecified:
            default:
                break;
        }
    } 
    
    public static void Bind(object obj)
    {
        if (obj == null)
            throw new NullReferenceException($"obj is required");
        
        var type = obj.GetType();
        var methods = type.GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(InputBindable), false).Length > 0)
            .ToArray();
        foreach (var method in type.GetMethods())
        {
            foreach (var attribute in method.GetCustomAttributes(typeof(InputBindable), false))
            {
                var attr = attribute as InputBindable;
                Bind(obj, method, attr);
            }
        }
    }
}



/*namespace BindingPresets
{
    static partial class Platformer2D
    {
        public static Action<InputAction> Fire()
        {
            return action =>
            {
                action.AddBinding("<Keyboard>/ctrl");
            };
        }
        
        public static Action<InputAction> Jump()
        {
            return action =>
            {
                action.AddBinding("<Keyboard>/space");
            };
        }
    }
}*/

// Extension to apply preset bindings to InputAction family of types.

// TODO We want to pick a preset to be applied via a menu or preset menu?!
[Serializable]
public class BindableInputAction<T> //: ScriptableObject 
{
    // We do not want hint to be serialized since we want it defined by code
    //[NonSerialized] private readonly IBindingPreset m_Hint;
    //[SerializeField] private int x = 123;

    //[SerializeField] private InputBinding[] m_Preset; // TODO We do not want this to be serialized
    //[SerializeField] private string hint;
    //[SerializeReference] private BindingPreset preset;
    
    // A reference to a preset factory
    [SerializeReference] private Action<InputAction> preset;
    
    // An embedded input action
    [SerializeField] public InputAction action;
    
    public BindableInputAction(Action<InputAction> preset)
    {
        this.preset = preset;
    }

    public Action<InputAction> bindingPreset => preset;

    private void OnEnable()
    {
        Debug.Log("OnEnable XXXXXXXX");
    }

    private void OnValidate()
    {
        
    }
    
    public static implicit operator bool(BindableInputAction<T> input)
    {
        return !ReferenceEquals(null, input); // TODO Return true if input has values
    }

    public void OnBeforeSerialize()
    {
        
    }

    public void OnAfterDeserialize()
    {
        Debug.Log("After deserialize");
    }

    public void ApplyPresetIfNotAlreadyBound()
    {
        // Early return in case the action has already been bound in edit-mode
        if (action.bindings.Any())
            return; 
        
        // Apply preset
        preset(action);
    }
}

[CustomPropertyDrawer(typeof(BindableInputAction<>), useForChildren: true)]
public class BindableInputActionPropertyDrawer : PropertyDrawer
{
    private const string kPresetName = "preset";
    private const string kActionName = "action";
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var presetProperty = property.FindPropertyRelative(kPresetName);
        var actionProperty = property.FindPropertyRelative(kActionName);

        return EditorGUIUtility.standardVerticalSpacing +
               //EditorGUI.GetPropertyHeight(presetProperty) +
               EditorGUIUtility.singleLineHeight +
               EditorGUIUtility.standardVerticalSpacing +
               EditorGUI.GetPropertyHeight(actionProperty);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //var obj = property.objectReferenceValue;
        //property.va
        
        var presetProperty = property.FindPropertyRelative(kPresetName);
        var actionProperty = property.FindPropertyRelative(kActionName);

        //var preset = presetProperty.objectReferenceValue;
        var rect = position;
        rect.height = EditorGUI.GetPropertyHeight(actionProperty) + EditorGUIUtility.standardVerticalSpacing * 2 + EditorGUIUtility.singleLineHeight;
        
        label = EditorGUI.BeginProperty(position, label, property);

        EditorGUI.LabelField(position, new GUIContent("Preset"), new GUIContent("Jump"));

        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        //EditorGUI.PropertyField(position, actionProperty);

        EditorGUI.EndProperty();
        //EditorGUI.ObjectField(position, actionProperty);

        //var script = property.serializedObject.targetObject;

        //var x = property.obj
        //var actionProperty = property.FindPropertyRelative("m_Action");
        //EditorGUI.PropertyField(position, actionProperty, new GUIContent(actionProperty.displayName));
        //EditorGUI.BeginProperty(position, label, x);
        /*if (hint != null)
            EditorGUI.ObjectField(position, hint);*/
        //EditorGUI.EndProperty();
    }
}

[Serializable]
public class CustomType<T> where T : struct
{
    [SerializeField] public T value;
}

[CustomPropertyDrawer(typeof(CustomType<>), useForChildren: true)]
public class CustomTypePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {

        //GUI.enabled = false;
        //if (prop != null)
        //    EditorGUI.PropertyField(position, prop, new GUIContent(prop.displayName));
        //GUI.enabled = true;
        //base.OnGUI(position, property, label);
    }
}

public class Concept : MonoBehaviour
{
    [SerializeField] private InputAction fire;

    [Header("Bindable Actions")]
    
    [SerializeField] private BindableInputAction<bool> jump3 = new(BindingPresets.Platformer2D.jump);

    //[SerializeFiel] public CustomType<Vector2> custom;
    
    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log(jump3);
        //Debug.Log(jump3.BindingPreset);
    }
    
    void OnEnable()
    {
        // Apply a binding preset if not bound in editor
        //fire.ApplyPresetIfNotBound(BindingPresets.Platformer2D.jump);
        jump3.ApplyPresetIfNotAlreadyBound();
        jump3.action.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        /*if (jump)
            Jump();*/
    }
    
    /*[InputBindable(hintClass: typeof(BindingHint.Platformer2D.))]
    public void Fire()
    {
        Debug.Log("Fire was invoked");
    }*/

    [InputBindable(hint : BindingHint.Platformer2D.Jump)]
    public void Jump()
    {
        Debug.Log("Jump was invoked");
    }
}

// What if we only had a component that would bind another components
// method. This would still not be type-safe, it would introduce indirection.
// However, that performance hit could be ignored for sake of simplicity.
// It would basically just need to have InputAction member and another
// UI and facilitate auto-assignment.
