#if UNITY_EDITOR || PACKAGE_DOCS_GENERATION
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

////REVIEW: generalize this to something beyond just parameters?

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A custom UI for editing parameter values on a <see cref="InputProcessor"/>, <see cref="InputBindingComposite"/>,
    /// or <see cref="IInputInteraction"/>.
    /// </summary>
    /// <remarks>
    /// When implementing a custom parameter editor, use <see cref="InputParameterEditor{TObject}"/> instead.
    /// </remarks>
    /// <seealso cref="InputActionRebindingExtensions.GetParameterValue(InputAction,string,InputBinding)"/>
    /// <seealso cref="InputActionRebindingExtensions.ApplyParameterOverride(InputActionMap,string,PrimitiveValue,InputBinding)"/>
    public abstract class InputParameterEditor
    {
        /// <summary>
        /// The <see cref="InputProcessor"/>, <see cref="InputBindingComposite"/>, or <see cref="IInputInteraction"/>
        /// being edited.
        /// </summary>
        public object target { get; internal set; }

        /// <summary>
        /// Callback for implementing a custom UI.
        /// </summary>
        public abstract void OnGUI();

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        /// <summary>
        /// Add visual elements for this parameter editor to a root VisualElement.
        /// </summary>
        /// <param name="root">The VisualElement that parameter editor elements should be added to.</param>
        /// <param name="onChangedCallback">A callback that will be called when any of the parameter editors
        /// changes value.</param>
        public abstract void OnDrawVisualElements(VisualElement root, Action onChangedCallback);
#endif

        internal abstract void SetTarget(object target);

        internal static Type LookupEditorForType(Type type)
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

    /// <summary>
    /// A custom UI for editing parameter values on a <see cref="InputProcessor"/>,
    /// <see cref="InputBindingComposite"/>, or <see cref="IInputInteraction"/>.
    /// </summary>
    /// <remarks>
    /// Custom parameter editors do not need to be registered explicitly. Say you have a custom
    /// <see cref="InputProcessor"/> called <c>QuantizeProcessor</c>. To define a custom editor
    /// UI for it, simply define a new class based on <c>InputParameterEditor&lt;QuantizeProcessor&gt;</c>.
    ///
    /// <example>
    /// <code>
    /// public class QuantizeProcessorEditor : InputParameterEditor&lt;QuantizeProcessor&gt;
    /// {
    ///     // You can put initialization logic in OnEnable, if you need it.
    ///     public override void OnEnable()
    ///     {
    ///         // Use the 'target' property to access the QuantizeProcessor instance.
    ///     }
    ///
    ///     // In OnGUI, you can define custom UI elements. Use EditorGUILayout to lay
    ///     // out the controls.
    ///     public override void OnGUI()
    ///     {
    ///         // Say that QuantizeProcessor has a "stepping" property that determines
    ///         // the stepping distance for discrete values returned by the processor.
    ///         // We can expose it here as a float field. To apply the modification to
    ///         // processor object, we just assign the value back to the field on it.
    ///         target.stepping = EditorGUILayout.FloatField(
    ///             m_SteppingLabel, target.stepping);
    ///     }
    ///
    ///     private GUIContent m_SteppingLabel = new GUIContent("Stepping",
    ///         "Discrete stepping with which input values will be quantized.");
    /// }
    /// </code>
    /// </example>
    ///
    /// Note that a parameter editor takes over the entire editing UI for the object and
    /// not just the editing of specific parameters.
    ///
    /// The default parameter editor will derive names from the names of the respective
    /// fields just like the Unity inspector does. Also, it will respect tooltips applied
    /// to these fields with Unity's <c>TooltipAttribute</c>.
    ///
    /// So, let's say that <c>QuantizeProcessor</c> from our example was defined like
    /// below. In that case, the result would be equivalent to the custom parameter editor
    /// UI defined above.
    ///
    /// <example>
    /// <code>
    /// public class QuantizeProcessor : InputProcessor&lt;float&gt;
    /// {
    ///     [Tooltip("Discrete stepping with which input values will be quantized.")]
    ///     public float stepping;
    ///
    ///     public override float Process(float value, InputControl control)
    ///     {
    ///         return value - value % stepping;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public abstract class InputParameterEditor<TObject> : InputParameterEditor
        where TObject : class
    {
        /// <summary>
        /// The <see cref="InputProcessor"/>, <see cref="InputBindingComposite"/>, or <see cref="IInputInteraction"/>
        /// being edited.
        /// </summary>
        public new TObject target { get; private set; }

        /// <summary>
        /// Called after the parameter editor has been initialized.
        /// </summary>
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

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        /// <summary>
        /// Default stub implementation of <see cref="InputParameterEditor.OnDrawVisualElements"/>.
        /// Should be overridden to create the desired UI.
        /// </summary>
        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
        }

#endif

        /// <summary>
        /// Helper for parameters that have defaults (usually from <see cref="InputSettings"/>).
        /// </summary>
        /// <remarks>
        /// Has a bool toggle to switch between default and custom value.
        /// </remarks>
        internal class CustomOrDefaultSetting
        {
            public void Initialize(string label, string tooltip, string defaultName, Func<float> getValue,
                Action<float> setValue, Func<float> getDefaultValue, bool defaultComesFromInputSettings = true,
                float defaultInitializedValue = default)
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
                m_DefaultInitializedValue = defaultInitializedValue;
                m_UseDefaultValue = Mathf.Approximately(getValue(), defaultInitializedValue);
                m_DefaultComesFromInputSettings = defaultComesFromInputSettings;
                m_HelpBoxText =
                    EditorGUIUtility.TrTextContent(
                        $"Uses \"{defaultName}\" set in project-wide input settings.");
            }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            public void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
            {
                var value = m_GetValue();

                if (m_UseDefaultValue)
                    value = m_GetDefaultValue();

                // If previous value was an epsilon away from default value, it most likely means that value was set by our own code down in this method.
                // Revert it back to default to show a nice readable value in UI.
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if ((value - float.Epsilon) == m_DefaultInitializedValue)
                    value = m_DefaultInitializedValue;

                var container = new VisualElement();
                var settingsContainer = new VisualElement { style = { flexDirection = FlexDirection.Row } };


                m_FloatField = new FloatField(m_ValueLabel.text) { value = value };
                m_FloatField.Q("unity-text-input").AddToClassList("float-field");
                m_FloatField.RegisterValueChangedCallback(ChangeSettingValue);
                m_FloatField.RegisterCallback<BlurEvent>(_ => OnEditEnd(onChangedCallback));
                m_FloatField.SetEnabled(!m_UseDefaultValue);

                m_HelpBox = new HelpBox(m_HelpBoxText.text, HelpBoxMessageType.None);

                m_DefaultToggle = new Toggle("Default") { value = m_UseDefaultValue };
                m_DefaultToggle.RegisterValueChangedCallback(evt => ToggleUseDefaultValue(evt, onChangedCallback));


                var buttonContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.RowReverse
                    }
                };
                m_OpenInputSettingsButton = new Button(InputSettingsProvider.Open){text = m_OpenInputSettingsLabel.text};
                m_OpenInputSettingsButton.AddToClassList("open-settings-button");

                settingsContainer.Add(m_FloatField);
                settingsContainer.Add(m_DefaultToggle);
                container.Add(settingsContainer);

                if (m_UseDefaultValue)
                {
                    buttonContainer.Add(m_OpenInputSettingsButton);
                    container.Add(m_HelpBox);
                }

                container.Add(buttonContainer);

                root.Add(container);
            }

            private void ChangeSettingValue(ChangeEvent<float> evt)
            {
                if (m_UseDefaultValue) return;

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (evt.newValue == m_DefaultInitializedValue)
                {
                    // If user sets a value that is equal to default initialized, change value slightly so it doesn't pass potential default checks.
                    ////TODO: refactor all of this to use tri-state values instead, there is no obvious float value that we can use as default (well maybe NaN),
                    ////so instead it would be better to have a separate bool to show if value is present or not.
                    m_SetValue(evt.newValue + float.Epsilon);
                }
                else
                {
                    m_SetValue(evt.newValue);
                }
            }

            private void OnEditEnd(Action onChangedCallback)
            {
                onChangedCallback.Invoke();
            }

            private void ToggleUseDefaultValue(ChangeEvent<bool> evt, Action onChangedCallback)
            {
                if (evt.newValue != m_UseDefaultValue)
                {
                    m_SetValue(!evt.newValue ? m_GetDefaultValue() : m_DefaultInitializedValue);
                    onChangedCallback.Invoke();
                }

                m_UseDefaultValue = evt.newValue;
                m_FloatField?.SetEnabled(!m_UseDefaultValue);
            }

#endif

            public void OnGUI()
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(m_UseDefaultValue);

                var value = m_GetValue();

                if (m_UseDefaultValue)
                    value = m_GetDefaultValue();

                // If previous value was an epsilon away from default value, it most likely means that value was set by our own code down in this method.
                // Revert it back to default to show a nice readable value in UI.
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if ((value - float.Epsilon) == m_DefaultInitializedValue)
                    value = m_DefaultInitializedValue;

                ////TODO: use slider rather than float field
                var newValue = EditorGUILayout.FloatField(m_ValueLabel, value, GUILayout.ExpandWidth(false));
                if (!m_UseDefaultValue)
                {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (newValue == m_DefaultInitializedValue)
                        // If user sets a value that is equal to default initialized, change value slightly so it doesn't pass potential default checks.
                        ////TODO: refactor all of this to use tri-state values instead, there is no obvious float value that we can use as default (well maybe NaN),
                        ////so instead it would be better to have a separate bool to show if value is present or not.
                        m_SetValue(newValue + float.Epsilon);
                    else
                        m_SetValue(newValue);
                }

                EditorGUI.EndDisabledGroup();

                var newUseDefault = GUILayout.Toggle(m_UseDefaultValue, m_ToggleLabel, GUILayout.ExpandWidth(false));
                if (newUseDefault != m_UseDefaultValue)
                {
                    if (!newUseDefault)
                        m_SetValue(m_GetDefaultValue());
                    else
                        m_SetValue(m_DefaultInitializedValue);
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
                    if (GUILayout.Button(m_OpenInputSettingsLabel, EditorStyles.miniButton))
                        InputSettingsProvider.Open();
                    EditorGUILayout.EndHorizontal();
                }
            }

            private Func<float> m_GetValue;
            private Action<float> m_SetValue;
            private Func<float> m_GetDefaultValue;
            private bool m_UseDefaultValue;
            private bool m_DefaultComesFromInputSettings;
            private float m_DefaultInitializedValue;
            private GUIContent m_ToggleLabel;
            private GUIContent m_ValueLabel;
            private GUIContent m_OpenInputSettingsLabel;
            private GUIContent m_HelpBoxText;
            private FloatField m_FloatField;
            private Button m_OpenInputSettingsButton;
            private Toggle m_DefaultToggle;
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            private HelpBox m_HelpBox;
#endif
        }
    }
}
#endif // UNITY_EDITOR
