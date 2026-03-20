#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

#pragma warning disable CS0414
namespace UnityEngine.InputSystem.Editor
{
    internal static class InputSettingsPath
    {
        public const string kSettingsRootPath = "Project/Input System Package";
    }

    internal class InputSettingsProvider : SettingsProvider, IDisposable
    {
        public const string kEditorBuildSettingsConfigKey = "com.unity.input.settings";

        public const string kSettingsPath = InputSettingsPath.kSettingsRootPath + "/Settings";

        private static readonly string[] kInputSettingsKeywords =
        {
            "Input", "Action", "Controls", "Gamepad", "Keyboard", "Mouse", "Touch"
        };

        public static void Open()
        {
            SettingsService.OpenProjectSettings(kSettingsPath);
        }

        [SettingsProvider]
        public static SettingsProvider CreateInputSettingsProvider()
        {
            return new InputSettingsProvider(kSettingsPath, SettingsScope.Project)
            {
                // We put this in a child node called "Settings" when Project-wide Actions is enabled.
                // When not enabled it sits on the main package Settings node.
                label = "Settings",
                keywords = kInputSettingsKeywords
            };
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
            m_RootElement = rootElement;
            InputSystem.onSettingsChange += OnSettingsChange;
            Undo.undoRedoPerformed += OnUndoRedo;
            BuildUI();
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            m_RootElement = null;
            InputSystem.onSettingsChange -= OnSettingsChange;
            Undo.undoRedoPerformed -= OnUndoRedo;
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

        private void BuildUI()
        {
            if (m_RootElement == null)
                return;

            InitializeWithCurrentSettingsIfNecessary();
            m_RootElement.Clear();

            m_CreateSettingsAssetContainer = new VisualElement();
            m_CreateSettingsAssetContainer.style.marginBottom = 12;
            m_RootElement.Add(m_CreateSettingsAssetContainer);

            m_CreateSettingsAssetHelpBox = new HelpBox(
                "Settings for the new input system are stored in an asset. Click the button below to create a settings asset you can edit.",
                HelpBoxMessageType.Info);
            m_CreateSettingsAssetContainer.Add(m_CreateSettingsAssetHelpBox);

            m_CreateSettingsAssetButton = new Button(() => CreateNewSettingsAsset("Assets/InputSystem.inputsettings.asset"))
            {
                text = "Create settings asset"
            };
            m_CreateSettingsAssetButton.style.marginTop = 6;
            m_CreateSettingsAssetButton.style.height = 30;
            m_CreateSettingsAssetContainer.Add(m_CreateSettingsAssetButton);

            var titleLabel = new Label("Settings");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 19;
            titleLabel.style.marginBottom = 12;
            m_RootElement.Add(titleLabel);

            m_HeaderContainer = new VisualElement();
            m_RootElement.Add(m_HeaderContainer);

            m_UpdateModeDropdown = CreateEnumDropdown(
                () => m_UpdateMode,
                m_UpdateModeContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_UpdateModeDropdown);

            m_UpdateModeHelpContainer = new VisualElement();

            m_UpdateModeHelpBox = new HelpBox(
                "This is not recommended, the default update mode is dynamic update and should only be changed for compelling reasons. Please refer to the documentation.",
                HelpBoxMessageType.Warning);
            m_UpdateModeHelpContainer.Add(m_UpdateModeHelpBox);

            m_UpdateModeReadMoreButton = new Button(OpenUpdateModeDocumentation)
            {
                text = "Read more"
            };
            m_UpdateModeHelpContainer.Add(m_UpdateModeReadMoreButton);
            m_HeaderContainer.Add(m_UpdateModeHelpContainer);

            m_BackgroundBehaviorDropdown = CreateEnumDropdown(
                () => m_BackgroundBehavior,
                m_BackgroundBehaviorContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_BackgroundBehaviorDropdown);

            m_BackgroundBehaviorHelpBox = new HelpBox(
                "Focus change behavior can only be changed if 'Run In Background' is enabled in Player Settings.",
                HelpBoxMessageType.Info);
            m_HeaderContainer.Add(m_BackgroundBehaviorHelpBox);

#if UNITY_INPUT_SYSTEM_PLATFORM_SCROLL_DELTA
            m_ScrollDeltaBehaviorDropdown = CreateEnumDropdown(
                () => m_ScrollDeltaBehavior,
                m_ScrollDeltaBehaviorContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_ScrollDeltaBehaviorDropdown);
#endif

            m_CompensateForScreenOrientationToggle = CreateToggle(
                () => m_CompensateForScreenOrientation,
                m_CompensateForScreenOrientationContent,
                RefreshUIToolkitHeaderState);
            m_CompensateForScreenOrientationToggle.style.marginTop = 12;
            m_CompensateForScreenOrientationToggle.style.marginBottom = 12;
            m_HeaderContainer.Add(m_CompensateForScreenOrientationToggle);

            m_DefaultDeadzoneMinField = CreateFloatField(
                () => m_DefaultDeadzoneMin,
                m_DefaultDeadzoneMinContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_DefaultDeadzoneMinField);

            m_DefaultDeadzoneMaxField = CreateFloatField(
                () => m_DefaultDeadzoneMax,
                m_DefaultDeadzoneMaxContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_DefaultDeadzoneMaxField);

            m_DefaultButtonPressPointField = CreateFloatField(
                () => m_DefaultButtonPressPoint,
                m_DefaultButtonPressPointContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_DefaultButtonPressPointField);

            m_ButtonReleaseThresholdField = CreateFloatField(
                () => m_ButtonReleaseThreshold,
                m_ButtonReleaseThresholdContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_ButtonReleaseThresholdField);

            m_DefaultTapTimeField = CreateFloatField(
                () => m_DefaultTapTime,
                m_DefaultTapTimeContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_DefaultTapTimeField);

            m_DefaultSlowTapTimeField = CreateFloatField(
                () => m_DefaultSlowTapTime,
                m_DefaultSlowTapTimeContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_DefaultSlowTapTimeField);

            m_DefaultHoldTimeField = CreateFloatField(
                () => m_DefaultHoldTime,
                m_DefaultHoldTimeContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_DefaultHoldTimeField);

            m_TapRadiusField = CreateFloatField(
                () => m_TapRadius,
                m_TapRadiusContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_TapRadiusField);

            m_MultiTapDelayTimeField = CreateFloatField(
                () => m_MultiTapDelayTime,
                m_MultiTapDelayTimeContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_MultiTapDelayTimeField);

            m_SupportedDevicesHelpBox = new HelpBox(
                "Leave 'Supported Devices' empty if you want the input system to support all input devices it can recognize. If, however, "
                + "you are only interested in a certain set of devices, adding them here will narrow the scope of what's presented in the editor "
                + "and avoid picking up input from devices not relevant to the project. When you add devices here, any device that will not be classified "
                + "as supported will appear under 'Unsupported Devices' in the input debugger.",
                HelpBoxMessageType.None);
            m_SupportedDevicesHelpBox.style.marginTop = 48;
            m_HeaderContainer.Add(m_SupportedDevicesHelpBox);

            var supportedDevicesTitleLabel = new Label("Supported Devices");
            supportedDevicesTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            supportedDevicesTitleLabel.style.marginTop = 6;
            m_HeaderContainer.Add(supportedDevicesTitleLabel);

            m_SupportedDevicesListView = new ListView
            {
                selectionType = UIElements.SelectionType.Single,
                reorderable = true,
                showBorder = true,
                fixedItemHeight = 22
            };
            m_SupportedDevicesListView.makeItem = MakeSupportedDevicesItem;
            m_SupportedDevicesListView.bindItem = BindSupportedDevicesItem;
            m_SupportedDevicesListView.itemIndexChanged += OnSupportedDevicesReordered;
            m_SupportedDevicesListView.selectionChanged += _ => RefreshSupportedDevicesButtonsState();
            m_HeaderContainer.Add(m_SupportedDevicesListView);

            var supportedDevicesButtonsContainer = new VisualElement();
            supportedDevicesButtonsContainer.style.flexDirection = FlexDirection.Row;
            supportedDevicesButtonsContainer.style.justifyContent = Justify.FlexEnd;
            supportedDevicesButtonsContainer.style.marginTop = 4;
            m_HeaderContainer.Add(supportedDevicesButtonsContainer);

            m_AddSupportedDeviceButton = new Button(AddSupportedDevice)
            {
                text = "Add"
            };
            supportedDevicesButtonsContainer.Add(m_AddSupportedDeviceButton);

            m_RemoveSupportedDeviceButton = new Button(RemoveSupportedDevice)
            {
                text = "Remove"
            };
            m_RemoveSupportedDeviceButton.style.marginLeft = 4;
            supportedDevicesButtonsContainer.Add(m_RemoveSupportedDeviceButton);

            m_iOSProvider.CreateGUI(m_HeaderContainer, () =>
            {
                Apply();
                RefreshUIToolkitHeaderState();
            });

            var editorTitleLabel = new Label("Editor");
            editorTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            editorTitleLabel.style.marginTop = 12;
            m_HeaderContainer.Add(editorTitleLabel);

            m_EditorInputBehaviorInPlayModeDropdown = CreateEnumDropdown(
                () => m_EditorInputBehaviorInPlayMode,
                m_EditorInputBehaviorInPlayModeContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_EditorInputBehaviorInPlayModeDropdown);

            var shortcutSupportTitleLabel = new Label("Improved Shortcut Support");
            shortcutSupportTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            shortcutSupportTitleLabel.style.marginTop = 12;
            m_HeaderContainer.Add(shortcutSupportTitleLabel);

            m_ShortcutKeysConsumeInputsToggle = CreateToggle(
                () => m_ShortcutKeysConsumeInputs,
                m_ShortcutKeysConsumeInputsContent,
                RefreshUIToolkitHeaderState);
            m_HeaderContainer.Add(m_ShortcutKeysConsumeInputsToggle);

            m_ShortcutKeysConsumeInputsHelpBox = new HelpBox(
                "Please note that enabling Improved Shortcut Support will cause actions with composite bindings to consume input and block any other actions which are enabled and sharing the same controls. "
                + "Input consumption is performed in priority order, with the action containing the greatest number of bindings checked first. "
                + "Therefore actions requiring fewer keypresses will not be triggered if an action using more keypresses is triggered and has overlapping controls. "
                + "This works for shortcut keys, however in other cases this might not give the desired result, especially where there are actions with the exact same number of composite controls, in which case it is non-deterministic which action will be triggered. "
                + "These conflicts may occur even between actions which belong to different Action Maps e.g. if using an UIInputModule with the Arrow Keys bound to the Navigate Action in the UI Action Map, this would interfere with other Action Maps using those keys. "
                + "However conflicts would not occur between actions which belong to different Action Assets. "
                + "Since event consumption only occurs for enabled actions, you can resolve unexpected issues by ensuring that only those Actions or Action Maps that are relevant to your game's current context are enabled. Enabling or disabling actions as your game or application moves between different contexts. ",
                HelpBoxMessageType.None);
            m_HeaderContainer.Add(m_ShortcutKeysConsumeInputsHelpBox);

            RefreshUIToolkitHeaderState();
        }

        private DropdownField CreateEnumDropdown(Func<SerializedProperty> propertyAccessor, GUIContent content, Action onValueChanged)
        {
            var dropdown = new DropdownField(content.text)
            {
                tooltip = content.tooltip
            };
            dropdown.RegisterValueChangedCallback(evt =>
            {
                var property = propertyAccessor();
                if (property == null)
                    return;

                var newIndex = dropdown.choices?.IndexOf(evt.newValue) ?? -1;
                if (newIndex == -1 || property.enumValueIndex == newIndex)
                    return;

                property.enumValueIndex = newIndex;
                Apply();
                onValueChanged?.Invoke();
            });

            return dropdown;
        }

        private Toggle CreateToggle(Func<SerializedProperty> propertyAccessor, GUIContent content, Action onValueChanged)
        {
            var toggle = new Toggle(content.text)
            {
                tooltip = content.tooltip
            };
            toggle.RegisterValueChangedCallback(evt =>
            {
                var property = propertyAccessor();
                if (property == null || property.boolValue == evt.newValue)
                    return;

                property.boolValue = evt.newValue;
                Apply();
                onValueChanged?.Invoke();
            });

            return toggle;
        }

        private FloatField CreateFloatField(Func<SerializedProperty> propertyAccessor, GUIContent content, Action onValueChanged)
        {
            var field = new FloatField(content.text)
            {
                tooltip = content.tooltip
            };
            field.RegisterValueChangedCallback(evt =>
            {
                var property = propertyAccessor();
                if (property == null || Mathf.Approximately(property.floatValue, evt.newValue))
                    return;

                property.floatValue = evt.newValue;
                Apply();
                onValueChanged?.Invoke();
            });

            return field;
        }

        private VisualElement MakeSupportedDevicesItem()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var icon = new Image
            {
                name = "icon",
                scaleMode = ScaleMode.ScaleToFit
            };
            icon.style.width = 20;
            icon.style.height = 20;
            icon.style.marginRight = 4;
            row.Add(icon);

            var label = new Label
            {
                name = "label"
            };
            label.style.flexGrow = 1;
            row.Add(label);

            return row;
        }

        private void BindSupportedDevicesItem(VisualElement element, int index)
        {
            var icon = element.Q<Image>("icon");
            var label = element.Q<Label>("label");
            var layoutName = m_Settings != null && index >= 0 && index < m_Settings.supportedDevices.Count
                ? m_Settings.supportedDevices[index]
                : string.Empty;

            if (icon != null)
                icon.image = string.IsNullOrEmpty(layoutName) ? null : EditorInputControlLayoutCache.GetIconForLayout(layoutName);
            if (label != null)
                label.text = layoutName;
        }

        private void AddSupportedDevice()
        {
            if (m_SupportedDevicesProperty == null)
                return;

            var dropdown = new InputControlPickerDropdown(
                new InputControlPickerState(),
                path =>
                {
                    ////REVIEW: Why are we converting from a layout into a plain string here instead of just using path strings in supportedDevices?
                    ////        Why not just have InputSettings.supportedDevices be a list of paths?
                    var layoutName = InputControlPath.TryGetDeviceLayout(path) ?? path;
                    var existingIndex = m_Settings.supportedDevices.IndexOf(x => x == layoutName);
                    if (existingIndex != -1)
                    {
                        m_SupportedDevicesListView?.SetSelection(existingIndex);
                        RefreshSupportedDevicesButtonsState();
                        return;
                    }

                    var numDevices = m_SupportedDevicesProperty.arraySize;
                    m_SupportedDevicesProperty.InsertArrayElementAtIndex(numDevices);
                    m_SupportedDevicesProperty.GetArrayElementAtIndex(numDevices).stringValue = layoutName;
                    Apply();
                    RefreshUIToolkitHeaderState();
                    m_SupportedDevicesListView?.SetSelection(numDevices);
                },
                mode: InputControlPicker.Mode.PickDevice);

            var buttonRect = m_AddSupportedDeviceButton?.worldBound ?? default;
            dropdown.Show(buttonRect);
        }

        private void RemoveSupportedDevice()
        {
            if (m_SupportedDevicesProperty == null || m_SupportedDevicesListView == null)
                return;

            var index = m_SupportedDevicesListView.selectedIndex;
            if (index < 0 || index >= m_SupportedDevicesProperty.arraySize)
                return;

            m_SupportedDevicesProperty.DeleteArrayElementAtIndex(index);
            Apply();
            RefreshUIToolkitHeaderState();

            if (m_SupportedDevicesProperty.arraySize > 0)
                m_SupportedDevicesListView.SetSelection(Mathf.Min(index, m_SupportedDevicesProperty.arraySize - 1));
        }

        private void OnSupportedDevicesReordered(int oldIndex, int newIndex)
        {
            if (m_SupportedDevicesProperty == null || oldIndex == newIndex)
                return;

            m_SupportedDevicesProperty.MoveArrayElement(oldIndex, newIndex);
            Apply();
            RefreshUIToolkitHeaderState();
            m_SupportedDevicesListView?.SetSelection(newIndex);
        }

        private void RefreshSupportedDevicesButtonsState()
        {
            var hasSettingsAsset = m_AvailableInputSettingsAssets != null && m_AvailableInputSettingsAssets.Length != 0;
            var canEditSettings = m_SettingsObject != null && hasSettingsAsset;

            if (m_SupportedDevicesListView != null)
                m_SupportedDevicesListView.SetEnabled(canEditSettings);
            if (m_AddSupportedDeviceButton != null)
                m_AddSupportedDeviceButton.SetEnabled(canEditSettings);
            if (m_RemoveSupportedDeviceButton != null)
                m_RemoveSupportedDeviceButton.SetEnabled(canEditSettings && m_SupportedDevicesListView != null && m_SupportedDevicesListView.selectedIndex >= 0);
        }

        private void RefreshSupportedDevicesList()
        {
            if (m_SupportedDevicesListView == null)
                return;

            m_SupportedDevicesListItems ??= new System.Collections.Generic.List<string>();
            m_SupportedDevicesListItems.Clear();

            if (m_Settings != null)
                m_SupportedDevicesListItems.AddRange(m_Settings.supportedDevices);

            m_SupportedDevicesListView.itemsSource = m_SupportedDevicesListItems;
            m_SupportedDevicesListView.Rebuild();
            RefreshSupportedDevicesButtonsState();
        }

        private void RefreshUIToolkitHeaderState()
        {
            if (m_HeaderContainer == null)
                return;

            var hasSettings = m_SettingsObject != null;
            var hasSettingsAsset = m_AvailableInputSettingsAssets != null && m_AvailableInputSettingsAssets.Length != 0;
            var canEditSettings = hasSettings && hasSettingsAsset;

            if (m_CreateSettingsAssetContainer != null)
                m_CreateSettingsAssetContainer.style.display = hasSettingsAsset ? DisplayStyle.None : DisplayStyle.Flex;

            UpdateDropdownChoices(m_UpdateModeDropdown, m_UpdateMode);
            if (m_UpdateModeDropdown != null)
                m_UpdateModeDropdown.SetEnabled(canEditSettings && m_UpdateMode != null);

            var showManualUpdateModeHelp = hasSettings &&
                m_UpdateMode != null &&
                m_UpdateMode.intValue == (int)InputSettings.UpdateMode.ProcessEventsManually;
            if (m_UpdateModeHelpContainer != null)
                m_UpdateModeHelpContainer.style.display = showManualUpdateModeHelp ? DisplayStyle.Flex : DisplayStyle.None;

            UpdateDropdownChoices(m_BackgroundBehaviorDropdown, m_BackgroundBehavior);
            if (m_BackgroundBehaviorDropdown != null)
                m_BackgroundBehaviorDropdown.SetEnabled(canEditSettings && Application.runInBackground && m_BackgroundBehavior != null);

            if (m_BackgroundBehaviorHelpBox != null)
            {
                var showRunInBackgroundHelp = hasSettings && !Application.runInBackground;
                m_BackgroundBehaviorHelpBox.style.display = showRunInBackgroundHelp ? DisplayStyle.Flex : DisplayStyle.None;
            }

#if UNITY_INPUT_SYSTEM_PLATFORM_SCROLL_DELTA
            UpdateDropdownChoices(m_ScrollDeltaBehaviorDropdown, m_ScrollDeltaBehavior);
            if (m_ScrollDeltaBehaviorDropdown != null)
                m_ScrollDeltaBehaviorDropdown.SetEnabled(canEditSettings && m_ScrollDeltaBehavior != null);
#endif

            if (m_CompensateForScreenOrientationToggle != null)
            {
                m_CompensateForScreenOrientationToggle.SetEnabled(canEditSettings && m_CompensateForScreenOrientation != null);
                m_CompensateForScreenOrientationToggle.SetValueWithoutNotify(m_CompensateForScreenOrientation?.boolValue ?? false);
            }

            UpdateFloatField(m_DefaultDeadzoneMinField, m_DefaultDeadzoneMin, canEditSettings);
            UpdateFloatField(m_DefaultDeadzoneMaxField, m_DefaultDeadzoneMax, canEditSettings);
            UpdateFloatField(m_DefaultButtonPressPointField, m_DefaultButtonPressPoint, canEditSettings);
            UpdateFloatField(m_ButtonReleaseThresholdField, m_ButtonReleaseThreshold, canEditSettings);
            UpdateFloatField(m_DefaultTapTimeField, m_DefaultTapTime, canEditSettings);
            UpdateFloatField(m_DefaultSlowTapTimeField, m_DefaultSlowTapTime, canEditSettings);
            UpdateFloatField(m_DefaultHoldTimeField, m_DefaultHoldTime, canEditSettings);
            UpdateFloatField(m_TapRadiusField, m_TapRadius, canEditSettings);
            UpdateFloatField(m_MultiTapDelayTimeField, m_MultiTapDelayTime, canEditSettings);
            RefreshSupportedDevicesList();

            UpdateDropdownChoices(m_EditorInputBehaviorInPlayModeDropdown, m_EditorInputBehaviorInPlayMode);
            if (m_EditorInputBehaviorInPlayModeDropdown != null)
                m_EditorInputBehaviorInPlayModeDropdown.SetEnabled(canEditSettings && m_EditorInputBehaviorInPlayMode != null);

            if (m_ShortcutKeysConsumeInputsToggle != null)
            {
                m_ShortcutKeysConsumeInputsToggle.SetEnabled(canEditSettings && m_ShortcutKeysConsumeInputs != null);
                m_ShortcutKeysConsumeInputsToggle.SetValueWithoutNotify(m_ShortcutKeysConsumeInputs?.boolValue ?? false);
            }

            if (m_ShortcutKeysConsumeInputsHelpBox != null)
                m_ShortcutKeysConsumeInputsHelpBox.style.display =
                    m_ShortcutKeysConsumeInputs != null && m_ShortcutKeysConsumeInputs.boolValue
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            m_iOSProvider?.RefreshUIToolkitState(canEditSettings);
        }

        private static void UpdateDropdownChoices(DropdownField dropdown, SerializedProperty property)
        {
            if (dropdown == null)
                return;

            if (property == null)
            {
                dropdown.choices = Array.Empty<string>().ToList();
                dropdown.SetValueWithoutNotify(string.Empty);
                return;
            }

            dropdown.choices = property.enumDisplayNames.ToList();
            if (property.enumValueIndex >= 0 && property.enumValueIndex < dropdown.choices.Count)
                dropdown.SetValueWithoutNotify(dropdown.choices[property.enumValueIndex]);
        }

        private static void UpdateFloatField(FloatField field, SerializedProperty property, bool canEditSettings)
        {
            if (field == null)
                return;

            field.SetEnabled(canEditSettings && property != null);
            field.SetValueWithoutNotify(property?.floatValue ?? 0f);
        }

        private static void OpenUpdateModeDocumentation()
        {
            var link = new Uri(InputSystem.kDocUrl + "/manual/Settings.html#update-mode");
            System.Diagnostics.Process.Start(link.AbsoluteUri);
        }

        private static void ShowPlatformSettings()
        {
            // Would be nice to get BuildTargetDiscovery.GetBuildTargetInfoList since that contains information about icons etc
        }

        private static void CreateNewSettingsAsset(string relativePath)
        {
            // Create and install the settings. This will lead to an InputSystem.onSettingsChange event which in turn
            // will cause us to re-initialize.
            InputSystem.settings = InputAssetEditorUtils.CreateAsset(ScriptableObject.CreateInstance<InputSettings>(), relativePath);
        }

        private static void CreateNewSettingsAsset()
        {
            var result = InputAssetEditorUtils.PromptUserForAsset(
                friendlyName: "Input Settings",
                suggestedAssetFilePathWithoutExtension: InputAssetEditorUtils.MakeProjectFileName("inputsettings"),
                assetFileExtension: "asset");
            if (result.result == InputAssetEditorUtils.DialogResult.Valid)
                CreateNewSettingsAsset(result.relativePath);
        }

        private void InitializeWithCurrentSettingsIfNecessary()
        {
            if (InputSystem.settings == m_Settings && m_Settings != null && m_SettingsDirtyCount == EditorUtility.GetDirtyCount(m_Settings))
                return;

            InitializeWithCurrentSettings();
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
            m_SettingsDirtyCount = EditorUtility.GetDirtyCount(m_Settings);
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

                // Ugly hack: GenericMenu interprets "/" as a submenu path. But luckily, "/" is not the only slash we have in Unicode.
                m_AvailableSettingsAssetsOptions[i] = new GUIContent(name.Replace("/", "\u29f8"));
            }

            // Look up properties.
            m_SettingsObject = new SerializedObject(m_Settings);
            m_UpdateMode = m_SettingsObject.FindProperty("m_UpdateMode");
            m_ScrollDeltaBehavior = m_SettingsObject.FindProperty("m_ScrollDeltaBehavior");
            m_CompensateForScreenOrientation = m_SettingsObject.FindProperty("m_CompensateForScreenOrientation");
            m_BackgroundBehavior = m_SettingsObject.FindProperty("m_BackgroundBehavior");
            m_EditorInputBehaviorInPlayMode = m_SettingsObject.FindProperty("m_EditorInputBehaviorInPlayMode");
            m_DefaultDeadzoneMin = m_SettingsObject.FindProperty("m_DefaultDeadzoneMin");
            m_DefaultDeadzoneMax = m_SettingsObject.FindProperty("m_DefaultDeadzoneMax");
            m_DefaultButtonPressPoint = m_SettingsObject.FindProperty("m_DefaultButtonPressPoint");
            m_ButtonReleaseThreshold = m_SettingsObject.FindProperty("m_ButtonReleaseThreshold");
            m_DefaultTapTime = m_SettingsObject.FindProperty("m_DefaultTapTime");
            m_DefaultSlowTapTime = m_SettingsObject.FindProperty("m_DefaultSlowTapTime");
            m_DefaultHoldTime = m_SettingsObject.FindProperty("m_DefaultHoldTime");
            m_TapRadius = m_SettingsObject.FindProperty("m_TapRadius");
            m_MultiTapDelayTime = m_SettingsObject.FindProperty("m_MultiTapDelayTime");
            m_ShortcutKeysConsumeInputs = m_SettingsObject.FindProperty("m_ShortcutKeysConsumeInputs");

            m_UpdateModeContent = new GUIContent("Update Mode", "When should the Input System be updated?");
#if UNITY_INPUT_SYSTEM_PLATFORM_SCROLL_DELTA
            m_ScrollDeltaBehaviorContent = new GUIContent("Scroll Delta Behavior", "Controls whether the value returned by the Scroll Wheel Delta is normalized (to be uniform across all platforms), or returns the non-normalized platform-specific range which can vary between platforms.");
#endif
            m_CompensateForScreenOrientationContent = new GUIContent("Compensate Orientation", "Whether sensor input on mobile devices should be transformed to be relative to the current device orientation.");
            m_BackgroundBehaviorContent = new GUIContent("Background Behavior", "If runInBackground is true (and in standalone *development* players and the editor), "
                + "determines what happens to InputDevices and events when the application moves in and out of running in the foreground.\n\n"
                + "'Reset And Disable Non-Background Devices' soft-resets and disables devices that cannot run in the background while the application does not have focus. Devices "
                + "that can run in the background remain enabled and will keep receiving input.\n"
                + "'Reset And Disable All Devices' soft-resets and disables *all* devices while the application does not have focus. No device will receive input while the application "
                + "is running in the background.\n"
                + "'Ignore Focus' leaves all devices untouched when application focus changes. While running in the background, all input that is received is processed as if "
                + "running in the foreground.");
            m_EditorInputBehaviorInPlayModeContent = new GUIContent("Play Mode Input Behavior", "When in play mode, determines how focus of the Game View is handled with respect to input.\n\n"
                + "'Pointers And Keyboards Respect Game View Focus' requires Game View focus only for pointers (mice, touch, etc.) and keyboards. Other devices will feed input to the game regardless "
                + "of whether the Game View is focused or not. Note that this means that input on these devices is not visible in other EditorWindows.\n"
                + "'All Devices Respect Game View Focus' requires Game View focus for all input devices. While focus is not on the Game View, all input on InputDevices will go to the editor and not "
                + "the game.\n"
                + "'All Device Input Always Goes To Game View' causes input to treat 'Background Behavior' exactly as in the player including devices potentially being disabled entirely while the Game View "
                + "does not have focus. In this setting, no input from the Input System will be visible to EditorWindows.");
            m_DefaultDeadzoneMinContent = new GUIContent("Default Deadzone Min", "Default 'min' value for Stick Deadzone and Axis Deadzone processors.");
            m_DefaultDeadzoneMaxContent = new GUIContent("Default Deadzone Max", "Default 'max' value for Stick Deadzone and Axis Deadzone processors.");
            m_DefaultButtonPressPointContent = new GUIContent("Default Button Press Point", "The default press point used for Button controls as well as for various interactions. For button controls which have analog physical inputs, this configures how far they need to   be held down to be considered 'pressed'.");
            m_ButtonReleaseThresholdContent = new GUIContent("Button Release Threshold", "Percent of press point at which a Button is considered released again. At 1, release points are identical to press points. At 0, a Button must be fully released before it can be pressed again.");
            m_DefaultTapTimeContent = new GUIContent("Default Tap Time", "Default duration to be used for Tap and MultiTap interactions. Also used by by Touch screen devices to distinguish taps from to new touches.");
            m_DefaultSlowTapTimeContent = new GUIContent("Default Slow Tap Time", "Default duration to be used for SlowTap interactions.");
            m_DefaultHoldTimeContent = new GUIContent("Default Hold Time", "Default duration to be used for Hold interactions.");
            m_TapRadiusContent = new GUIContent("Tap Radius", "Maximum distance between two finger taps on a touch screen device allowed for the system to consider this a tap of the same touch (as opposed to a new touch).");
            m_MultiTapDelayTimeContent = new GUIContent("MultiTap Delay Time", "Default delay to be allowed between taps for MultiTap interactions. Also used by by touch devices to count multi taps.");
            m_ShortcutKeysConsumeInputsContent = new GUIContent("Enable Input Consumption", "Actions are exclusively triggered and will consume/block other actions sharing the same input. E.g. when pressing the 'Shift+B' keys, the associated action would trigger but any action bound to just the 'B' key would be prevented from triggering at the same time.");
            m_SupportedDevicesProperty = m_SettingsObject.FindProperty("m_SupportedDevices");


            if (m_iOSProvider == null)
                m_iOSProvider = new InputSettingsiOSProvider(m_SettingsObject);
            else
                m_iOSProvider.Update(m_SettingsObject);
        }

        private void Apply()
        {
            Debug.Assert(m_Settings != null);

            m_SettingsObject.ApplyModifiedProperties();
            m_SettingsObject.Update();
            m_Settings.OnChange();
        }

        private void OnUndoRedo()
        {
            if (m_Settings != null && EditorUtility.GetDirtyCount(m_Settings) != m_SettingsDirtyCount)
                m_Settings.OnChange();
            InitializeWithCurrentSettingsIfNecessary();
            RefreshUIToolkitHeaderState();
        }

        private void OnSettingsChange()
        {
            InitializeWithCurrentSettingsIfNecessary();
            RefreshUIToolkitHeaderState();

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

        [NonSerialized] private int m_SettingsDirtyCount;
        [NonSerialized] private SerializedObject m_SettingsObject;
        [NonSerialized] private SerializedProperty m_UpdateMode;
        [NonSerialized] private SerializedProperty m_ScrollDeltaBehavior;
        [NonSerialized] private SerializedProperty m_CompensateForScreenOrientation;
        [NonSerialized] private SerializedProperty m_BackgroundBehavior;
        [NonSerialized] private SerializedProperty m_EditorInputBehaviorInPlayMode;
        [NonSerialized] private SerializedProperty m_DefaultDeadzoneMin;
        [NonSerialized] private SerializedProperty m_DefaultDeadzoneMax;
        [NonSerialized] private SerializedProperty m_DefaultButtonPressPoint;
        [NonSerialized] private SerializedProperty m_ButtonReleaseThreshold;
        [NonSerialized] private SerializedProperty m_DefaultTapTime;
        [NonSerialized] private SerializedProperty m_DefaultSlowTapTime;
        [NonSerialized] private SerializedProperty m_DefaultHoldTime;
        [NonSerialized] private SerializedProperty m_TapRadius;
        [NonSerialized] private SerializedProperty m_MultiTapDelayTime;
        [NonSerialized] private SerializedProperty m_ShortcutKeysConsumeInputs;
        [NonSerialized] private SerializedProperty m_SupportedDevicesProperty;

        [NonSerialized] private string[] m_AvailableInputSettingsAssets;
        [NonSerialized] private GUIContent[] m_AvailableSettingsAssetsOptions;
        [NonSerialized] private int m_CurrentSelectedInputSettingsAsset;

        private GUIContent m_UpdateModeContent;
#if UNITY_INPUT_SYSTEM_PLATFORM_SCROLL_DELTA
        private GUIContent m_ScrollDeltaBehaviorContent;
#endif
        private GUIContent m_CompensateForScreenOrientationContent;
        private GUIContent m_BackgroundBehaviorContent;
        private GUIContent m_EditorInputBehaviorInPlayModeContent;
        private GUIContent m_DefaultDeadzoneMinContent;
        private GUIContent m_DefaultDeadzoneMaxContent;
        private GUIContent m_DefaultButtonPressPointContent;
        private GUIContent m_ButtonReleaseThresholdContent;
        private GUIContent m_DefaultTapTimeContent;
        private GUIContent m_DefaultSlowTapTimeContent;
        private GUIContent m_DefaultHoldTimeContent;
        private GUIContent m_TapRadiusContent;
        private GUIContent m_MultiTapDelayTimeContent;
        private GUIContent m_ShortcutKeysConsumeInputsContent;

        [NonSerialized] private InputSettingsiOSProvider m_iOSProvider;
        [NonSerialized] private VisualElement m_RootElement;
        [NonSerialized] private VisualElement m_CreateSettingsAssetContainer;
        [NonSerialized] private VisualElement m_HeaderContainer;
        [NonSerialized] private VisualElement m_UpdateModeHelpContainer;
        [NonSerialized] private DropdownField m_UpdateModeDropdown;
        [NonSerialized] private DropdownField m_BackgroundBehaviorDropdown;
#if UNITY_INPUT_SYSTEM_PLATFORM_SCROLL_DELTA
        [NonSerialized] private DropdownField m_ScrollDeltaBehaviorDropdown;
#endif
        [NonSerialized] private DropdownField m_EditorInputBehaviorInPlayModeDropdown;
        [NonSerialized] private ListView m_SupportedDevicesListView;
        [NonSerialized] private Button m_AddSupportedDeviceButton;
        [NonSerialized] private Button m_RemoveSupportedDeviceButton;
        [NonSerialized] private Toggle m_CompensateForScreenOrientationToggle;
        [NonSerialized] private Toggle m_ShortcutKeysConsumeInputsToggle;
        [NonSerialized] private FloatField m_DefaultDeadzoneMinField;
        [NonSerialized] private FloatField m_DefaultDeadzoneMaxField;
        [NonSerialized] private FloatField m_DefaultButtonPressPointField;
        [NonSerialized] private FloatField m_ButtonReleaseThresholdField;
        [NonSerialized] private FloatField m_DefaultTapTimeField;
        [NonSerialized] private FloatField m_DefaultSlowTapTimeField;
        [NonSerialized] private FloatField m_DefaultHoldTimeField;
        [NonSerialized] private FloatField m_TapRadiusField;
        [NonSerialized] private FloatField m_MultiTapDelayTimeField;
        [NonSerialized] private HelpBox m_CreateSettingsAssetHelpBox;
        [NonSerialized] private Button m_CreateSettingsAssetButton;
        [NonSerialized] private HelpBox m_UpdateModeHelpBox;
        [NonSerialized] private Button m_UpdateModeReadMoreButton;
        [NonSerialized] private HelpBox m_BackgroundBehaviorHelpBox;
        [NonSerialized] private HelpBox m_ShortcutKeysConsumeInputsHelpBox;
        [NonSerialized] private HelpBox m_SupportedDevicesHelpBox;
        [NonSerialized] private System.Collections.Generic.List<string> m_SupportedDevicesListItems;

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
            EditorGUILayout.Space();

            if (GUILayout.Button("Open Input Settings Window", GUILayout.Height(30)))
                InputSettingsProvider.Open();

            EditorGUILayout.Space();

            InputAssetEditorUtils.DrawMakeActiveGui(InputSystem.settings, target as InputSettings,
                target.name, "settings", (value) => InputSystem.settings = value);
        }

        protected override bool ShouldHideOpenButton()
        {
            return true;
        }
    }
}
#endif // UNITY_EDITOR
