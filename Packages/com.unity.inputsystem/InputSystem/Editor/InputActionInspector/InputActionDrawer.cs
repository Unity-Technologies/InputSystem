#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    [CustomPropertyDrawer(typeof(InputAction))]
    internal class InputDrawers : InputDrawersBase
    {
        protected override void OpenAddMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            menu.AddItem(m_BindingGUI, false, AddBinding, property);
            foreach (var composite in InputBindingComposite.s_Composites.names)
            {
                menu.AddItem(new GUIContent(m_CompositeGUI.text + "/" + composite), false, OnAddCompositeBinding, new List<object>(){composite, property});
            }
            menu.ShowAsContext();
        }

        internal void AddBinding(object propertyObj)
        {
            var property = (SerializedProperty)propertyObj;
            InputActionSerializationHelpers.AddBinding(property, null);
            property.serializedObject.ApplyModifiedProperties();
            m_Tree.Reload();
        }

        internal void OnAddCompositeBinding(object paramList)
        {
            var compositeName = (string)((List<object>)paramList)[0];
            var property = (SerializedProperty)((List<object>)paramList)[1];
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);
            InputActionSerializationHelpers.AddCompositeBinding(property, null, compositeName, compositeType);
            property.serializedObject.ApplyModifiedProperties();
            m_Tree.Reload();
        }

        protected override InspectorTree CreateTree(SerializedProperty property)
        {
            return InspectorTree.CreateFromActionProperty(() => {}, property);
        }

        protected override string GetSuffix()
        {
            return " Action";
        }
    }
}
#endif // UNITY_EDITOR
