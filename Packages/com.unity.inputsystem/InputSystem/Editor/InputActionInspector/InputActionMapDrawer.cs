#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    [CustomPropertyDrawer(typeof(InputActionMap))]
    internal class InputActionMapDrawer : InputDrawersBase
    {
        protected override void OpenAddMenu(SerializedProperty property)
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

        protected void AddAction(object propertyObj)
        {
            var property = (SerializedProperty)propertyObj;
            InputActionSerializationHelpers.AddAction(property);
            property.serializedObject.ApplyModifiedProperties();
            m_Tree.Reload();
        }

        internal void AddBinding(object propertyObj)
        {
            if (!CanAddBinding())
                return;
            var property = (SerializedProperty)propertyObj;
            var actionMapProperty = (SerializedProperty)propertyObj;
            var action = m_Tree.GetSelectedAction();
            InputActionSerializationHelpers.AddBinding(action.elementProperty, actionMapProperty);
            property.serializedObject.ApplyModifiedProperties();
            m_Tree.Reload();
        }

        protected bool CanAddBinding()
        {
            return m_Tree.GetSelectedAction() != null;
        }

        internal void OnAddCompositeBinding(object paramList)
        {
            if (!CanAddBinding())
                return;

            var compositeName = (string)((List<object>)paramList)[0];
            var property = (SerializedProperty)((List<object>)paramList)[1];
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);
            var action = m_Tree.GetSelectedAction();
            InputActionSerializationHelpers.AddCompositeBinding(action.elementProperty, property, compositeName, compositeType);
            property.serializedObject.ApplyModifiedProperties();
            m_Tree.Reload();
        }

        protected override InspectorTree CreateTree(SerializedProperty property)
        {
            return InspectorTree.CreateFromActionMapProperty(() => {}, property);
        }

        protected override string GetSuffix()
        {
            return " Action Map";
        }
    }
}
#endif // UNITY_EDITOR
