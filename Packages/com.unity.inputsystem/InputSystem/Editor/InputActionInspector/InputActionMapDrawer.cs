#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    [CustomPropertyDrawer(typeof(InputActionMap))]
    public class InputActionMapDrawer : PropertyDrawer
    {
        const int kFoldoutHeight = 15;
        const int kBindingIndent = 5;

        InputActionListTreeView m_TreeView;
        GUIContent m_BindingGUI = EditorGUIUtility.TrTextContent("Binding");
        GUIContent m_ActionGUI = EditorGUIUtility.TrTextContent("Action");
        GUIContent m_CompositeGUI = EditorGUIUtility.TrTextContent("Composite");

        public InputActionMapDrawer()
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
                    OpenAddMenu(property);
                }

                m_TreeView.OnGUI(position);

                if (Event.current.type == EventType.ValidateCommand)
                {
                    if (Event.current.commandName == "Delete")
                    {
                        Event.current.Use();
                    }
                }
                if (Event.current.type == EventType.ExecuteCommand)
                {
                    if (Event.current.commandName == "Delete")
                    {
                        DeleteSelectedRows(property);
                        Event.current.Use();
                    }
                }
            }
            EditorGUI.EndProperty();
        }

        void DeleteSelectedRows(SerializedProperty actionProperty)
        {
            var row = m_TreeView.GetSelectedRow();
            var rowType = row.GetType();

            // Remove composite bindings
            if (rowType == typeof(CompositeTreeItem))
            {
                for (var i = row.children.Count - 1; i >= 0; i--)
                {
                    var composite = (CompositeTreeItem)row.children[i];

                    InputActionSerializationHelpers.RemoveBinding(actionProperty, composite.index);
                }
                InputActionSerializationHelpers.RemoveBinding(actionProperty, row.index);
            }

            // Remove bindings
            if (rowType == typeof(BindingTreeItem))
            {
                InputActionSerializationHelpers.RemoveBinding(actionProperty, row.index);
            }

            m_TreeView.SetSelection(new List<int>());
            m_TreeView.Reload();
        }

        void OpenAddMenu(SerializedProperty property)
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
                m_TreeView = InputActionComponentListTreeView.CreateFromActionMapProperty(() => {}, property);
                m_TreeView.OnContextClick = OnContextClick;
            }
        }

        void OnContextClick(SerializedProperty serializedProperty)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete"), false,
                () =>
                {
                    DeleteSelectedRows(serializedProperty);
                });
            menu.ShowAsContext();
        }

        void SetActionNameIfNotSet(SerializedProperty actionProperty)
        {
            var nameProperty = actionProperty.FindPropertyRelative("m_Name");
            if (!string.IsNullOrEmpty(nameProperty.stringValue))
                return;

            var name = actionProperty.displayName;
            if (name.EndsWith(" Action Map"))
                name = name.Substring(0, name.Length - " Action Map".Length);

            nameProperty.stringValue = name;
            // Don't apply. Let's apply it as a side-effect whenever something about
            // the action in the UI is changed.
        }
    }
}
#endif // UNITY_EDITOR
