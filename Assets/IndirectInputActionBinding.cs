using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("Input/Input Action Binding")]
[DisallowMultipleComponent] // Only a single component is allowed per object
//[ExecuteInEditMode]         // Allow reflecting other components in edit mode.
public class InputActionBindings : MonoBehaviour
{
      private BindTarget[] bindTargets = { };

      [UnityEditor.Callbacks.DidReloadScripts]
      static void OnReloadScripts()
      {
            Debug.Log("OnReloadScripts");
      }
      
      private struct BindTarget
      {
            public MethodInfo method;
            public InputBindable attribute;

            public override string ToString()
            {
                  return $"Method: {method}, Attribute: {attribute}";
            }
      }
      
      private static IEnumerable<BindTarget> GetTargets(GameObject obj)
      {
            foreach (var component in obj.GetComponents<MonoBehaviour>())
            {
                  foreach (var method in component.GetType().GetMethods())
                  {
                        foreach (var attribute in method.GetCustomAttributes(typeof(InputBindable), false))
                        {
                              var attr = attribute as InputBindable;
                              yield return new BindTarget(){attribute = attr, method = method};
                        }
                  }
            }
      }

      private void UpdateBindTargets()
      {
            bindTargets = GetTargets(this.gameObject).ToArray();
      }

      private void ListBindTargets()
      {
            foreach (var target in bindTargets)
                  Debug.Log(target);
      }
      
      public void OnValidate()
      {
            Debug.Log("OnValidate");
      }

      public void Update()
      {
            if (Application.isPlaying)
                  return;
            
            ListBindTargets();
      }
}

[CustomPropertyDrawer(typeof(InputActionBindings))]
public class InputActionBindingsPropertyDrawer : PropertyDrawer
{
      public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
      {
            EditorGUI.BeginProperty(position, label, property);
            var value = property.objectReferenceValue;
            EditorGUI.LabelField(position, "What?");
            EditorGUI.EndProperty();
      }
}