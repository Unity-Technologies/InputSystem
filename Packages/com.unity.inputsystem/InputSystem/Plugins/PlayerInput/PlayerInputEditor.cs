#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Utilities;

#if UNITY_INPUT_SYSTEM_ENABLE_UI
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.UI.Editor;
#endif

////TODO: detect if new input system isn't enabled and provide UI to enable it
#pragma warning disable 0414
namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A custom inspector for the <see cref="PlayerInput"/> component.
    /// </summary>
    [CustomEditor(typeof(PlayerInput))]
    internal class PlayerInputEditor : UnityEditor.Editor
    {
        public const string kDefaultInputActionsAssetPath =
            "Packages/com.unity.inputsystem/InputSystem/Plugins/PlayerInput/DefaultInputActions.inputactions";

        public void OnEnable()
        {
            InputActionImporter.onImport += Refresh;
            InputUser.onChange += OnUserChange;

            // Look up properties.
            m_ActionsProperty = serializedObject.FindProperty(nameof(PlayerInput.m_Actions));
            m_DefaultControlSchemeProperty = serializedObject.FindProperty(nameof(PlayerInput.m_DefaultControlScheme));
            m_NeverAutoSwitchControlSchemesProperty = serializedObject.FindProperty(nameof(PlayerInput.m_NeverAutoSwitchControlSchemes));
            m_DefaultActionMapProperty = serializedObject.FindProperty(nameof(PlayerInput.m_DefaultActionMap));
            m_NotificationBehaviorProperty = serializedObject.FindProperty(nameof(PlayerInput.m_NotificationBehavior));
            m_CameraProperty = serializedObject.FindProperty(nameof(PlayerInput.m_Camera));
            m_ActionEventsProperty = serializedObject.FindProperty(nameof(PlayerInput.m_ActionEvents));
            m_DeviceLostEventProperty = serializedObject.FindProperty(nameof(PlayerInput.m_DeviceLostEvent));
            m_DeviceRegainedEventProperty = serializedObject.FindProperty(nameof(PlayerInput.m_DeviceRegainedEvent));
            m_ControlsChangedEventProperty = serializedObject.FindProperty(nameof(PlayerInput.m_ControlsChangedEvent));

            #if UNITY_INPUT_SYSTEM_ENABLE_UI
            m_UIInputModuleProperty = serializedObject.FindProperty(nameof(PlayerInput.m_UIInputModule));
            #endif
        }

        public void OnDisable()
        {
            new InputComponentEditorAnalytic(InputSystemComponent.PlayerInput).Send();
            new PlayerInputEditorAnalytic(this).Send();
        }

        public void OnDestroy()
        {
            InputActionImporter.onImport -= Refresh;
            InputUser.onChange -= OnUserChange;
        }

        private void Refresh()
        {
            ////FIXME: doesn't seem like we're picking up the results of the latest import
            m_ActionAssetInitialized = false;
            Repaint();
        }

        private void OnUserChange(InputUser user, InputUserChange change, InputDevice device)
        {
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            // Action config section.
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ActionsProperty);
            var actionsWereChanged = false;
            if (EditorGUI.EndChangeCheck() || !m_ActionAssetInitialized || CheckIfActionAssetChanged())
            {
                OnActionAssetChange();
                actionsWereChanged = true;
            }

            ++EditorGUI.indentLevel;
            if (m_ControlSchemeOptions != null && m_ControlSchemeOptions.Length > 1) // Don't show if <Any> is the only option.
            {
                // Default control scheme picker.
                Color currentBg = GUI.backgroundColor;
                // if the invalid DefaultControlSchemeName is selected set the popup draw the BG color in red
                if (m_InvalidDefaultControlSchemeName != null && m_SelectedDefaultControlScheme == 1)
                    GUI.backgroundColor = Color.red;

                var rect = EditorGUILayout.GetControlRect();
                var label = EditorGUI.BeginProperty(rect, m_DefaultControlSchemeText, m_DefaultControlSchemeProperty);
                var selected = EditorGUI.Popup(rect, label, m_SelectedDefaultControlScheme, m_ControlSchemeOptions);
                EditorGUI.EndProperty();
                if (selected != m_SelectedDefaultControlScheme)
                {
                    if (selected == 0)
                    {
                        m_DefaultControlSchemeProperty.stringValue = null;
                    }
                    // if there is an invalid default scheme name it will be at rank 1.
                    // we use m_InvalidDefaultControlSchemeName to prevent usage of the string with "name<Not Found>"
                    else if (m_InvalidDefaultControlSchemeName != null && selected == 1)
                    {
                        m_DefaultControlSchemeProperty.stringValue = m_InvalidDefaultControlSchemeName;
                    }
                    else
                    {
                        m_DefaultControlSchemeProperty.stringValue = m_ControlSchemeOptions[selected].text;
                    }
                    m_SelectedDefaultControlScheme = selected;
                }
                // Restore the initial color
                GUI.backgroundColor = currentBg;


                rect = EditorGUILayout.GetControlRect();
                label = EditorGUI.BeginProperty(rect, m_AutoSwitchText, m_NeverAutoSwitchControlSchemesProperty);
                var neverAutoSwitchValueOld = m_NeverAutoSwitchControlSchemesProperty.boolValue;
                var neverAutoSwitchValueNew = !EditorGUI.Toggle(rect, label, !neverAutoSwitchValueOld);
                EditorGUI.EndProperty();
                if (neverAutoSwitchValueOld != neverAutoSwitchValueNew)
                {
                    m_NeverAutoSwitchControlSchemesProperty.boolValue = neverAutoSwitchValueNew;
                    serializedObject.ApplyModifiedProperties();
                }
            }
            if (m_ActionMapOptions != null && m_ActionMapOptions.Length > 0)
            {
                // Default action map picker.
                var rect = EditorGUILayout.GetControlRect();
                var label = EditorGUI.BeginProperty(rect, m_DefaultActionMapText, m_DefaultActionMapProperty);
                var selected = EditorGUI.Popup(rect, label, m_SelectedDefaultActionMap,
                    m_ActionMapOptions);
                EditorGUI.EndProperty();
                if (selected != m_SelectedDefaultActionMap)
                {
                    if (selected == 0)
                    {
                        m_DefaultActionMapProperty.stringValue = null;
                    }
                    else
                    {
                        // Use ID rather than name.
                        var asset = (InputActionAsset)m_ActionsProperty.objectReferenceValue;
                        var actionMap = asset.FindActionMap(m_ActionMapOptions[selected].text);
                        if (actionMap != null)
                            m_DefaultActionMapProperty.stringValue = actionMap.id.ToString();
                    }
                    m_SelectedDefaultActionMap = selected;
                }
            }
            --EditorGUI.indentLevel;
            DoHelpCreateAssetUI();

            #if UNITY_INPUT_SYSTEM_ENABLE_UI
            // UI config section.
            if (m_UIPropertyText == null)
                m_UIPropertyText = EditorGUIUtility.TrTextContent("UI Input Module", m_UIInputModuleProperty.GetTooltip());
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_UIInputModuleProperty, m_UIPropertyText);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (m_UIInputModuleProperty.objectReferenceValue != null)
            {
                var uiModule = m_UIInputModuleProperty.objectReferenceValue as InputSystemUIInputModule;
                if (m_ActionsProperty.objectReferenceValue != null && uiModule.actionsAsset != m_ActionsProperty.objectReferenceValue)
                {
                    EditorGUILayout.HelpBox("The referenced InputSystemUIInputModule is configured using different input actions than this PlayerInput. They should match if you want to synchronize PlayerInput actions to the UI input.", MessageType.Warning);
                    if (GUILayout.Button(m_FixInputModuleText))
                        InputSystemUIInputModuleEditor.ReassignActions(uiModule, m_ActionsProperty.objectReferenceValue as InputActionAsset);
                }
            }
            #endif

            // Camera section.
            if (m_CameraPropertyText == null)
                m_CameraPropertyText = EditorGUIUtility.TrTextContent("Camera", m_CameraProperty.GetTooltip());
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CameraProperty, m_CameraPropertyText);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            // Notifications/event section.
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_NotificationBehaviorProperty, m_NotificationBehaviorText);
            if (EditorGUI.EndChangeCheck() || actionsWereChanged || !m_NotificationBehaviorInitialized)
                OnNotificationBehaviorChange();
            switch ((PlayerNotifications)m_NotificationBehaviorProperty.intValue)
            {
                case PlayerNotifications.SendMessages:
                case PlayerNotifications.BroadcastMessages:
                    Debug.Assert(m_SendMessagesHelpText != null);
                    EditorGUILayout.HelpBox(m_SendMessagesHelpText);
                    break;

                case PlayerNotifications.InvokeUnityEvents:
                    m_EventsGroupUnfolded = EditorGUILayout.Foldout(m_EventsGroupUnfolded, m_EventsGroupText, toggleOnLabelClick: true);
                    if (m_EventsGroupUnfolded)
                    {
                        // Action events. Group by action map.
                        if (m_ActionNames != null)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                for (var n = 0; n < m_NumActionMaps; ++n)
                                {
                                    // Skip action maps that have no names (case 1317735).
                                    if (m_ActionMapNames[n] == null)
                                        continue;

                                    m_ActionMapEventsUnfolded[n] = EditorGUILayout.Foldout(m_ActionMapEventsUnfolded[n],
                                        m_ActionMapNames[n], toggleOnLabelClick: true);
                                    using (new EditorGUI.IndentLevelScope())
                                    {
                                        if (m_ActionMapEventsUnfolded[n])
                                        {
                                            for (var i = 0; i < m_ActionNames.Length; ++i)
                                            {
                                                if (m_ActionMapIndices[i] != n)
                                                    continue;

                                                EditorGUILayout.PropertyField(m_ActionEventsProperty.GetArrayElementAtIndex(i), m_ActionNames[i]);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Misc events.
                        EditorGUILayout.PropertyField(m_DeviceLostEventProperty);
                        EditorGUILayout.PropertyField(m_DeviceRegainedEventProperty);
                        EditorGUILayout.PropertyField(m_ControlsChangedEventProperty);
                    }
                    break;
            }

            // Miscellaneous buttons.
            DoUtilityButtonsUI();

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            // Debug UI.
            if (EditorApplication.isPlaying)
                DoDebugUI();
        }

        // This checks changes that are not captured by BeginChangeCheck/EndChangeCheck.
        // One such case is when the user triggers a "Reset" on the component.
        bool CheckIfActionAssetChanged()
        {
            if (m_ActionsProperty.objectReferenceValue != null)
            {
                var assetInstanceID = m_ActionsProperty.objectReferenceValue.GetInstanceID();
                bool result = assetInstanceID != m_ActionAssetInstanceID;
                m_ActionAssetInstanceID = (int)assetInstanceID;
                return result;
            }

            m_ActionAssetInstanceID = -1;
            return false;
        }

        private void DoHelpCreateAssetUI()
        {
            if (m_ActionsProperty.objectReferenceValue != null)
            {
                // All good. We already have an asset.
                return;
            }

            EditorGUILayout.HelpBox("There are no input actions associated with this input component yet. Click the button below to create "
                + "a new set of input actions or drag an existing input actions asset into the field above.", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button(m_CreateActionsText, EditorStyles.miniButton, GUILayout.MaxWidth(120)))
            {
                // Request save file location.
                var defaultFileName = Application.productName;
                var fileName = EditorUtility.SaveFilePanel("Create Input Actions Asset", "Assets", defaultFileName,
                    InputActionAsset.Extension);

                ////TODO: take current Supported Devices into account when creating this

                // Create and import asset and open editor.
                if (!string.IsNullOrEmpty(fileName))
                {
                    if (!fileName.StartsWith(Application.dataPath))
                    {
                        Debug.LogError($"Path must be located in Assets/ folder (got: '{fileName}')");
                        EditorGUILayout.EndHorizontal();
                        return;
                    }

                    if (!fileName.EndsWith("." + InputActionAsset.Extension))
                        fileName += "." + InputActionAsset.Extension;

                    // Load default actions and update all GUIDs.
                    var defaultActionsText = File.ReadAllText(kDefaultInputActionsAssetPath);
                    var newActions = InputActionAsset.FromJson(defaultActionsText);
                    foreach (var map in newActions.actionMaps)
                    {
                        map.m_Id = Guid.NewGuid().ToString();
                        foreach (var action in map.actions)
                            action.m_Id = Guid.NewGuid().ToString();
                    }
                    newActions.name = Path.GetFileNameWithoutExtension(fileName);
                    var newActionsText = newActions.ToJson();

                    // Write it out and tell the asset DB to pick it up.
                    File.WriteAllText(fileName, newActionsText);

                    // Import the new asset
                    var relativePath = "Assets/" + fileName.Substring(Application.dataPath.Length + 1);
                    AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceSynchronousImport);

                    // Load imported object.
                    var importedObject = AssetDatabase.LoadAssetAtPath<InputActionAsset>(relativePath);

                    // Set it on the PlayerInput component.
                    m_ActionsProperty.objectReferenceValue = importedObject;
                    serializedObject.ApplyModifiedProperties();

                    // Open the asset.
                    AssetDatabase.OpenAsset(importedObject);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
        }

        private void DoUtilityButtonsUI()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(m_OpenSettingsText, EditorStyles.miniButton))
                InputSettingsProvider.Open();

            if (GUILayout.Button(m_OpenDebuggerText, EditorStyles.miniButton))
                InputDebuggerWindow.CreateOrShow();

            EditorGUILayout.EndHorizontal();
        }

        private void DoDebugUI()
        {
            var playerInput = (PlayerInput)target;

            if (!playerInput.user.valid)
                return;

            ////TODO: show actions when they happen

            var user = playerInput.user.index.ToString();
            var controlScheme = playerInput.user.controlScheme?.name;
            var devices = string.Join(", ", playerInput.user.pairedDevices);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(m_DebugText, EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("User", user);
            EditorGUILayout.LabelField("Control Scheme", controlScheme);
            EditorGUILayout.LabelField("Devices", devices);
            EditorGUI.EndDisabledGroup();
        }

        private void OnNotificationBehaviorChange()
        {
            Debug.Assert(m_ActionAssetInitialized);
            serializedObject.ApplyModifiedProperties();

            var notificationBehavior = (PlayerNotifications)m_NotificationBehaviorProperty.intValue;
            switch (notificationBehavior)
            {
                // Create text that lists all the messages sent by the component.
                case PlayerNotifications.BroadcastMessages:
                case PlayerNotifications.SendMessages:
                {
                    var builder = new StringBuilder();
                    builder.Append("Will ");
                    if (notificationBehavior == PlayerNotifications.BroadcastMessages)
                        builder.Append("BroadcastMessage()");
                    else
                        builder.Append("SendMessage()");
                    builder.Append(" to GameObject: ");
                    builder.Append(PlayerInput.DeviceLostMessage);
                    builder.Append(", ");
                    builder.Append(PlayerInput.DeviceRegainedMessage);
                    builder.Append(", ");
                    builder.Append(PlayerInput.ControlsChangedMessage);

                    var playerInput = (PlayerInput)target;
                    var asset = playerInput.m_Actions;
                    if (asset != null)
                    {
                        foreach (var action in asset)
                        {
                            builder.Append(", On");
                            builder.Append(CSharpCodeHelpers.MakeTypeName(action.name));
                        }
                    }

                    m_SendMessagesHelpText = new GUIContent(builder.ToString());
                    break;
                }

                case PlayerNotifications.InvokeUnityEvents:
                {
                    var playerInput = (PlayerInput)target;
                    if (playerInput.m_DeviceLostEvent == null)
                        playerInput.m_DeviceLostEvent = new PlayerInput.DeviceLostEvent();
                    if (playerInput.m_DeviceRegainedEvent == null)
                        playerInput.m_DeviceRegainedEvent = new PlayerInput.DeviceRegainedEvent();
                    if (playerInput.m_ControlsChangedEvent == null)
                        playerInput.m_ControlsChangedEvent = new PlayerInput.ControlsChangedEvent();
                    serializedObject.Update();

                    // Force action refresh.
                    m_ActionAssetInitialized = false;
                    Refresh();
                    break;
                }
            }

            m_NotificationBehaviorInitialized = true;
        }

        private void OnActionAssetChange()
        {
            serializedObject.ApplyModifiedProperties();
            m_ActionAssetInitialized = true;

            var playerInput = (PlayerInput)target;
            var asset = (InputActionAsset)m_ActionsProperty.objectReferenceValue;
            if (asset == null)
            {
                m_ControlSchemeOptions = null;
                m_ActionMapOptions = null;
                m_ActionNames = null;
                m_SelectedDefaultActionMap = -1;
                m_SelectedDefaultControlScheme = -1;
                m_InvalidDefaultControlSchemeName = null;
                return;
            }

            // If we're sending Unity events, read out the event list.
            if ((PlayerNotifications)m_NotificationBehaviorProperty.intValue ==
                PlayerNotifications.InvokeUnityEvents)
            {
                ////FIXME: this should preserve the same order that we have in the asset
                var newActionNames = new List<GUIContent>();
                var newActionEvents = new List<PlayerInput.ActionEvent>();
                var newActionMapIndices = new List<int>();

                m_NumActionMaps = 0;
                m_ActionMapNames = null;

                void AddEntry(InputAction action, PlayerInput.ActionEvent actionEvent)
                {
                    newActionNames.Add(new GUIContent(action.name));
                    newActionEvents.Add(actionEvent);

                    var actionMapIndex = asset.actionMaps.IndexOfReference(action.actionMap);
                    newActionMapIndices.Add(actionMapIndex);

                    if (actionMapIndex >= m_NumActionMaps)
                        m_NumActionMaps = actionMapIndex + 1;

                    ArrayHelpers.PutAtIfNotSet(ref m_ActionMapNames, actionMapIndex,
                        () => new GUIContent(action.actionMap.name));
                }

                // Bring over any action events that we already have and that are still in the asset.
                var oldActionEvents = playerInput.m_ActionEvents;
                if (oldActionEvents != null)
                {
                    foreach (var entry in oldActionEvents)
                    {
                        var guid = entry.actionId;
                        var action = asset.FindAction(guid);
                        if (action != null)
                            AddEntry(action, entry);
                    }
                }

                // Add any new actions.
                foreach (var action in asset)
                {
                    // Skip if it was already in there.
                    if (oldActionEvents != null && oldActionEvents.Any(x => x.actionId == action.id.ToString()))
                        continue;

                    ////FIXME: adds bindings to the name
                    AddEntry(action, new PlayerInput.ActionEvent(action.id, action.ToString()));
                }

                m_ActionNames = newActionNames.ToArray();
                m_ActionMapIndices = newActionMapIndices.ToArray();
                Array.Resize(ref m_ActionMapEventsUnfolded, m_NumActionMaps);
                playerInput.m_ActionEvents = newActionEvents.ToArray();
            }

            // Read out control schemes.
            var selectedDefaultControlScheme = playerInput.defaultControlScheme;
            m_InvalidDefaultControlSchemeName = null;
            m_SelectedDefaultControlScheme = 0;
            ////TODO: sort alphabetically and ensure that the order is the same in the schemes editor
            var controlSchemesNames = asset.controlSchemes.Select(cs => cs.name).ToList();

            // try to find the selected Default Control Scheme
            if (!string.IsNullOrEmpty(selectedDefaultControlScheme))
            {
                // +1 since <Any> will be the first in the list
                m_SelectedDefaultControlScheme = 1 + controlSchemesNames.FindIndex(name => string.Compare(name, selectedDefaultControlScheme,
                    StringComparison.InvariantCultureIgnoreCase) == 0);
                // if not found, will insert the invalid name next to <Any>
                if (m_SelectedDefaultControlScheme == 0)
                {
                    m_InvalidDefaultControlSchemeName = selectedDefaultControlScheme;
                    m_SelectedDefaultControlScheme = 1;
                    controlSchemesNames.Insert(0, $"{selectedDefaultControlScheme}{L10n.Tr("<Not Found>")}");
                }
            }
            else
            {
                playerInput.defaultControlScheme = null;
            }

            m_ControlSchemeOptions = new GUIContent[controlSchemesNames.Count + 1];
            m_ControlSchemeOptions[0] = new GUIContent(EditorGUIUtility.TrTextContent("<Any>"));
            for (var i = 0; i < controlSchemesNames.Count; ++i)
            {
                m_ControlSchemeOptions[i + 1] = new GUIContent(controlSchemesNames[i]);
            }

            // Read out action maps.
            var selectedDefaultActionMap = !string.IsNullOrEmpty(playerInput.defaultActionMap)
                ? asset.FindActionMap(playerInput.defaultActionMap)
                : null;
            m_SelectedDefaultActionMap = asset.actionMaps.Count > 0 ? 1 : 0;
            var actionMaps = asset.actionMaps;
            m_ActionMapOptions = new GUIContent[actionMaps.Count + 1];
            m_ActionMapOptions[0] = new GUIContent(EditorGUIUtility.TrTextContent("<None>"));
            ////TODO: sort alphabetically
            for (var i = 0; i < actionMaps.Count; ++i)
            {
                var actionMap = actionMaps[i];
                m_ActionMapOptions[i + 1] = new GUIContent(actionMap.name);

                if (selectedDefaultActionMap != null && actionMap == selectedDefaultActionMap)
                    m_SelectedDefaultActionMap = i + 1;
            }
            if (m_SelectedDefaultActionMap <= 0)
                playerInput.defaultActionMap = null;
            else
                playerInput.defaultActionMap = m_ActionMapOptions[m_SelectedDefaultActionMap].text;

            serializedObject.Update();
        }

        [SerializeField] private bool m_EventsGroupUnfolded;
        [SerializeField] private bool[] m_ActionMapEventsUnfolded;

        [NonSerialized] private readonly GUIContent m_CreateActionsText = EditorGUIUtility.TrTextContent("Create Actions...");
        [NonSerialized] private readonly GUIContent m_FixInputModuleText = EditorGUIUtility.TrTextContent("Fix UI Input Module");
        [NonSerialized] private readonly GUIContent m_OpenSettingsText = EditorGUIUtility.TrTextContent("Open Input Settings");
        [NonSerialized] private readonly GUIContent m_OpenDebuggerText = EditorGUIUtility.TrTextContent("Open Input Debugger");
        [NonSerialized] private readonly GUIContent m_EventsGroupText =
            EditorGUIUtility.TrTextContent("Events", "UnityEvents triggered by the PlayerInput component");
        [NonSerialized] private readonly GUIContent m_NotificationBehaviorText =
            EditorGUIUtility.TrTextContent("Behavior",
                "Determine how notifications should be sent when an input-related event associated with the player happens.");
        [NonSerialized] private readonly GUIContent m_DefaultControlSchemeText =
            EditorGUIUtility.TrTextContent("Default Scheme", "Which control scheme to try by default. If not set, PlayerInput "
                + "will simply go through all control schemes in the action asset and try one after the other. If set, PlayerInput will try "
                + "the given scheme first but if using that fails (e.g. when not required devices are missing) will fall back to trying the other "
                + "control schemes in order.");
        [NonSerialized] private readonly GUIContent m_DefaultActionMapText =
            EditorGUIUtility.TrTextContent("Default Map", "Action map to enable by default. If not set, no actions will be enabled by default.");
        [NonSerialized] private readonly GUIContent m_AutoSwitchText =
            EditorGUIUtility.TrTextContent("Auto-Switch",
                "By default, when there is only a single PlayerInput, the player "
                + "is allowed to freely switch between control schemes simply by starting to use a different device. By toggling this property off, this "
                + "behavior is disabled and even with a single player, the player will stay locked onto the explicitly selected control scheme. Note "
                + "that you can still change control schemes explicitly through the PlayerInput API.\n\nWhen there are multiple PlayerInputs in the game, auto-switching is disabled automatically regardless of the value of this property.");
        [NonSerialized] private readonly GUIContent m_DebugText = EditorGUIUtility.TrTextContent("Debug");
        [NonSerialized] private GUIContent m_UIPropertyText;
        [NonSerialized] private GUIContent m_CameraPropertyText;
        [NonSerialized] private GUIContent m_SendMessagesHelpText;
        [NonSerialized] private GUIContent[] m_ActionNames;
        [NonSerialized] private GUIContent[] m_ActionMapNames;
        [NonSerialized] private int[] m_ActionMapIndices;
        [NonSerialized] private int m_NumActionMaps;
        [NonSerialized] private int m_SelectedDefaultControlScheme;
        [NonSerialized] private string m_InvalidDefaultControlSchemeName;
        [NonSerialized] private GUIContent[] m_ControlSchemeOptions;
        [NonSerialized] private int m_SelectedDefaultActionMap;
        [NonSerialized] private GUIContent[] m_ActionMapOptions;

        [NonSerialized] private SerializedProperty m_ActionsProperty;
        [NonSerialized] private SerializedProperty m_DefaultControlSchemeProperty;
        [NonSerialized] private SerializedProperty m_DefaultActionMapProperty;
        [NonSerialized] private SerializedProperty m_NeverAutoSwitchControlSchemesProperty;
        [NonSerialized] private SerializedProperty m_NotificationBehaviorProperty;
        #if UNITY_INPUT_SYSTEM_ENABLE_UI
        [NonSerialized] private SerializedProperty m_UIInputModuleProperty;
        #endif
        [NonSerialized] private SerializedProperty m_ActionEventsProperty;
        [NonSerialized] private SerializedProperty m_CameraProperty;
        [NonSerialized] private SerializedProperty m_DeviceLostEventProperty;
        [NonSerialized] private SerializedProperty m_DeviceRegainedEventProperty;
        [NonSerialized] private SerializedProperty m_ControlsChangedEventProperty;

        [NonSerialized] private bool m_NotificationBehaviorInitialized;
        [NonSerialized] private bool m_ActionAssetInitialized;
        [NonSerialized] private int m_ActionAssetInstanceID;
    }
}
#endif // UNITY_EDITOR
