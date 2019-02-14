#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
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
            foreach (var composite in InputBindingComposite.s_Composites.internedNames.Where(x =>
                !InputBindingComposite.s_Composites.aliases.Contains(x)))
            {
                menu.AddItem(new GUIContent(m_CompositeGUI.text + "/" + composite), false, OnAddCompositeBinding,
                    new KeyValuePair<string, SerializedProperty>(composite, property));
            }
            menu.ShowAsContext();
        }

        private void AddBinding(object propertyObj)
        {
            var property = (SerializedProperty)propertyObj;
            InputActionSerializationHelpers.AddBinding(property);
            property.serializedObject.ApplyModifiedProperties();
            m_Tree.Reload();
        }

        private void OnAddCompositeBinding(object compositeAndProperty)
        {
            var compositeName = ((KeyValuePair<string, SerializedProperty>)compositeAndProperty).Key;
            var property = ((KeyValuePair<string, SerializedProperty>)compositeAndProperty).Value;
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
