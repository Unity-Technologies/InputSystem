#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    [CustomPropertyDrawer(typeof(InputActionMap))]
    [CustomPropertyDrawer(typeof(InputAction))]
    public class InputActionDrawer : PropertyDrawer
    {
        const int kFoldoutHeight = 15;
        const int kBindingIndent = 5;

        InputActionListTreeView m_TreeView;
        CopyPasteUtility m_CopyPasteUtility;
        
        GUIContent m_BindingGUI = EditorGUIUtility.TrTextContent("Binding");
        GUIContent m_ActionGUI = EditorGUIUtility.TrTextContent("Action");
        GUIContent m_CompositeGUI = EditorGUIUtility.TrTextContent("Composite");

        public InputActionDrawer()
        {
            Undo.undoRedoPerformed += OnUndoRedoCallback;
        }

        void OnUndoRedoCallback()
        {
            if (m_TreeView == null)
            {
                //TODO how to unregister it in a better way?
                Undo.undoRedoPerformed -= OnUndoRedoCallback;
                return;
            }
            // Force tree rebuild
            m_TreeView = null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = (float)kFoldoutHeight;
            if (property.isExpanded)
            {
                InitTreeIfNeeded(property);
                height += m_TreeView.totalHeight;
            }

            return height;
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
   
            // If the action has no name, infer it from the name of the action property.
            SetActionNameIfNotSet(property);
            
            var foldoutRect = position;
            foldoutRect.height = kFoldoutHeight;
            
            var btnRect = foldoutRect;
            btnRect.x = btnRect.width - 20;
            btnRect.width = 20;
            
            foldoutRect.width -= 20;

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

            if (property.isExpanded)
            {
                position.y += kFoldoutHeight + 2;
                position.x += kBindingIndent;
                position.width -= kBindingIndent;

                InitTreeIfNeeded(property);

                if (GUI.Button(btnRect, "+"))
                {
                    if (property.type == "InputAction")
                    {
                        OpenAddMenuForAction(property);
                    }
                    else
                    {
                        OpenAddMenuForActionMap(property);
                    }
                }

                m_TreeView.OnGUI(position);
                
                if (m_TreeView.HasFocus())
                {
                    if (Event.current.type == EventType.ValidateCommand)
                    {
                        if (m_CopyPasteUtility.IsValidCommand(Event.current.commandName))
                        {
                            Event.current.Use();
                        }
                    }

                    if (Event.current.type == EventType.ExecuteCommand)
                    {
                        m_CopyPasteUtility.HandleCommandEvent(Event.current.commandName);
                    }
                }
            }
            EditorGUI.EndProperty();
        }
        
        void OpenAddMenuForAction(SerializedProperty property)
        {
            var menu = new GenericMenu();
            menu.AddItem(m_BindingGUI, false, AddBinding, property);
           
            foreach (var composite in InputBindingComposite.s_Composites.names)
            {
                menu.AddItem(new GUIContent(m_CompositeGUI.text + "/" + composite), false, OnAddCompositeBinding, new List<object>(){composite, property});
            }
            
            menu.ShowAsContext();
        }
        
        void OpenAddMenuForActionMap(SerializedProperty property)
        {
            var menu = new GenericMenu();
            if (CanAddBinding())
            {
                menu.AddItem(m_BindingGUI, false, AddBinding, property);
            }
            else
            {
                menu.AddDisabledItem(m_BindingGUI, false);
            }
            menu.AddItem(m_ActionGUI, false, AddAction, property);
            if (CanAddBinding())
            {
                foreach (var composite in InputBindingComposite.s_Composites.names)
                {
                    menu.AddItem(new GUIContent(m_CompositeGUI.text + "/" + composite), false, OnAddCompositeBinding, new List<object>() { composite, property });
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent(m_CompositeGUI));
            }
            menu.ShowAsContext();
        }

        void AddAction(object propertyObj)
        {
            var property = (SerializedProperty)propertyObj;
            InputActionSerializationHelpers.AddAction(property);
            property.serializedObject.ApplyModifiedProperties();
            m_TreeView.Reload();
        }

        void AddBinding(object propertyObj)
        {
            if (!CanAddBinding())
                return;
            var actionMapProperty = (SerializedProperty)propertyObj;
            var action = m_TreeView.GetSelectedAction();
            InputActionSerializationHelpers.AppendBinding(action.elementProperty, actionMapProperty);
            action.elementProperty.serializedObject.ApplyModifiedProperties();
            m_TreeView.Reload();
        }

        bool CanAddBinding()
        {
            return m_TreeView.GetSelectedAction() != null;
        }

        void OnAddCompositeBinding(object paramList)
        {
            if (!CanAddBinding())
                return;
            var compositeName = (string)((List<object>)paramList)[0];
            var mapProperty = (SerializedProperty)((List<object>)paramList)[1];
            var action = m_TreeView.GetSelectedAction();
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);
            InputActionSerializationHelpers.AppendCompositeBinding(action.elementProperty, mapProperty, compositeName, compositeType);
            mapProperty.serializedObject.ApplyModifiedProperties();
            m_TreeView.Reload();
        }

        void InitTreeIfNeeded(SerializedProperty property)
        {
            if (m_TreeView == null)
            {
                if (property.type == "InputAction")
                {
                    m_TreeView = InputActionComponentListTreeView.CreateFromActionProperty(() => { }, property);
                }
                else
                {
                    m_TreeView = InputActionComponentListTreeView.CreateFromActionMapProperty(() => { }, property);
                    
                }
                m_TreeView.OnContextClick = OnContextClick;
                m_CopyPasteUtility = new CopyPasteUtility(m_TreeView);
            }
        }

        void OnContextClick(SerializedProperty serializedProperty)
        {
            var menu = new GenericMenu();
            m_CopyPasteUtility.AddOptionsToMenu(menu);
            menu.ShowAsContext();
        }
        
        void SetActionNameIfNotSet(SerializedProperty actionProperty)
        {
            var nameProperty = actionProperty.FindPropertyRelative("m_Name");
            if (!string.IsNullOrEmpty(nameProperty.stringValue))
                return;

            var suffix = actionProperty.type == "InputAction" ? "Action" : " Action Map";
            var name = actionProperty.displayName;
            if (name.EndsWith(suffix))
            {
                name = name.Substring(0, name.Length - suffix.Length);
            }
            nameProperty.stringValue = name;

            // Don't apply. Let's apply it as a side-effect whenever something about
            // the action in the UI is changed.
        }
    }
}
#endif // UNITY_EDITOR
