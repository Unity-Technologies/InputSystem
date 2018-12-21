#if UNITY_EDITOR && UNITY_2018_3_OR_NEWER
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Experimental.Input.Utilities;

////TODO: detect if new input backends are enabled and put UI in here to enable them if needed

////TODO: keywords (2019.1+)
#pragma warning disable CS0414
namespace UnityEngine.Experimental.Input.Editor
{
    internal class InputSettingsProvider : SettingsProvider
    {
        public const string kEditorBuildSettingsConfigKey = "com.unity.input.settings";

        [SettingsProvider]
        public static SettingsProvider CreateInputSettingsProvider()
        {
            return new InputSettingsProvider("Project/Input (NEW)", SettingsScope.Project);
        }

        private InputSettingsProvider(string path, SettingsScope scopes)
            : base(path, scopes)
        {
            label = "Input (NEW)";
            s_Instance = this;

            InputSystem.onSettingsChange += OnSettingsChange;
        }

        public override void OnGUI(string searchContext)
        {
            if (m_Settings == null)
                InitializeWithCurrentSettings();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            var selectedSettingsAsset = EditorGUILayout.Popup(m_CurrentSelectedInputSettingsAsset,
                m_AvailableSettingsAssetsOptions, EditorStyles.toolbarPopup);
            if (selectedSettingsAsset != m_CurrentSelectedInputSettingsAsset)
            {
                // If we selected an asset and the current settings are not coming from an asset,
                // remove the "<No Asset>" entry we added to the dropdown.
                if (selectedSettingsAsset != 0 && m_AvailableInputSettingsAssets[m_CurrentSelectedInputSettingsAsset] == "<No Asset>")
                    ArrayHelpers.EraseAt(ref m_AvailableInputSettingsAssets, m_CurrentSelectedInputSettingsAsset);

                m_CurrentSelectedInputSettingsAsset = selectedSettingsAsset;
                var settings =
                    AssetDatabase.LoadAssetAtPath<InputSettings>(
                        m_AvailableInputSettingsAssets[selectedSettingsAsset]);

                // Install. This will automatically cause us to re-initialize through InputSystem.onSettingsChange.
                InputSystem.settings = settings;
            }

            // Style can only be initialized after skin has been initialized. Settings providers are
            // pulled up earlier than that so we do it lazily from here.
            if (m_NewAssetButtonStyle == null)
            {
                m_NewAssetButtonStyle = new GUIStyle("toolbarButton");
                m_NewAssetButtonStyle.fixedWidth = 40;
            }

            if (GUILayout.Button(m_NewAssetButtonText, m_NewAssetButtonStyle))
                CreateNewSettingsAsset();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (m_AvailableInputSettingsAssets[m_CurrentSelectedInputSettingsAsset] == "<No Asset>")
            {
                EditorGUILayout.HelpBox("Input settings can only be edited when stored in an asset.\n\n"
                    + "Choose an existing asset (if any) from the dropdown or create a new asset by pressing the 'New' button. "
                    + "Input settings can be placed anywhere in your project.",
                    MessageType.Info);
            }

            EditorGUILayout.HelpBox(
                "Please note that the new input system is still under development and not all features are fully functional or stable yet.\n\n"
                + "For more information, visit https://github.com/Unity-Technologies/InputSystem or https://forum.unity.com/forums/new-input-system.103/.",
                MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            EditorGUILayout.Space();

            Debug.Assert(m_Settings != null);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_UpdateMode);
            var updateMode = (InputSettings.UpdateMode)m_UpdateMode.intValue;
            if (updateMode == InputSettings.UpdateMode.ProcessEventsInBothFixedAndDynamicUpdate)
            {
                // Choosing action update mode only makes sense if we have an ambiguous situation, i.e.
                // when we have both dynamic and fixed updates in the picture.
                ////TODO: enable when action update mode is properly sorted
                //EditorGUILayout.PropertyField(m_ActionUpdateMode);
            }

            ////TODO: enable when backported
            //EditorGUILayout.PropertyField(m_TimesliceEvents);

            EditorGUILayout.PropertyField(m_RunInBackground);
            EditorGUILayout.PropertyField(m_FilterNoiseOnCurrent);
            EditorGUILayout.PropertyField(m_CompensateForScreenOrientation);

            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_DefaultDeadzoneMin);
            EditorGUILayout.PropertyField(m_DefaultDeadzoneMax);
            EditorGUILayout.PropertyField(m_DefaultButtonPressPoint);
            EditorGUILayout.PropertyField(m_DefaultTapTime);
            EditorGUILayout.PropertyField(m_DefaultSlowTapTime);
            EditorGUILayout.PropertyField(m_DefaultHoldTime);
            EditorGUILayout.PropertyField(m_DefaultSensitivity);

            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Leave 'Supported Devices' empty if you want the input system to support all input devices it can recognize. If, however, "
                + "you are only interested in a certain set of devices, adding them here will narrow the scope of what's presented in the editor "
                + "and avoid picking up input from devices not relevant to the project.", MessageType.None);

            m_SupportedDevices.DoLayoutList();

            if (EditorGUI.EndChangeCheck())
                Apply();
        }

        private static void CreateNewSettingsAsset()
        {
            // Query for file name.
            var projectName = PlayerSettings.productName;
            var path = EditorUtility.SaveFilePanel("Create Input Settings File", "Assets",
                projectName + ".inputsettings", "asset");
            if (string.IsNullOrEmpty(path))
                return;

            // Make sure the path is in the Assets/ folder.
            path = path.Replace("\\", "/"); // Make sure we only get '/' separators.
            var dataPath = Application.dataPath + "/";
            if (!path.StartsWith(dataPath, StringComparison.CurrentCultureIgnoreCase))
            {
                Debug.LogError(string.Format(
                    "Input settings must be stored in Assets folder of the project (got: '{0}')", path));
                return;
            }

            // Make sure it ends with .asset.
            var extension = Path.GetExtension(path);
            if (string.Compare(extension, ".asset", StringComparison.InvariantCultureIgnoreCase) != 0)
                path += ".asset";

            // Create settings file.
            var settings = ScriptableObject.CreateInstance<InputSettings>();
            var relativePath = "Assets/" + path.Substring(dataPath.Length);
            AssetDatabase.CreateAsset(settings, relativePath);

            // Install the settings. This will lead to an InputSystem.onSettingsChange event which in turn
            // will cause us to re-initialize.
            InputSystem.settings = settings;
        }

        /// <summary>
        /// Grab <see cref="InputSystem.settings"/> and set it up for editing.
        /// </summary>
        private void InitializeWithCurrentSettings()
        {
            // Find the set of available assets in the project.
            m_AvailableInputSettingsAssets = FindInputSettingsInProject();

            // See which is the active one.
            m_Settings = InputSystem.settings;
            var currentSettingsPath = AssetDatabase.GetAssetPath(m_Settings);
            if (string.IsNullOrEmpty(currentSettingsPath))
            {
                // The current settings aren't coming from an asset. These won't be editable
                // in the UI but we still have to show something.

                m_CurrentSelectedInputSettingsAsset = ArrayHelpers.Append(ref m_AvailableInputSettingsAssets, "<No Asset>");
                EditorBuildSettings.RemoveConfigObject(kEditorBuildSettingsConfigKey);
            }
            else
            {
                m_CurrentSelectedInputSettingsAsset = ArrayHelpers.IndexOf(m_AvailableInputSettingsAssets, currentSettingsPath);
                if (m_CurrentSelectedInputSettingsAsset == -1)
                {
                    // This is odd and shouldn't happen. Solve by just adding the path to the list.
                    m_CurrentSelectedInputSettingsAsset =
                        ArrayHelpers.Append(ref m_AvailableInputSettingsAssets, currentSettingsPath);
                }

                ////REVIEW: should we store this by platform?
                EditorBuildSettings.AddConfigObject(kEditorBuildSettingsConfigKey, m_Settings, true);
            }

            // Refresh the list of assets we display in the UI.
            m_AvailableSettingsAssetsOptions = new GUIContent[m_AvailableInputSettingsAssets.Length];
            for (var i = 0; i < m_AvailableInputSettingsAssets.Length; ++i)
            {
                var name = m_AvailableInputSettingsAssets[i];
                if (name.StartsWith("Assets/"))
                    name = name.Substring("Assets/".Length);
                if (name.EndsWith(".asset"))
                    name = name.Substring(0, name.Length - ".asset".Length);
                if (name.EndsWith(".inputsettings"))
                    name = name.Substring(0, name.Length - ".inputsettings".Length);
                m_AvailableSettingsAssetsOptions[i] = new GUIContent(name);
            }

            // Look up properties.
            m_SettingsObject = new SerializedObject(m_Settings);
            m_UpdateMode = m_SettingsObject.FindProperty("m_UpdateMode");
            m_ActionUpdateMode = m_SettingsObject.FindProperty("m_ActionUpdateMode");
            m_TimesliceEvents = m_SettingsObject.FindProperty("m_TimesliceEvents");
            m_RunInBackground = m_SettingsObject.FindProperty("m_RunInBackground");
            m_CompensateForScreenOrientation = m_SettingsObject.FindProperty("m_CompensateForScreenOrientation");
            m_FilterNoiseOnCurrent = m_SettingsObject.FindProperty("m_FilterNoiseOnCurrent");
            m_DefaultDeadzoneMin = m_SettingsObject.FindProperty("m_DefaultDeadzoneMin");
            m_DefaultDeadzoneMax = m_SettingsObject.FindProperty("m_DefaultDeadzoneMax");
            m_DefaultButtonPressPoint = m_SettingsObject.FindProperty("m_DefaultButtonPressPoint");
            m_DefaultTapTime = m_SettingsObject.FindProperty("m_DefaultTapTime");
            m_DefaultSlowTapTime = m_SettingsObject.FindProperty("m_DefaultSlowTapTime");
            m_DefaultHoldTime = m_SettingsObject.FindProperty("m_DefaultHoldTime");
            m_DefaultSensitivity = m_SettingsObject.FindProperty("m_DefaultSensitivity");

            // Initialize ReorderableList for list of supported devices.
            var supportedDevicesProperty = m_SettingsObject.FindProperty("m_SupportedDevices");
            m_SupportedDevices = new ReorderableList(m_SettingsObject, supportedDevicesProperty)
            {
                drawHeaderCallback =
                    rect => { EditorGUI.LabelField(rect, m_SupportedDevicesText); },
                onChangedCallback =
                    list => { Apply(); },
                onAddDropdownCallback =
                    (rect, list) =>
                {
                    var state = new AdvancedDropdownState();
                    var dropdown = new InputControlPickerDropdown(state,
                        path =>
                        {
                            var layoutName = InputControlPath.TryGetDeviceLayout(path) ?? path;
                            var numDevices = supportedDevicesProperty.arraySize;
                            supportedDevicesProperty.InsertArrayElementAtIndex(numDevices);
                            supportedDevicesProperty.GetArrayElementAtIndex(numDevices)
                                .stringValue = layoutName;
                            Apply();
                        }, InputControlPickerDropdown.Mode.PickDevice);
                    dropdown.Show(rect);
                },
                drawElementCallback =
                    (rect, index, isActive, isFocused) =>
                {
                    var layoutName = m_Settings.supportedDevices[index];
                    var icon = EditorInputControlLayoutCache.GetIconForLayout(layoutName);
                    if (icon != null)
                    {
                        var iconRect = rect;
                        iconRect.width = 20;
                        rect.x += 20;
                        rect.width -= 20;

                        GUI.Label(iconRect, icon);
                    }

                    EditorGUI.LabelField(rect, m_Settings.supportedDevices[index]);
                }
            };
        }

        private void Apply()
        {
            Debug.Assert(m_Settings != null);

            m_SettingsObject.ApplyModifiedProperties();
            m_Settings.OnChange();
        }

        private void OnSettingsChange()
        {
            if (InputSystem.settings != m_Settings)
                InitializeWithCurrentSettings();

            ////REVIEW: leads to double-repaint when the settings change is initiated by us; problem?
            ////FIXME: doesn't seem like there's a way to issue a repaint with the 2018.3 API
            #if UNITY_2019_1_OR_NEWER
            Repaint();
            #endif
        }

        /// <summary>
        /// Find all <see cref="InputSettings"/> stored in assets in the current project.
        /// </summary>
        /// <returns>List of input settings in project.</returns>
        private static string[] FindInputSettingsInProject()
        {
            var guids = AssetDatabase.FindAssets("t:InputSettings");
            return guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();
        }

        [SerializeField] private InputSettings m_Settings;
        [SerializeField] private bool m_SettingsIsNotAnAsset;

        [NonSerialized] private SerializedObject m_SettingsObject;
        [NonSerialized] private SerializedProperty m_UpdateMode;
        [NonSerialized] private SerializedProperty m_ActionUpdateMode;
        [NonSerialized] private SerializedProperty m_TimesliceEvents;
        [NonSerialized] private SerializedProperty m_RunInBackground;
        [NonSerialized] private SerializedProperty m_RunUpdatesManually;
        [NonSerialized] private SerializedProperty m_CompensateForScreenOrientation;
        [NonSerialized] private SerializedProperty m_FilterNoiseOnCurrent;
        [NonSerialized] private SerializedProperty m_DefaultDeadzoneMin;
        [NonSerialized] private SerializedProperty m_DefaultDeadzoneMax;
        [NonSerialized] private SerializedProperty m_DefaultButtonPressPoint;
        [NonSerialized] private SerializedProperty m_DefaultTapTime;
        [NonSerialized] private SerializedProperty m_DefaultSlowTapTime;
        [NonSerialized] private SerializedProperty m_DefaultHoldTime;
        [NonSerialized] private SerializedProperty m_DefaultSensitivity;

        [NonSerialized] private ReorderableList m_SupportedDevices;
        [NonSerialized] private string[] m_AvailableInputSettingsAssets;
        [NonSerialized] private GUIContent[] m_AvailableSettingsAssetsOptions;
        [NonSerialized] private int m_CurrentSelectedInputSettingsAsset;

        [NonSerialized] private GUIContent m_NewAssetButtonText = EditorGUIUtility.TrTextContent("New");
        [NonSerialized] private GUIContent m_SupportedDevicesText = EditorGUIUtility.TrTextContent("Supported Devices");
        [NonSerialized] private GUIStyle m_NewAssetButtonStyle;

        private static InputSettingsProvider s_Instance;

        internal static void ForceReload()
        {
            if (s_Instance != null)
            {
                // Force next OnGUI() to re-initialize.
                s_Instance.m_Settings = null;
                #if UNITY_2019_1_OR_NEWER
                s_Instance.Repaint();
                #endif
            }
        }
    }
}
#endif // UNITY_EDITOR && UNITY_2018_3_OR_NEWER
