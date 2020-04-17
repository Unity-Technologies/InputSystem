#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

////TODO: detect if new input backends are enabled and put UI in here to enable them if needed

////TODO: keywords (2019.1+)
#pragma warning disable CS0414
namespace UnityEngine.InputSystem.Editor
{
    internal class InputSettingsProvider : SettingsProvider, IDisposable
    {
        public const string kEditorBuildSettingsConfigKey = "com.unity.input.settings";
        public const string kSettingsPath = "Project/Input System Package";

        public static void Open()
        {
            SettingsService.OpenProjectSettings(kSettingsPath);
        }

        [SettingsProvider]
        public static SettingsProvider CreateInputSettingsProvider()
        {
            return new InputSettingsProvider(kSettingsPath, SettingsScope.Project);
        }

        private InputSettingsProvider(string path, SettingsScope scopes)
            : base(path, scopes)
        {
            label = "Input System Package";
            s_Instance = this;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            InputSystem.onSettingsChange += OnSettingsChange;
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            InputSystem.onSettingsChange -= OnSettingsChange;
        }

        public void Dispose()
        {
            m_SettingsObject?.Dispose();
        }

        public override void OnTitleBarGUI()
        {
            if (EditorGUILayout.DropdownButton(EditorGUIUtility.IconContent("_Popup"), FocusType.Passive, EditorStyles.label))
            {
                var menu = new GenericMenu();
                menu.AddDisabledItem(new GUIContent("Available Settings Assets:"));
                menu.AddSeparator("");
                for (var i = 0; i < m_AvailableSettingsAssetsOptions.Length; i++)
                    menu.AddItem(new GUIContent(m_AvailableSettingsAssetsOptions[i]), m_CurrentSelectedInputSettingsAsset == i, (path) => {
                        InputSystem.settings = AssetDatabase.LoadAssetAtPath<InputSettings>((string)path);
                    }, m_AvailableInputSettingsAssets[i]);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("New Settings Asset…"), false, CreateNewSettingsAsset);
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        public override void OnGUI(string searchContext)
        {
            if (m_Settings == null)
                InitializeWithCurrentSettings();

            if (m_AvailableInputSettingsAssets.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "Settings for the new input system are stored in an asset. Click the button below to create a settings asset you can edit.",
                    MessageType.Info);
                if (GUILayout.Button("Create settings asset", GUILayout.Height(30)))
                    CreateNewSettingsAsset("Assets/InputSystem.inputsettings.asset");
                GUILayout.Space(20);
            }

            using (new EditorGUI.DisabledScope(m_AvailableInputSettingsAssets.Length == 0))
            {
                EditorGUILayout.Space();
                EditorGUILayout.Separator();
                EditorGUILayout.Space();

                Debug.Assert(m_Settings != null);

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(m_UpdateMode, m_UpdateModeContent);

                EditorGUILayout.PropertyField(m_FilterNoiseOnCurrent, m_FilterNoiseOnCurrentContent);
                EditorGUILayout.PropertyField(m_CompensateForScreenOrientation, m_CompensateForScreenOrientationContent);

                EditorGUILayout.Space();
                EditorGUILayout.Separator();
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(m_DefaultDeadzoneMin, m_DefaultDeadzoneMinContent);
                EditorGUILayout.PropertyField(m_DefaultDeadzoneMax, m_DefaultDeadzoneMaxContent);
                EditorGUILayout.PropertyField(m_DefaultButtonPressPoint, m_DefaultButtonPressPointContent);
                EditorGUILayout.PropertyField(m_DefaultTapTime, m_DefaultTapTimeContent);
                EditorGUILayout.PropertyField(m_DefaultSlowTapTime, m_DefaultSlowTapTimeContent);
                EditorGUILayout.PropertyField(m_DefaultHoldTime, m_DefaultHoldTimeContent);
                EditorGUILayout.PropertyField(m_TapRadius, m_TapRadiusContent);
                EditorGUILayout.PropertyField(m_MultiTapDelayTime, m_MultiTapDelayTimeContent);

                EditorGUILayout.Space();
                EditorGUILayout.Separator();
                EditorGUILayout.Space();

                EditorGUILayout.HelpBox("Leave 'Supported Devices' empty if you want the input system to support all input devices it can recognize. If, however, "
                    + "you are only interested in a certain set of devices, adding them here will narrow the scope of what's presented in the editor "
                    + "and avoid picking up input from devices not relevant to the project. When you add devices here, any device that will not be classified "
                    + "as support will appear under 'Unsupported Devices' in the input debugger.", MessageType.None);

                m_SupportedDevices.DoLayoutList();

                if (EditorGUI.EndChangeCheck())
                    Apply();
            }
        }

        private static void CreateNewSettingsAsset(string relativePath)
        {
            // Create settings file.
            var settings = ScriptableObject.CreateInstance<InputSettings>();
            AssetDatabase.CreateAsset(settings, relativePath);
            EditorGUIUtility.PingObject(settings);
            // Install the settings. This will lead to an InputSystem.onSettingsChange event which in turn
            // will cause us to re-initialize.
            InputSystem.settings = settings;
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
            var relativePath = "Assets/" + path.Substring(dataPath.Length);
            CreateNewSettingsAsset(relativePath);
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
                if (m_AvailableInputSettingsAssets.Length != 0)
                {
                    m_CurrentSelectedInputSettingsAsset = 0;
                    m_Settings = AssetDatabase.LoadAssetAtPath<InputSettings>(m_AvailableInputSettingsAssets[0]);
                    InputSystem.settings = m_Settings;
                }
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

                // Ugly hack: GenericMenu iterprets "/" as a submenu path. But luckily, "/" is not the only slash we have in Unicode.
                m_AvailableSettingsAssetsOptions[i] = new GUIContent(name.Replace("/", "\u29f8"));
            }

            // Look up properties.
            m_SettingsObject = new SerializedObject(m_Settings);
            m_UpdateMode = m_SettingsObject.FindProperty("m_UpdateMode");
            m_CompensateForScreenOrientation = m_SettingsObject.FindProperty("m_CompensateForScreenOrientation");
            m_FilterNoiseOnCurrent = m_SettingsObject.FindProperty("m_FilterNoiseOnCurrent");
            m_DefaultDeadzoneMin = m_SettingsObject.FindProperty("m_DefaultDeadzoneMin");
            m_DefaultDeadzoneMax = m_SettingsObject.FindProperty("m_DefaultDeadzoneMax");
            m_DefaultButtonPressPoint = m_SettingsObject.FindProperty("m_DefaultButtonPressPoint");
            m_DefaultTapTime = m_SettingsObject.FindProperty("m_DefaultTapTime");
            m_DefaultSlowTapTime = m_SettingsObject.FindProperty("m_DefaultSlowTapTime");
            m_DefaultHoldTime = m_SettingsObject.FindProperty("m_DefaultHoldTime");
            m_TapRadius = m_SettingsObject.FindProperty("m_TapRadius");
            m_MultiTapDelayTime = m_SettingsObject.FindProperty("m_MultiTapDelayTime");

            m_UpdateModeContent = new GUIContent("Update Mode", "When should the Input System be updated?");
            m_FilterNoiseOnCurrentContent = new GUIContent("Filter Noise on current", "If enabled, input from noisy controls will not cause a device to become '.current'.");
            m_CompensateForScreenOrientationContent = new GUIContent("Compensate Orientation", "Whether sensor input on mobile devices should be transformed to be relative to the current device orientation.");
            m_DefaultDeadzoneMinContent = new GUIContent("Default Deadzone Min", "Default 'min' value for Stick Deadzone and Axis Deadzone processors.");
            m_DefaultDeadzoneMaxContent = new GUIContent("Default Deadzone Max", "Default 'max' value for Stick Deadzone and Axis Deadzone processors.");
            m_DefaultButtonPressPointContent = new GUIContent("Default Button Press Point", "The default press point used for Button controls as well as for various interactions. For button controls which have analog physical inputs, this configures how far they need to   be held down to be considered 'pressed'.");
            m_DefaultTapTimeContent = new GUIContent("Default Tap Time", "Default duration to be used for Tap and MultiTap interactions. Also used by by Touch screen devices to distinguish taps from to new touches.");
            m_DefaultSlowTapTimeContent = new GUIContent("Default Slow Tap Time", "Default duration to be used for SlowTap interactions.");
            m_DefaultHoldTimeContent = new GUIContent("Default Hold Time", "Default duration to be used for Hold interactions.");
            m_TapRadiusContent = new GUIContent("Tap Radius", "Maximum distance between two finger taps on a touch screen device allowed for the system to consider this a tap of the same touch (as opposed to a new touch).");
            m_MultiTapDelayTimeContent = new GUIContent("MultiTap Delay Time", "Default delay to be allowed between taps for MultiTap interactions. Also used by by touch devices to count multi taps.");

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
                    var dropdown = new InputControlPickerDropdown(
                        new InputControlPickerState(),
                        path =>
                        {
                            var layoutName = InputControlPath.TryGetDeviceLayout(path) ?? path;
                            var existingIndex = m_Settings.supportedDevices.IndexOf(x => x == layoutName);
                            if (existingIndex != -1)
                            {
                                m_SupportedDevices.index = existingIndex;
                                return;
                            }
                            var numDevices = supportedDevicesProperty.arraySize;
                            supportedDevicesProperty.InsertArrayElementAtIndex(numDevices);
                            supportedDevicesProperty.GetArrayElementAtIndex(numDevices)
                                .stringValue = layoutName;
                            m_SupportedDevices.index = numDevices;
                            Apply();
                        },
                        mode: InputControlPicker.Mode.PickDevice);
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
            Repaint();
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
        [NonSerialized] private SerializedProperty m_CompensateForScreenOrientation;
        [NonSerialized] private SerializedProperty m_FilterNoiseOnCurrent;
        [NonSerialized] private SerializedProperty m_DefaultDeadzoneMin;
        [NonSerialized] private SerializedProperty m_DefaultDeadzoneMax;
        [NonSerialized] private SerializedProperty m_DefaultButtonPressPoint;
        [NonSerialized] private SerializedProperty m_DefaultTapTime;
        [NonSerialized] private SerializedProperty m_DefaultSlowTapTime;
        [NonSerialized] private SerializedProperty m_DefaultHoldTime;
        [NonSerialized] private SerializedProperty m_TapRadius;
        [NonSerialized] private SerializedProperty m_MultiTapDelayTime;

        [NonSerialized] private ReorderableList m_SupportedDevices;
        [NonSerialized] private string[] m_AvailableInputSettingsAssets;
        [NonSerialized] private GUIContent[] m_AvailableSettingsAssetsOptions;
        [NonSerialized] private int m_CurrentSelectedInputSettingsAsset;

        [NonSerialized] private GUIContent m_SupportedDevicesText = EditorGUIUtility.TrTextContent("Supported Devices");
        [NonSerialized] private GUIStyle m_NewAssetButtonStyle;

        GUIContent m_UpdateModeContent;
        GUIContent m_FilterNoiseOnCurrentContent;
        GUIContent m_CompensateForScreenOrientationContent;
        GUIContent m_DefaultDeadzoneMinContent;
        GUIContent m_DefaultDeadzoneMaxContent;
        GUIContent m_DefaultButtonPressPointContent;
        GUIContent m_DefaultTapTimeContent;
        GUIContent m_DefaultSlowTapTimeContent;
        GUIContent m_DefaultHoldTimeContent;
        GUIContent m_TapRadiusContent;
        GUIContent m_MultiTapDelayTimeContent;

        private static InputSettingsProvider s_Instance;

        internal static void ForceReload()
        {
            if (s_Instance != null)
            {
                // Force next OnGUI() to re-initialize.
                s_Instance.m_Settings = null;

                // Request repaint.
                SettingsService.NotifySettingsProviderChanged();
            }
        }
    }

    [CustomEditor(typeof(InputSettings))]
    internal class InputSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Space(10);
            if (GUILayout.Button("Open Input Settings Window", GUILayout.Height(30)))
                InputSettingsProvider.Open();
            GUILayout.Space(10);

            if (InputSystem.settings == target)
                EditorGUILayout.HelpBox("This asset contains the currently active settings for the Input System.", MessageType.Info);
            else
            {
                string currentlyActiveAssetsPath = null;
                if (InputSystem.settings != null)
                    currentlyActiveAssetsPath = AssetDatabase.GetAssetPath(InputSystem.settings);
                if (!string.IsNullOrEmpty(currentlyActiveAssetsPath))
                    currentlyActiveAssetsPath = $"The currently active settings are stored in {currentlyActiveAssetsPath}. ";
                EditorGUILayout.HelpBox($"Note that this asset does not contain the currently active settings for the Input System. {currentlyActiveAssetsPath??""}Click \"Make Active\" below to make {target.name} the active one.", MessageType.Warning);
                if (GUILayout.Button($"Make active", EditorStyles.miniButton))
                    InputSystem.settings = (InputSettings)target;
            }
        }
    }
}
#endif // UNITY_EDITOR
