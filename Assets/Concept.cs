using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
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
public class Input3<T> //: ScriptableObject 
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
    
    public Input3(Action<InputAction> preset)
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
    
    public static implicit operator bool(Input3<T> input)
    {
        return !ReferenceEquals(null, input); // TODO Return true if input has values
    }

    public void OnBeforeSerialize()
    {
        Debug.Log("Before serialize");
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

[CustomPropertyDrawer(typeof(Input3<>), useForChildren: true)]
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

        EditorGUI.LabelField(position, new GUIContent("Preset"), new GUIContent("[Preset]"));

        //EditorGUI.ObjectField()
        
        //position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
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

// Allows picking an asset.
// If not saved to an asset its saved with the scene.
// If ScriptableSingleton its saved as a project wide.
public class Scriptable : ScriptableObject
{
    private void OnValidate()
    {
        Debug.Log("Scriptable OnValidate");
    }

    private void OnEnable()
    {
        Debug.Log("Scriptable OnEnable");
    }

    private void OnDisable()
    {
        throw new NotImplementedException();
    }

    private void OnDestroy()
    {
        throw new NotImplementedException();
    }
}

public class Concept : MonoBehaviour
{
    [SerializeField] private InputAction fire;

    [Header("Bindable Actions")]
    //[SerializeField] private Input<bool> move = new(preset: BindingPresets.ByGenre.Platformer2D.Move);
    //[SerializeField] private Input<bool> jump
    //
    //= new(BindingPresets.ByGenre.Platformer2D.Jump);

    [SerializeField] public TypedInputAction<bool> jump = new(preset: BindingPresets.ByGenre.Platformer2D.Jump);

    [SerializeField] public Scriptable scriptable;
    //[SerializeField] private InputAction jump3;

    
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
        jump.ApplyPresetIfNotAlreadyBound(); // TODO Could just have passed preset here unless we want it from editor, could be achieved with attribute
        jump.Enable();
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

    // Small but doesn't encapsulate full callback context
    [InputBindable(hint : BindingHint.Platformer2D.Jump)]
    public void Jump()
    {
        Debug.Log("Jump was invoked");
    }
}

public class ConceptUtils
{
    public static T[] GetAllInstances<T>() where T : ScriptableObject
    {
        var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name); //FindAssets uses tags check documentation for more info
        var a = new T[guids.Length];
        for (var i =0; i < guids.Length; ++i)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            a[i] = AssetDatabase.LoadAssetAtPath<T>(path);
        }
        return a;
    }
}

public class MenuTest : MonoBehaviour
{
    // Add a menu item named "Do Something" to MyMenu in the menu bar.
    [MenuItem("MyMenu/Do Something")]
    static void DoSomething()
    {
        //foreach (var x : ConceptUtils.GetAllInstances<InputBindable>())
    }
}

// What if we only had a component that would bind another components
// method. This would still not be type-safe, it would introduce indirection.
// However, that performance hit could be ignored for sake of simplicity.
// It would basically just need to have InputAction member and another
// UI and facilitate auto-assignment.
