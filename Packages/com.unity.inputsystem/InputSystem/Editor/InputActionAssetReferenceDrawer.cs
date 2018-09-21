#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Events;
using UnityEngine.Experimental.Input.Utilities;

////FIXME: seems like drag&drop doesn't work anymore

namespace UnityEngine.Experimental.Input.Editor
{
    /// <summary>
    /// A custom <see cref="PropertyDrawer">property drawer</see> for <see cref="InputActionAssetReference"/>
    /// properties.
    /// </summary>
    [CustomPropertyDrawer(typeof(InputActionAssetReference), useForChildren: true)]
    public class InputActionAssetReferenceDrawer : PropertyDrawer
    {
        private const float kSpace = 2;
        private const float kIndent = 16;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InitializeIfNeeded(property);

            var height = EditorGUIUtility.singleLineHeight;
            height += kSpace;

            if (m_ActionEventCount > 0)
            {
                height += EditorGUIUtility.singleLineHeight; // Foldout.
                height += kSpace;

                if (m_ActionEventsExpanded)
                {
                    for (var i = 0; i < m_ActionEventCount; ++i)
                    {
                        height += EditorGUIUtility.singleLineHeight; // Foldout.
                        if (m_ActionEvents[i].isExpanded)
                        {
                            height += kSpace;
                            height += EditorGUI.GetPropertyHeight(m_ActionEvents[i]);
                        }
                        height += kSpace;
                    }
                }
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitializeIfNeeded(property);

            EditorGUI.BeginProperty(position, label, property);

            // Asset.
            var assetRect = position;
            assetRect.height = EditorGUIUtility.singleLineHeight;
            var assetProperty = property.FindPropertyRelative("m_Asset");
            EditorGUI.PropertyField(assetRect, assetProperty, label);

            // Action events.
            if (m_ActionEventCount > 0)
            {
                var eventsFoldoutRect = position;
                eventsFoldoutRect.y += assetRect.height + kSpace;
                eventsFoldoutRect.x += kIndent;
                eventsFoldoutRect.width -= kIndent;
                eventsFoldoutRect.height = EditorGUIUtility.singleLineHeight;

                m_ActionEventsExpanded =
                    EditorGUI.Foldout(eventsFoldoutRect, m_ActionEventsExpanded, m_ActionEventsLabel);
                if (m_ActionEventsExpanded)
                {
                    var actionEventsRect = eventsFoldoutRect;
                    actionEventsRect.y += eventsFoldoutRect.height + kSpace;
                    actionEventsRect.x += kIndent;
                    actionEventsRect.width -= kIndent;

                    for (var i = 0; i < m_ActionEventCount; ++i)
                    {
                        var actionEventProperty = m_ActionEvents[i];

                        // Foldout.
                        var foldoutRect = actionEventsRect;
                        foldoutRect.height = EditorGUIUtility.singleLineHeight;
                        actionEventsRect.y += foldoutRect.height + kSpace;
                        var isExpanded = EditorGUI.Foldout(foldoutRect, actionEventProperty.isExpanded,
                            actionEventProperty.displayName);
                        actionEventProperty.isExpanded = isExpanded;
                        if (isExpanded)
                        {
                            var eventRect = foldoutRect;
                            eventRect.y += foldoutRect.height + kSpace;
                            eventRect.x += kIndent;
                            eventRect.width -= kIndent;
                            eventRect.height = EditorGUI.GetPropertyHeight(actionEventProperty);
                            actionEventsRect.y += eventRect.height + kSpace;

                            EditorGUI.PropertyField(eventRect, actionEventProperty);
                        }
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        private void InitializeIfNeeded(SerializedProperty property)
        {
            if (m_Initialized)
                return;

            // Look for action events.
            foreach (var child in property.GetChildren())
            {
                // Skip properties that aren't action events.
                var type = child.GetFieldType();
                if (type == null || !typeof(UnityEvent<InputAction.CallbackContext>).IsAssignableFrom(type))
                    continue;

                // Add to list.
                ArrayHelpers.AppendWithCapacity(ref m_ActionEvents, ref m_ActionEventCount, child.Copy());
            }

            m_Initialized = true;
        }

        private bool m_Initialized;
        private bool m_ActionEventsExpanded;
        private int m_ActionEventCount;
        private SerializedProperty[] m_ActionEvents;
        private GUIContent m_ActionEventsLabel = EditorGUIUtility.TrTextContent("Action Events");
    }
}
#endif // UNITY_EDITOR
