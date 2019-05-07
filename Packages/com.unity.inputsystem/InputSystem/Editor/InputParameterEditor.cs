#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

////REVIEW: generalize this to something beyond just parameters?

namespace UnityEngine.InputSystem.Editor
{
    public abstract class InputParameterEditor
    {
        public object target { get; internal set; }

        public abstract void OnGUI();

        internal abstract void SetTarget(object target);

        public static Type LookupEditorForType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (s_TypeLookupCache == null)
            {
                s_TypeLookupCache = new Dictionary<Type, Type>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var typeInfo in assembly.DefinedTypes)
                    {
                        // Only looking for classes.
                        if (!typeInfo.IsClass)
                            continue;

                        var definedType = typeInfo.AsType();
                        if (definedType == null)
                            continue;

                        // Only looking for InputParameterEditors.
                        if (!typeof(InputParameterEditor).IsAssignableFrom(definedType))
                            continue;

                        // Grab <TValue> parameter from InputParameterEditor<>.
                        var objectType =
                            TypeHelpers.GetGenericTypeArgumentFromHierarchy(definedType, typeof(InputParameterEditor<>),
                                0);
                        if (objectType == null)
                            continue;

                        s_TypeLookupCache[objectType] = definedType;
                    }
                }
            }

            s_TypeLookupCache.TryGetValue(type, out var editorType);
            return editorType;
        }

        private static Dictionary<Type, Type> s_TypeLookupCache;
    }

    public abstract class InputParameterEditor<TObject> : InputParameterEditor
        where TObject : class
    {
        public new TObject target { get; private set; }

        protected virtual void OnEnable()
        {
        }

        internal override void SetTarget(object target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (!(target is TObject targetOfType))
                throw new ArgumentException(
                    $"Expecting object of type '{typeof(TObject).Name}' but got object of type '{target.GetType().Name}' instead",
                    nameof(target));

            this.target = targetOfType;
            base.target = targetOfType;

            OnEnable();
        }

        /// <summary>
        /// Helper for parameters that have defaults (usually from <see cref="InputSettings"/>).
        /// </summary>
        /// <remarks>
        /// Has a bool toggle to switch between default and custom value.
        /// </remarks>
        internal struct CustomOrDefaultSetting
        {
            public void Initialize(string label, string tooltip, string defaultName, Func<float> getValue,
                Action<float> setValue, Func<float> getDefaultValue, bool defaultComesFromInputSettings = true)
            {
                m_GetValue = getValue;
                m_SetValue = setValue;
                m_GetDefaultValue = getDefaultValue;
                m_ToggleLabel = EditorGUIUtility.TrTextContent("Default",
                    defaultComesFromInputSettings
                    ? $"If enabled, the default {label.ToLower()} configured globally in the input settings is used. See Edit >> Project Settings... >> Input (NEW)."
                    : "If enabled, the default value is used.");
                m_ValueLabel = EditorGUIUtility.TrTextContent(label, tooltip);
                if (defaultComesFromInputSettings)
                    m_OpenInputSettingsLabel = EditorGUIUtility.TrTextContent("Open Input Settings");
                m_UseDefaultValue = Mathf.Approximately(getValue(), 0);
                m_DefaultComesFromInputSettings = defaultComesFromInputSettings;
                m_HelpBoxText =
                    EditorGUIUtility.TrTextContent(
                        $"Uses \"{defaultName}\" set in project-wide input settings.");
            }

            public void OnGUI()
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(m_UseDefaultValue);
                var value = m_GetValue();
                if (m_UseDefaultValue)
                    value = m_GetDefaultValue();
                ////TODO: use slider rather than float field
                var newValue = EditorGUILayout.FloatField(m_ValueLabel, value, GUILayout.ExpandWidth(false));
                if (!m_UseDefaultValue)
                    m_SetValue(newValue);
                EditorGUI.EndDisabledGroup();
                var newUseDefault = GUILayout.Toggle(m_UseDefaultValue, m_ToggleLabel, GUILayout.ExpandWidth(false));
                if (newUseDefault != m_UseDefaultValue)
                {
                    if (!newUseDefault)
                        m_SetValue(m_GetDefaultValue());
                    else
                        m_SetValue(0);
                }
                m_UseDefaultValue = newUseDefault;
                EditorGUILayout.EndHorizontal();

                // If we're using a default from global InputSettings, show info text for that and provide
                // button to open input settings.
                if (m_UseDefaultValue && m_DefaultComesFromInputSettings)
                {
                    EditorGUILayout.HelpBox(m_HelpBoxText);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(m_OpenInputSettingsLabel, EditorStyles.miniButton, GUILayout.MaxWidth(100)))
                        InputSettingsProvider.Open();
                    EditorGUILayout.EndHorizontal();
                }
            }

            private Func<float> m_GetValue;
            private Action<float> m_SetValue;
            private Func<float> m_GetDefaultValue;
            private bool m_UseDefaultValue;
            private bool m_DefaultComesFromInputSettings;
            private GUIContent m_ToggleLabel;
            private GUIContent m_ValueLabel;
            private GUIContent m_OpenInputSettingsLabel;
            private GUIContent m_HelpBoxText;
        }
    }
}
#endif // UNITY_EDITOR
