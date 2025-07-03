#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Utilities;

#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif

////FIXME: Generate proper IDs for the individual tree view items; the current sequential numbering scheme just causes lots of
////       weird expansion/collapsing to happen.

////TODO: add way to load and replay event traces

////TODO: refresh metrics on demand

////TODO: when an action is triggered and when a device changes state, make them bold in the list for a brief moment

////TODO: show input users and their actions and devices

////TODO: append " (Disabled) to disabled devices and grey them out

////TODO: split 'Local' and 'Remote' at root rather than inside subnodes

////TODO: refresh when unrecognized device pops up

namespace UnityEngine.InputSystem.Editor
{
    // Allows looking at input activity in the editor.
    internal class InputDebuggerWindow : EditorWindow, ISerializationCallbackReceiver
    {
        private static int s_Disabled;
        private static InputDebuggerWindow s_Instance;

        [MenuItem("Window/Analysis/Input Debugger", false, 2100)]
        public static void CreateOrShow()
        {
            if (s_Instance == null)
            {
                s_Instance = GetWindow<InputDebuggerWindow>();
                s_Instance.Show();
                s_Instance.titleContent = new GUIContent("Input Debug");
            }
            else
            {
                s_Instance.Show();
                s_Instance.Focus();
            }
        }

        public static void Enable()
        {
            if (s_Disabled == 0)
                return;

            --s_Disabled;
            if (s_Disabled == 0 && s_Instance != null)
            {
                s_Instance.InstallHooks();
                s_Instance.Refresh();
            }
        }

        public static void Disable()
        {
            ++s_Disabled;
            if (s_Disabled == 1 && s_Instance != null)
            {
                s_Instance.UninstallHooks();
                s_Instance.Refresh();
            }
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            // Update tree if devices are added or removed.
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed)
                Refresh();
        }

        private void OnLayoutChange(string name, InputControlLayoutChange change)
        {
            // Update tree if layout setup has changed.
            Refresh();
        }

        private void OnActionChange(object actionOrMap, InputActionChange change)
        {
            switch (change)
            {
                // When an action is triggered, we only need a repaint.
                case InputActionChange.ActionStarted:
                case InputActionChange.ActionPerformed:
                case InputActionChange.ActionCanceled:
                    Repaint();
                    break;

                case InputActionChange.ActionEnabled:
                case InputActionChange.ActionDisabled:
                case InputActionChange.ActionMapDisabled:
                case InputActionChange.ActionMapEnabled:
                case InputActionChange.BoundControlsChanged:
                    Refresh();
                    break;
            }
        }

        private void OnSettingsChange()
        {
            Refresh();
        }

        private string OnFindLayout(ref InputDeviceDescription description, string matchedLayout,
            InputDeviceExecuteCommandDelegate executeCommandDelegate)
        {
            // If there's no matched layout, there's a chance this device will go in
            // the unsupported list. There's no direct notification for that so we
            // preemptively trigger a refresh.
            if (string.IsNullOrEmpty(matchedLayout))
                Refresh();

            return null;
        }

        private void Refresh()
        {
            m_NeedReload = true;
            Repaint();
        }

        public void OnDestroy()
        {
            UninstallHooks();
        }

        private void InstallHooks()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            InputSystem.onLayoutChange += OnLayoutChange;
            InputSystem.onFindLayoutForDevice += OnFindLayout;
            InputSystem.onActionChange += OnActionChange;
            InputSystem.onSettingsChange += OnSettingsChange;
        }

        private void UninstallHooks()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            InputSystem.onLayoutChange -= OnLayoutChange;
            InputSystem.onFindLayoutForDevice -= OnFindLayout;
            InputSystem.onActionChange -= OnActionChange;
            InputSystem.onSettingsChange -= OnSettingsChange;
        }

        private void Initialize()
        {
            InstallHooks();

            var newTreeViewState = m_TreeViewState == null;
            if (newTreeViewState)
                m_TreeViewState = new TreeViewState();

            m_TreeView = new InputSystemTreeView(m_TreeViewState);

            // Set default expansion states.
            if (newTreeViewState)
                m_TreeView.SetExpanded(m_TreeView.devicesItem.id, true);

            m_Initialized = true;
        }

        public void OnGUI()
        {
            if (s_Disabled > 0)
            {
                EditorGUILayout.LabelField("Disabled");
                return;
            }

            // If the new backends aren't enabled, show a warning in the debugger.
            if (!EditorPlayerSettingHelpers.newSystemBackendsEnabled)
            {
                EditorGUILayout.HelpBox(
                    "Platform backends for the new input system are not enabled. " +
                    "No devices and input from hardware will come through in the new input system APIs.\n\n" +
                    "To enable the backends, set 'Active Input Handling' in the player settings to either 'Input System (Preview)' " +
                    "or 'Both' and restart the editor.", MessageType.Warning);
            }

            // This also brings us back online after a domain reload.
            if (!m_Initialized)
            {
                Initialize();
            }
            else if (m_NeedReload)
            {
                m_TreeView.Reload();
                m_NeedReload = false;
            }

            DrawToolbarGUI();

            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_TreeView.OnGUI(rect);
        }

        private static void ResetDevice(InputDevice device, bool hard)
        {
            var playerUpdateType = InputDeviceDebuggerWindow.DetermineUpdateTypeToShow(device);
            var currentUpdateType = InputState.currentUpdateType;
            InputStateBuffers.SwitchTo(InputSystem.s_Manager.m_StateBuffers, playerUpdateType);
            InputSystem.ResetDevice(device, alsoResetDontResetControls: hard);
            InputStateBuffers.SwitchTo(InputSystem.s_Manager.m_StateBuffers, currentUpdateType);
        }

        private static void ToggleAddDevicesNotSupportedByProject()
        {
            InputEditorUserSettings.addDevicesNotSupportedByProject =
                !InputEditorUserSettings.addDevicesNotSupportedByProject;
        }

        private void ToggleDiagnosticMode()
        {
            if (InputSystem.s_Manager.m_Diagnostics != null)
            {
                InputSystem.s_Manager.m_Diagnostics = null;
            }
            else
            {
                if (m_Diagnostics == null)
                    m_Diagnostics = new InputDiagnostics();
                InputSystem.s_Manager.m_Diagnostics = m_Diagnostics;
            }
        }

        private static void ToggleTouchSimulation()
        {
            InputEditorUserSettings.simulateTouch = !InputEditorUserSettings.simulateTouch;
        }

        private static void EnableRemoteDevices(bool enable = true)
        {
            foreach (var player in EditorConnection.instance.ConnectedPlayers)
            {
                EditorConnection.instance.Send(enable ? RemoteInputPlayerConnection.kStartSendingMsg : RemoteInputPlayerConnection.kStopSendingMsg, new byte[0], player.playerId);
                if (!enable)
                    InputSystem.remoting.RemoveRemoteDevices(player.playerId);
            }
        }

        private static void DrawConnectionGUI()
        {
            if (GUILayout.Button("Remote Devices…", EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();
                var haveRemotes = InputSystem.devices.Any(x => x.remote);
                if (EditorConnection.instance.ConnectedPlayers.Count > 0)
                    menu.AddItem(new GUIContent("Show remote devices"), haveRemotes, () =>
                    {
                        EnableRemoteDevices(!haveRemotes);
                    });
                else
                    menu.AddDisabledItem(new GUIContent("Show remote input devices"));

                menu.AddSeparator("");

                var availableProfilers = ProfilerDriver.GetAvailableProfilers();
                foreach (var profiler in availableProfilers)
                {
                    var enabled = ProfilerDriver.IsIdentifierConnectable(profiler);
                    var profilerName = ProfilerDriver.GetConnectionIdentifier(profiler);
                    var isConnected = ProfilerDriver.connectedProfiler == profiler;
                    if (enabled)
                        menu.AddItem(new GUIContent(profilerName), isConnected, () =>
                        {
                            ProfilerDriver.connectedProfiler = profiler;
                            EnableRemoteDevices();
                        });
                    else
                        menu.AddDisabledItem(new GUIContent(profilerName));
                }

                foreach (var device in UnityEditor.Hardware.DevDeviceList.GetDevices())
                {
                    var supportsPlayerConnection = (device.features & UnityEditor.Hardware.DevDeviceFeatures.PlayerConnection) != 0;
                    if (!device.isConnected || !supportsPlayerConnection)
                        continue;

                    var url = "device://" + device.id;
                    var isConnected = ProfilerDriver.connectedProfiler == 0xFEEE && ProfilerDriver.directConnectionUrl == url;
                    menu.AddItem(new GUIContent(device.name), isConnected, () =>
                    {
                        ProfilerDriver.DirectURLConnect(url);
                        EnableRemoteDevices();
                    });
                }

                menu.ShowAsContext();
            }
        }

        private void DrawToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(Contents.optionsContent, EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();

                menu.AddItem(Contents.addDevicesNotSupportedByProjectContent, InputEditorUserSettings.addDevicesNotSupportedByProject,
                    ToggleAddDevicesNotSupportedByProject);
                menu.AddItem(Contents.diagnosticsModeContent, InputSystem.s_Manager.m_Diagnostics != null,
                    ToggleDiagnosticMode);
                menu.AddItem(Contents.touchSimulationContent, InputEditorUserSettings.simulateTouch, ToggleTouchSimulation);

                // Add the inverse of "Copy Device Description" which adds a device with the description from
                // the clipboard to the system. This is most useful for debugging and makes it very easy to
                // have a first pass at device descriptions supplied by users.
                try
                {
                    var copyBuffer = EditorHelpers.GetSystemCopyBufferContents();
                    if (!string.IsNullOrEmpty(copyBuffer) &&
                        copyBuffer.StartsWith("{") && !InputDeviceDescription.FromJson(copyBuffer).empty)
                    {
                        menu.AddItem(Contents.pasteDeviceDescriptionAsDevice, false, () =>
                        {
                            var description = InputDeviceDescription.FromJson(copyBuffer);
                            InputSystem.AddDevice(description);
                        });
                    }
                }
                catch (ArgumentException)
                {
                    // Catch and ignore exception if buffer doesn't actually contain an InputDeviceDescription
                    // in (proper) JSON format.
                }

                menu.ShowAsContext();
            }

            DrawConnectionGUI();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        [SerializeField] private TreeViewState m_TreeViewState;

        [NonSerialized] private InputDiagnostics m_Diagnostics;
        [NonSerialized] private InputSystemTreeView m_TreeView;
        [NonSerialized] private bool m_Initialized;
        [NonSerialized] private bool m_NeedReload;

        internal static void ReviveAfterDomainReload()
        {
            if (s_Instance != null)
            {
                // Trigger initial repaint. Will call Initialize() to install hooks and
                // refresh tree.
                s_Instance.Repaint();
            }
        }

        private static class Contents
        {
            public static readonly GUIContent optionsContent = new GUIContent("Options");
            public static readonly GUIContent touchSimulationContent = new GUIContent("Simulate Touch Input From Mouse or Pen");
            public static readonly GUIContent pasteDeviceDescriptionAsDevice = new GUIContent("Paste Device Description as Device");
            public static readonly GUIContent addDevicesNotSupportedByProjectContent = new GUIContent("Add Devices Not Listed in 'Supported Devices'");
            public static readonly GUIContent diagnosticsModeContent = new GUIContent("Enable Event Diagnostics");
            public static readonly GUIContent openDebugView = new GUIContent("Open Device Debug View");
            public static readonly GUIContent copyDeviceDescription = new GUIContent("Copy Device Description");
            public static readonly GUIContent copyLayoutAsJSON = new GUIContent("Copy Layout as JSON");
            public static readonly GUIContent createDeviceFromLayout = new GUIContent("Create Device from Layout");
            public static readonly GUIContent generateCodeFromLayout = new GUIContent("Generate Precompiled Layout");
            public static readonly GUIContent removeDevice = new GUIContent("Remove Device");
            public static readonly GUIContent enableDevice = new GUIContent("Enable Device");
            public static readonly GUIContent disableDevice = new GUIContent("Disable Device");
            public static readonly GUIContent syncDevice = new GUIContent("Try to Sync Device");
            public static readonly GUIContent softResetDevice = new GUIContent("Reset Device (Soft)");
            public static readonly GUIContent hardResetDevice = new GUIContent("Reset Device (Hard)");
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            s_Instance = this;
        }

        private class InputSystemTreeView : TreeView
        {
            public TreeViewItem actionsItem { get; private set; }
            public TreeViewItem devicesItem { get; private set; }
            public TreeViewItem layoutsItem { get; private set; }
            public TreeViewItem settingsItem { get; private set; }
            public TreeViewItem metricsItem { get; private set; }
            public TreeViewItem usersItem { get; private set; }

            public InputSystemTreeView(TreeViewState state)
                : base(state)
            {
                Reload();
            }

            protected override void ContextClickedItem(int id)
            {
                var item = FindItem(id, rootItem);
                if (item == null)
                    return;

                if (item is DeviceItem deviceItem)
                {
                    var menu = new GenericMenu();
                    menu.AddItem(Contents.openDebugView, false, () => InputDeviceDebuggerWindow.CreateOrShowExisting(deviceItem.device));
                    menu.AddItem(Contents.copyDeviceDescription, false,
                        () => EditorHelpers.SetSystemCopyBufferContents(deviceItem.device.description.ToJson()));
                    menu.AddItem(Contents.removeDevice, false, () => InputSystem.RemoveDevice(deviceItem.device));
                    if (deviceItem.device.enabled)
                        menu.AddItem(Contents.disableDevice, false, () => InputSystem.DisableDevice(deviceItem.device));
                    else
                        menu.AddItem(Contents.enableDevice, false, () => InputSystem.EnableDevice(deviceItem.device));
                    menu.AddItem(Contents.syncDevice, false, () => InputSystem.TrySyncDevice(deviceItem.device));
                    menu.AddItem(Contents.softResetDevice, false, () => ResetDevice(deviceItem.device, false));
                    menu.AddItem(Contents.hardResetDevice, false, () => ResetDevice(deviceItem.device, true));
                    menu.ShowAsContext();
                }

                if (item is UnsupportedDeviceItem unsupportedDeviceItem)
                {
                    var menu = new GenericMenu();
                    menu.AddItem(Contents.copyDeviceDescription, false,
                        () => EditorHelpers.SetSystemCopyBufferContents(unsupportedDeviceItem.description.ToJson()));
                    menu.ShowAsContext();
                }

                if (item is LayoutItem layoutItem)
                {
                    var layout = EditorInputControlLayoutCache.TryGetLayout(layoutItem.layoutName);
                    if (layout != null)
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(Contents.copyLayoutAsJSON, false,
                            () => EditorHelpers.SetSystemCopyBufferContents(layout.ToJson()));
                        if (layout.isDeviceLayout)
                        {
                            menu.AddItem(Contents.createDeviceFromLayout, false,
                                () => InputSystem.AddDevice(layout.name));
                            menu.AddItem(Contents.generateCodeFromLayout, false, () =>
                            {
                                var fileName = EditorUtility.SaveFilePanel("Generate InputDevice Code", "", "Fast" + layoutItem.layoutName, "cs");
                                var isInAssets = fileName.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase);
                                if (isInAssets)
                                    fileName = "Assets/" + fileName.Substring(Application.dataPath.Length + 1);
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    var code = InputLayoutCodeGenerator.GenerateCodeFileForDeviceLayout(layoutItem.layoutName, fileName, prefix: "Fast");
                                    File.WriteAllText(fileName, code);
                                    if (isInAssets)
                                        AssetDatabase.Refresh();
                                }
                            });
                        }
                        menu.ShowAsContext();
                    }
                }
            }

            protected override void DoubleClickedItem(int id)
            {
                var item = FindItem(id, rootItem);

                if (item is DeviceItem deviceItem)
                    InputDeviceDebuggerWindow.CreateOrShowExisting(deviceItem.device);
            }

            protected override TreeViewItem BuildRoot()
            {
                var id = 0;

                var root = new TreeViewItem
                {
                    id = id++,
                    depth = -1
                };

                ////TODO: this will need to be improved for multi-user scenarios
                // Actions.
                m_EnabledActions.Clear();
                InputSystem.ListEnabledActions(m_EnabledActions);
                if (m_EnabledActions.Count > 0)
                {
                    actionsItem = AddChild(root, "", ref id);
                    AddEnabledActions(actionsItem, ref id);

                    if (!actionsItem.hasChildren)
                    {
                        // We are culling actions that are assigned to users so we may end up with an empty
                        // list even if we have enabled actions. If we do, remove the "Actions" item from the tree.
                        root.children.Remove(actionsItem);
                    }
                    else
                    {
                        // Update title to include action count.
                        actionsItem.displayName = $"Actions ({actionsItem.children.Count})";
                    }
                }

                // Users.
                var userCount = InputUser.all.Count;
                if (userCount > 0)
                {
                    usersItem = AddChild(root, $"Users ({userCount})", ref id);
                    foreach (var user in InputUser.all)
                        AddUser(usersItem, user, ref id);
                }

                // Devices.
                var devices = InputSystem.devices;
                devicesItem = AddChild(root, $"Devices ({devices.Count})", ref id);
                var haveRemotes = devices.Any(x => x.remote);
                TreeViewItem localDevicesNode = null;
                if (haveRemotes)
                {
                    // Split local and remote devices into groups.

                    localDevicesNode = AddChild(devicesItem, "Local", ref id);
                    AddDevices(localDevicesNode, devices, ref id);

                    var remoteDevicesNode = AddChild(devicesItem, "Remote", ref id);
                    foreach (var player in EditorConnection.instance.ConnectedPlayers)
                    {
                        var playerNode = AddChild(remoteDevicesNode, player.name, ref id);
                        AddDevices(playerNode, devices, ref id, player.playerId);
                    }
                }
                else
                {
                    // We don't have remote devices so don't add an extra group for local devices.
                    // Put them all directly underneath the "Devices" node.
                    AddDevices(devicesItem, devices, ref id);
                }

                ////TDO: unsupported and disconnected devices should also be shown for remotes

                if (m_UnsupportedDevices == null)
                    m_UnsupportedDevices = new List<InputDeviceDescription>();
                m_UnsupportedDevices.Clear();
                InputSystem.GetUnsupportedDevices(m_UnsupportedDevices);
                if (m_UnsupportedDevices.Count > 0)
                {
                    var parent = haveRemotes ? localDevicesNode : devicesItem;
                    var unsupportedDevicesNode = AddChild(parent, $"Unsupported ({m_UnsupportedDevices.Count})", ref id);
                    foreach (var device in m_UnsupportedDevices)
                    {
                        var item = new UnsupportedDeviceItem
                        {
                            id = id++,
                            depth = unsupportedDevicesNode.depth + 1,
                            displayName = device.ToString(),
                            description = device
                        };
                        unsupportedDevicesNode.AddChild(item);
                    }
                    unsupportedDevicesNode.children.Sort((a, b) =>
                        string.Compare(a.displayName, b.displayName, StringComparison.InvariantCulture));
                }

                var disconnectedDevices = InputSystem.disconnectedDevices;
                if (disconnectedDevices.Count > 0)
                {
                    var parent = haveRemotes ? localDevicesNode : devicesItem;
                    var disconnectedDevicesNode = AddChild(parent, $"Disconnected ({disconnectedDevices.Count})", ref id);
                    foreach (var device in disconnectedDevices)
                        AddChild(disconnectedDevicesNode, device.ToString(), ref id);
                    disconnectedDevicesNode.children.Sort((a, b) =>
                        string.Compare(a.displayName, b.displayName, StringComparison.InvariantCulture));
                }

                // Layouts.
                layoutsItem = AddChild(root, "Layouts", ref id);
                AddControlLayouts(layoutsItem, ref id);

                ////FIXME: this shows local configuration only
                // Settings.
                var settings = InputSystem.settings;
                var settingsAssetPath = AssetDatabase.GetAssetPath(settings);
                var settingsLabel = "Settings";
                if (!string.IsNullOrEmpty(settingsAssetPath))
                    settingsLabel = $"Settings ({Path.GetFileName(settingsAssetPath)})";
                settingsItem = AddChild(root, settingsLabel, ref id);
                AddValueItem(settingsItem, "Update Mode", settings.updateMode, ref id);
                AddValueItem(settingsItem, "Compensate For Screen Orientation", settings.compensateForScreenOrientation, ref id);
                AddValueItem(settingsItem, "Default Button Press Point", settings.defaultButtonPressPoint, ref id);
                AddValueItem(settingsItem, "Default Deadzone Min", settings.defaultDeadzoneMin, ref id);
                AddValueItem(settingsItem, "Default Deadzone Max", settings.defaultDeadzoneMax, ref id);
                AddValueItem(settingsItem, "Default Tap Time", settings.defaultTapTime, ref id);
                AddValueItem(settingsItem, "Default Slow Tap Time", settings.defaultSlowTapTime, ref id);
                AddValueItem(settingsItem, "Default Hold Time", settings.defaultHoldTime, ref id);
                if (settings.supportedDevices.Count > 0)
                {
                    var supportedDevices = AddChild(settingsItem, "Supported Devices", ref id);
                    foreach (var item in settings.supportedDevices)
                    {
                        var icon = EditorInputControlLayoutCache.GetIconForLayout(item);
                        AddChild(supportedDevices, item, ref id, icon);
                    }
                }
                settingsItem.children.Sort((a, b) => string.Compare(a.displayName, b.displayName, StringComparison.InvariantCultureIgnoreCase));

                // Metrics.
                var metrics = InputSystem.metrics;
                metricsItem = AddChild(root, "Metrics", ref id);
                AddChild(metricsItem,
                    "Current State Size in Bytes: " + StringHelpers.NicifyMemorySize(metrics.currentStateSizeInBytes),
                    ref id);
                AddValueItem(metricsItem, "Current Control Count", metrics.currentControlCount, ref id);
                AddValueItem(metricsItem, "Current Layout Count", metrics.currentLayoutCount, ref id);

                return root;
            }

            private void AddUser(TreeViewItem parent, InputUser user, ref int id)
            {
                ////REVIEW: can we get better identification? allow associating GameObject with user?
                var userItem = AddChild(parent, "User #" + user.index, ref id);

                // Control scheme.
                var controlScheme = user.controlScheme;
                if (controlScheme != null)
                    AddChild(userItem, "Control Scheme: " + controlScheme, ref id);

                // Paired and lost devices.
                AddDeviceListToUser("Paired Devices", user.pairedDevices, ref id, userItem);
                AddDeviceListToUser("Lost Devices", user.lostDevices, ref id, userItem);

                // Actions.
                var actions = user.actions;
                if (actions != null)
                {
                    var actionsItem = AddChild(userItem, "Actions", ref id);
                    foreach (var action in actions)
                        AddActionItem(actionsItem, action, ref id);

                    parent.children?.Sort((a, b) => string.Compare(a.displayName, b.displayName, StringComparison.CurrentCultureIgnoreCase));
                }
            }

            private void AddDeviceListToUser(string title, ReadOnlyArray<InputDevice> devices, ref int id, TreeViewItem userItem)
            {
                if (devices.Count == 0)
                    return;

                var devicesItem = AddChild(userItem, title, ref id);
                foreach (var device in devices)
                {
                    Debug.Assert(device != null, title + " has a null item!");
                    if (device == null)
                        continue;

                    var item = new DeviceItem
                    {
                        id = id++,
                        depth = devicesItem.depth + 1,
                        displayName = device.ToString(),
                        device = device,
                        icon = EditorInputControlLayoutCache.GetIconForLayout(device.layout),
                    };
                    devicesItem.AddChild(item);
                }
            }

            private static void AddDevices(TreeViewItem parent, IEnumerable<InputDevice> devices, ref int id, int participantId = InputDevice.kLocalParticipantId)
            {
                foreach (var device in devices)
                {
                    if (device.m_ParticipantId != participantId)
                        continue;

                    var displayName = device.name;
                    if (device.usages.Count > 0)
                        displayName += " (" + string.Join(",", device.usages) + ")";

                    var item = new DeviceItem
                    {
                        id = id++,
                        depth = parent.depth + 1,
                        displayName = displayName,
                        device = device,
                        icon = EditorInputControlLayoutCache.GetIconForLayout(device.layout),
                    };
                    parent.AddChild(item);
                }

                parent.children?.Sort((a, b) => string.Compare(a.displayName, b.displayName));
            }

            private void AddControlLayouts(TreeViewItem parent, ref int id)
            {
                // Split root into three different groups:
                // 1) Control layouts
                // 2) Device layouts that don't match specific products
                // 3) Device layouts that match specific products

                var controls = AddChild(parent, "Controls", ref id);
                var devices = AddChild(parent, "Abstract Devices", ref id);
                var products = AddChild(parent, "Specific Devices", ref id);

                foreach (var layout in EditorInputControlLayoutCache.allControlLayouts)
                    AddControlLayoutItem(layout, controls, ref id);
                foreach (var layout in EditorInputControlLayoutCache.allDeviceLayouts)
                    AddControlLayoutItem(layout, devices, ref id);
                foreach (var layout in EditorInputControlLayoutCache.allProductLayouts)
                {
                    var rootBaseLayoutName = InputControlLayout.s_Layouts.GetRootLayoutName(layout.name).ToString();
                    var groupName = string.IsNullOrEmpty(rootBaseLayoutName) ? "Other" : rootBaseLayoutName + "s";

                    var group = products.children?.FirstOrDefault(x => x.displayName == groupName);
                    if (group == null)
                    {
                        group = AddChild(products, groupName, ref id);
                        if (!string.IsNullOrEmpty(rootBaseLayoutName))
                            group.icon = EditorInputControlLayoutCache.GetIconForLayout(rootBaseLayoutName);
                    }

                    AddControlLayoutItem(layout, group, ref id);
                }

                controls.children?.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                devices.children?.Sort((a, b) => string.Compare(a.displayName, b.displayName));

                if (products.children != null)
                {
                    products.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                    foreach (var productGroup in products.children)
                        productGroup.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                }
            }

            private TreeViewItem AddControlLayoutItem(InputControlLayout layout, TreeViewItem parent, ref int id)
            {
                var item = new LayoutItem
                {
                    parent = parent,
                    depth = parent.depth + 1,
                    id = id++,
                    displayName = layout.displayName ?? layout.name,
                    layoutName = layout.name,
                };
                item.icon = EditorInputControlLayoutCache.GetIconForLayout(layout.name);
                parent.AddChild(item);

                // Header.
                AddChild(item, "Type: " + layout.type?.Name, ref id);
                if (!string.IsNullOrEmpty(layout.m_DisplayName))
                    AddChild(item, "Display Name: " + layout.m_DisplayName, ref id);
                if (!string.IsNullOrEmpty(layout.name))
                    AddChild(item, "Name: " + layout.name, ref id);
                var baseLayouts = StringHelpers.Join(layout.baseLayouts, ", ");
                if (!string.IsNullOrEmpty(baseLayouts))
                    AddChild(item, "Extends: " + baseLayouts, ref id);
                if (layout.stateFormat != 0)
                    AddChild(item, "Format: " + layout.stateFormat, ref id);
                if (layout.m_UpdateBeforeRender != null)
                {
                    var value = layout.m_UpdateBeforeRender.Value ? "Update" : "Disabled";
                    AddChild(item, "Before Render: " + value, ref id);
                }
                if (layout.commonUsages.Count > 0)
                {
                    AddChild(item,
                        "Common Usages: " +
                        string.Join(", ", layout.commonUsages.Select(x => x.ToString()).ToArray()),
                        ref id);
                }
                if (layout.appliedOverrides.Count() > 0)
                {
                    AddChild(item,
                        "Applied Overrides: " +
                        string.Join(", ", layout.appliedOverrides),
                        ref id);
                }

                ////TODO: find a more elegant solution than multiple "Matching Devices" parents when having multiple
                ////      matchers
                // Device matchers.
                foreach (var matcher in EditorInputControlLayoutCache.GetDeviceMatchers(layout.name))
                {
                    var node = AddChild(item, "Matching Devices", ref id);
                    foreach (var pattern in matcher.patterns)
                        AddChild(node, $"{pattern.Key} => \"{pattern.Value}\"", ref id);
                }

                // Controls.
                if (layout.controls.Count > 0)
                {
                    var controls = AddChild(item, "Controls", ref id);
                    foreach (var control in layout.controls)
                        AddControlItem(control, controls, ref id);

                    controls.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                }

                return item;
            }

            private void AddControlItem(InputControlLayout.ControlItem control, TreeViewItem parent, ref int id)
            {
                var item = AddChild(parent, control.variants.IsEmpty() ? control.name : string.Format("{0} ({1})",
                    control.name, control.variants), ref id);

                if (!control.layout.IsEmpty())
                    item.icon = EditorInputControlLayoutCache.GetIconForLayout(control.layout);

                ////TODO: fully merge TreeViewItems from isModifyingExistingControl control layouts into the control they modify

                ////TODO: allow clicking this field to jump to the layout
                if (!control.layout.IsEmpty())
                    AddChild(item, $"Layout: {control.layout}", ref id);
                if (!control.variants.IsEmpty())
                    AddChild(item, $"Variant: {control.variants}", ref id);
                if (!string.IsNullOrEmpty(control.displayName))
                    AddChild(item, $"Display Name: {control.displayName}", ref id);
                if (!string.IsNullOrEmpty(control.shortDisplayName))
                    AddChild(item, $"Short Display Name: {control.shortDisplayName}", ref id);
                if (control.format != 0)
                    AddChild(item, $"Format: {control.format}", ref id);
                if (control.offset != InputStateBlock.InvalidOffset)
                    AddChild(item, $"Offset: {control.offset}", ref id);
                if (control.bit != InputStateBlock.InvalidOffset)
                    AddChild(item, $"Bit: {control.bit}", ref id);
                if (control.sizeInBits != 0)
                    AddChild(item, $"Size In Bits: {control.sizeInBits}", ref id);
                if (control.isArray)
                    AddChild(item, $"Array Size: {control.arraySize}", ref id);
                if (!string.IsNullOrEmpty(control.useStateFrom))
                    AddChild(item, $"Use State From: {control.useStateFrom}", ref id);
                if (!control.defaultState.isEmpty)
                    AddChild(item, $"Default State: {control.defaultState.ToString()}", ref id);
                if (!control.minValue.isEmpty)
                    AddChild(item, $"Min Value: {control.minValue.ToString()}", ref id);
                if (!control.maxValue.isEmpty)
                    AddChild(item, $"Max Value: {control.maxValue.ToString()}", ref id);

                if (control.usages.Count > 0)
                    AddChild(item, "Usages: " + string.Join(", ", control.usages.Select(x => x.ToString()).ToArray()), ref id);
                if (control.aliases.Count > 0)
                    AddChild(item, "Aliases: " + string.Join(", ", control.aliases.Select(x => x.ToString()).ToArray()), ref id);

                if (control.isNoisy || control.isSynthetic)
                {
                    var flags = "Flags: ";
                    if (control.isNoisy)
                        flags += "Noisy";
                    if (control.isSynthetic)
                    {
                        if (control.isNoisy)
                            flags += ", Synthetic";
                        else
                            flags += "Synthetic";
                    }
                    AddChild(item, flags, ref id);
                }

                if (control.parameters.Count > 0)
                {
                    var parameters = AddChild(item, "Parameters", ref id);
                    foreach (var parameter in control.parameters)
                        AddChild(parameters, parameter.ToString(), ref id);
                }

                if (control.processors.Count > 0)
                {
                    var processors = AddChild(item, "Processors", ref id);
                    foreach (var processor in control.processors)
                    {
                        var processorItem = AddChild(processors, processor.name, ref id);
                        foreach (var parameter in processor.parameters)
                            AddChild(processorItem, parameter.ToString(), ref id);
                    }
                }
            }

            private void AddValueItem<TValue>(TreeViewItem parent, string name, TValue value, ref int id)
            {
                var item = new ConfigurationItem
                {
                    id = id++,
                    depth = parent.depth + 1,
                    displayName = $"{name}: {value.ToString()}",
                    name = name
                };
                parent.AddChild(item);
            }

            private void AddEnabledActions(TreeViewItem parent, ref int id)
            {
                foreach (var action in m_EnabledActions)
                {
                    // If we have users, find out if the action is owned by a user. If so, don't display
                    // it separately.
                    var isOwnedByUser = false;
                    foreach (var user in InputUser.all)
                    {
                        var userActions = user.actions;
                        if (userActions != null && userActions.Contains(action))
                        {
                            isOwnedByUser = true;
                            break;
                        }
                    }

                    if (!isOwnedByUser)
                        AddActionItem(parent, action, ref id);
                }

                parent.children?.Sort((a, b) => string.Compare(a.displayName, b.displayName, StringComparison.CurrentCultureIgnoreCase));
            }

            private unsafe void AddActionItem(TreeViewItem parent, InputAction action, ref int id)
            {
                // Add item for action.
                var name = action.actionMap != null ? $"{action.actionMap.name}/{action.name}" : action.name;
                if (!action.enabled)
                    name += " (Disabled)";
                if (action.actionMap != null && action.actionMap.m_Asset != null)
                {
                    name += $" ({action.actionMap.m_Asset.name})";
                }
                else
                {
                    name += " (no asset)";
                }

                var item = AddChild(parent, name, ref id);

                // Grab state.
                var actionMap = action.GetOrCreateActionMap();
                actionMap.ResolveBindingsIfNecessary();
                var state = actionMap.m_State;

                // Add list of resolved controls.
                var actionIndex = action.m_ActionIndexInState;
                var totalBindingCount = state.totalBindingCount;
                for (var i = 0; i < totalBindingCount; ++i)
                {
                    ref var bindingState = ref state.bindingStates[i];
                    if (bindingState.actionIndex != actionIndex)
                        continue;
                    if (bindingState.isComposite)
                        continue;

                    var binding = state.GetBinding(i);
                    var controlCount = bindingState.controlCount;
                    var controlStartIndex = bindingState.controlStartIndex;
                    for (var n = 0; n < controlCount; ++n)
                    {
                        var control = state.controls[controlStartIndex + n];
                        var interactions =
                            StringHelpers.Join(new[] { binding.effectiveInteractions, action.interactions }, ",");

                        var text = control.path;
                        if (!string.IsNullOrEmpty(interactions))
                        {
                            var namesAndParameters = NameAndParameters.ParseMultiple(interactions);
                            text += " [";
                            text += string.Join(",", namesAndParameters.Select(x => x.name));
                            text += "]";
                        }

                        AddChild(item, text, ref id);
                    }
                }
            }

            private TreeViewItem AddChild(TreeViewItem parent, string displayName, ref int id, Texture2D icon = null)
            {
                var item = new TreeViewItem
                {
                    id = id++,
                    depth = parent.depth + 1,
                    displayName = displayName,
                    icon = icon,
                };
                parent.AddChild(item);
                return item;
            }

            private List<InputDeviceDescription> m_UnsupportedDevices;
            private List<InputAction> m_EnabledActions = new List<InputAction>();

            private class DeviceItem : TreeViewItem
            {
                public InputDevice device;
            }

            private class UnsupportedDeviceItem : TreeViewItem
            {
                public InputDeviceDescription description;
            }

            private class ConfigurationItem : TreeViewItem
            {
                public string name;
            }

            private class LayoutItem : TreeViewItem
            {
                public InternedString layoutName;
            }
        }
    }
}
#endif // UNITY_EDITOR
