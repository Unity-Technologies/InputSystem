#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    abstract class InputDrawersBase : PropertyDrawer
    {
        const int kFoldoutHeight = 15;
        const int kBindingIndent = 5;

        protected InputActionListTreeView m_TreeView;
        CopyPasteUtility m_CopyPasteUtility;

        protected GUIContent m_BindingGUI = EditorGUIUtility.TrTextContent("Binding");
        protected GUIContent m_ActionGUI = EditorGUIUtility.TrTextContent("Action");
        protected GUIContent m_CompositeGUI = EditorGUIUtility.TrTextContent("Composite");

        protected InputDrawersBase()
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

        void InitTreeIfNeeded(SerializedProperty property)
        {
            if (m_TreeView == null)
            {
                m_TreeView = CreateTree(property);
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

            var suffix = GetSuffix();
            var name = actionProperty.displayName;
            if (name.EndsWith(suffix))
            {
                name = name.Substring(0, name.Length - suffix.Length);
            }
            nameProperty.stringValue = name;

            // Don't apply. Let's apply it as a side-effect whenever something about
            // the action in the UI is changed.
        }

        protected abstract void OpenAddMenu(SerializedProperty property);
        protected abstract InputActionListTreeView CreateTree(SerializedProperty property);
        protected abstract string GetSuffix();
    }
}
#endif // UNITY_EDITOR
