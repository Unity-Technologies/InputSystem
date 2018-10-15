#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;

////TODO: show input users

////TODO: append " (Disabled) to disabled devices and grey them out

////TODO: split 'Local' and 'Remote' at root rather than inside subnodes

////TODO: Ideally, I'd like all separate EditorWindows opened by the InputDiagnostics to automatically
////      be docked into the container window of InputDebuggerWindow

////TODO: add view to tweak InputConfiguration interactively in the editor

////TODO: display icons on devices depending on type of device

////TODO: make configuration update when changed

////TODO: refresh when unrecognized device pops up

////TODO: context menu
////      devices: open debugger window, remove device, disable device
////      layouts: copy as json, remove layout
////      actions: disable action

namespace UnityEngine.Experimental.Input.Editor
{
    // Allows looking at input activity in the editor.
    internal class InputDebuggerWindow : EditorWindow, ISerializationCallbackReceiver
    {
        private static int s_Disabled;
        private static InputDebuggerWindow s_Instance;

        [MenuItem("Window/Input Debugger", false, 2100)]
        public static void Init()
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
                s_Instance.InstallHooks();

            ////REVIEW: technically, we'd have to do a refresh here but that'd mean that in the current setup
            ////        we'd do a refresh after every single test; find a better solution
        }

        public static void Disable()
        {
            ++s_Disabled;
            if (s_Disabled == 1 && s_Instance != null)
                s_Instance.UninstallHooks();
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
            Refresh();
        }

        private string OnFindLayout(int deviceId, ref InputDeviceDescription description, string matchedLayout,
            IInputRuntime runtime)
        {
            // If there's no matched layout, there's a chance this device will go in
            // the unsupported list. There's no direct notification for that so we
            // pre-emptively trigger a refresh.
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
        }

        private void UninstallHooks()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            InputSystem.onLayoutChange -= OnLayoutChange;
            InputSystem.onFindLayoutForDevice -= OnFindLayout;
            InputSystem.onActionChange -= OnActionChange;
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

            // This also brings us back online after a domain reload.
            if (!m_Initialized)
                Initialize();
            else if (m_NeedReload)
            {
                m_TreeView.Reload();
                m_NeedReload = false;
            }

            DrawToolbarGUI();

            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
            m_TreeView.OnGUI(rect);
        }

        private void DrawToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Enable/disable diagnostics mode.
            var diagnosticsMode = GUILayout.Toggle(m_DiagnosticsMode, Contents.diagnosticsModeContent, EditorStyles.toolbarButton);
            if (diagnosticsMode != m_DiagnosticsMode)
            {
                if (diagnosticsMode)
                {
                    if (m_Diagnostics == null)
                        m_Diagnostics = new InputDiagnostics();
                    InputSystem.s_Manager.m_Diagnostics = m_Diagnostics;
                }
                else
                {
                    InputSystem.s_Manager.m_Diagnostics = null;
                }
                m_DiagnosticsMode = diagnosticsMode;
            }

            InputConfiguration.LockInputToGame = GUILayout.Toggle(InputConfiguration.LockInputToGame,
                Contents.lockInputToGameContent, EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        [SerializeField] private bool m_DiagnosticsMode;
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
            public static GUIContent lockInputToGameContent = new GUIContent("Lock Input to Game");
            public static GUIContent diagnosticsModeContent = new GUIContent("Enable Diagnostics");
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            s_Instance = this;
        }

        class InputSystemTreeView : TreeView
        {
            public TreeViewItem actionsItem { get; private set; }
            public TreeViewItem devicesItem { get; private set; }
            public TreeViewItem layoutsItem { get; private set; }
            public TreeViewItem configurationItem { get; private set; }
            public TreeViewItem usersItem { get; private set; }

            public InputSystemTreeView(TreeViewState state)
                : base(state)
            {
                Reload();
            }

            protected override void ContextClickedItem(int id)
            {
            }

            protected override void DoubleClickedItem(int id)
            {
                var item = FindItem(id, rootItem);
                if (item == null)
                    return;

                var deviceItem = item as DeviceItem;
                if (deviceItem != null)
                {
                    InputDeviceDebuggerWindow.CreateOrShowExisting(deviceItem.device);
                    return;
                }
            }

            protected override TreeViewItem BuildRoot()
            {
                var id = 0;

                var root = new TreeViewItem
                {
                    id = id++,
                    depth = -1
                };

                // Actions.
                m_EnabledActions.Clear();
                InputSystem.ListEnabledActions(m_EnabledActions);
                if (m_EnabledActions.Count > 0)
                {
                    actionsItem = AddChild(root, string.Format("Actions ({0})", m_EnabledActions.Count), ref id);
                    AddEnabledActions(actionsItem, ref id);
                }

                // Users.
                ////TODO

                // Devices.
                var devices = InputSystem.devices;
                devicesItem = AddChild(root, string.Format("Devices ({0})", devices.Count), ref id);
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
                        AddDevices(playerNode, devices, ref id, "Remote" + player.playerId + InputControlLayout.kNamespaceQualifier);
                    }
                }
                else
                {
                    // We don't have remote devices so don't add an extra group for local devices.
                    // Put them all directly underneath the "Devices" node.
                    AddDevices(devicesItem, devices, ref id);
                }

                if (m_UnsupportedDevices == null)
                    m_UnsupportedDevices = new List<InputDeviceDescription>();
                m_UnsupportedDevices.Clear();
                InputSystem.GetUnsupportedDevices(m_UnsupportedDevices);
                if (m_UnsupportedDevices.Count > 0)
                {
                    var parent = haveRemotes ? localDevicesNode : devicesItem;
                    var unsupportedDevicesNode = AddChild(parent, string.Format("Unsupported ({0})", m_UnsupportedDevices.Count), ref id);
                    foreach (var device in m_UnsupportedDevices)
                        AddChild(unsupportedDevicesNode, device.ToString(), ref id);
                    unsupportedDevicesNode.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                }

                var disconnectedDevices = InputSystem.disconnectedDevices;
                if (disconnectedDevices.Count > 0)
                {
                    var parent = haveRemotes ? localDevicesNode : devicesItem;
                    var disconnectedDevicesNode = AddChild(parent, string.Format("Disconnected ({0})", disconnectedDevices.Count), ref id);
                    foreach (var device in disconnectedDevices)
                        AddChild(disconnectedDevicesNode, device.ToString(), ref id);
                    disconnectedDevicesNode.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                }

                // Layouts.
                layoutsItem = AddChild(root, "Layouts", ref id);
                AddControlLayouts(layoutsItem, ref id);

                ////FIXME: this shows local configuration only
                // Configuration.
                configurationItem = AddChild(root, "Configuration", ref id);
                AddConfigurationItem(configurationItem, "ButtonPressPoint", InputConfiguration.ButtonPressPoint, ref id);
                AddConfigurationItem(configurationItem, "DeadzoneMin", InputConfiguration.DeadzoneMin, ref id);
                AddConfigurationItem(configurationItem, "DeadzoneMax", InputConfiguration.DeadzoneMax, ref id);
                AddConfigurationItem(configurationItem, "LockInputToGame", InputConfiguration.LockInputToGame, ref id);
                configurationItem.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));

                return root;
            }

            private void AddDevices(TreeViewItem parent, IEnumerable<InputDevice> devices, ref int id, string namePrefix = null)
            {
                foreach (var device in devices)
                {
                    if (namePrefix != null)
                    {
                        if (!device.name.StartsWith(namePrefix))
                            continue;
                    }
                    else if (device.name.Contains(InputControlLayout.kNamespaceQualifier))
                        continue;

                    var item = new DeviceItem
                    {
                        id = id++,
                        depth = parent.depth + 1,
                        displayName = namePrefix != null ? device.name.Substring(namePrefix.Length) : device.name,
                        device = device,
                    };
                    parent.AddChild(item);
                }

                if (parent.children != null)
                    parent.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
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
                    if (string.IsNullOrEmpty(rootBaseLayoutName))
                        rootBaseLayoutName = "Other";
                    else
                        rootBaseLayoutName += "s";

                    var group = products.children != null
                        ? products.children.FirstOrDefault(x => x.displayName == rootBaseLayoutName)
                        : null;
                    if (group == null)
                        group = AddChild(products, rootBaseLayoutName, ref id);

                    AddControlLayoutItem(layout, group, ref id);
                }

                if (controls.children != null)
                    controls.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                if (devices.children != null)
                    devices.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                if (products.children != null)
                {
                    products.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                    foreach (var productGroup in products.children)
                        productGroup.children.Sort((a, b) => string.Compare(a.displayName, b.displayName));
                }
            }

            private TreeViewItem AddControlLayoutItem(InputControlLayout layout, TreeViewItem parent, ref int id)
            {
                var item = AddChild(parent, layout.name, ref id);

                // Header.
                AddChild(item, "Type: " + layout.type.Name, ref id);
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
                        string.Join(", ", layout.commonUsages.Select(x => x.ToString()).ToArray()), ref id);
                }

                ////TODO: find a more elegant solution than multiple "Matching Devices" parents when having multiple
                ////      matchers
                // Device matchers.
                foreach (var matcher in EditorInputControlLayoutCache.GetDeviceMatchers(layout.name))
                {
                    var node = AddChild(item, "Matching Devices", ref id);
                    foreach (var pattern in matcher.patterns)
                        AddChild(node, string.Format("{0} => \"{1}\"", pattern.Key, pattern.Value), ref id);
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

                ////TODO: fully merge TreeViewItems from isModifyingChildControlByPath control layouts into the control they modify

                ////TODO: allow clicking this field to jump to the layout
                if (!control.layout.IsEmpty())
                    AddChild(item, string.Format("Layout: {0}", control.layout), ref id);
                if (!control.variants.IsEmpty())
                    AddChild(item, string.Format("Variant: {0}", control.variants), ref id);
                if (control.format != 0)
                    AddChild(item, string.Format("Format: {0}", control.format), ref id);
                if (control.offset != InputStateBlock.kInvalidOffset)
                    AddChild(item, string.Format("Offset: {0}", control.offset), ref id);
                if (control.bit != InputStateBlock.kInvalidOffset)
                    AddChild(item, string.Format("Bit: {0}", control.bit), ref id);
                if (control.sizeInBits != 0)
                    AddChild(item, string.Format("Size In Bits: {0}", control.sizeInBits), ref id);
                if (control.isArray)
                    AddChild(item, string.Format("Array Size: {0}", control.arraySize), ref id);
                if (!string.IsNullOrEmpty(control.useStateFrom))
                    AddChild(item, string.Format("Use State From: {0}", control.useStateFrom), ref id);
                if (!control.defaultState.isEmpty)
                    AddChild(item, string.Format("Default State: {0}", control.defaultState.ToString()), ref id);

                if (control.usages.Count > 0)
                    AddChild(item, "Usages: " + string.Join(", ", control.usages.Select(x => x.ToString()).ToArray()), ref id);
                if (control.aliases.Count > 0)
                    AddChild(item, "Aliases: " + string.Join(", ", control.aliases.Select(x => x.ToString()).ToArray()), ref id);

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

            private void AddConfigurationItem<TValue>(TreeViewItem parent, string name, TValue value, ref int id)
            {
                var item = new ConfigurationItem
                {
                    id = id++,
                    depth = parent.depth + 1,
                    displayName = string.Format("{0}: {1}", name, value.ToString()),
                    name = name
                };
                parent.AddChild(item);
            }

            private void AddEnabledActions(TreeViewItem parent, ref int id)
            {
                foreach (var action in m_EnabledActions)
                {
                    // Add item for action.
                    var set = action.actionMap;
                    var setName = set != null ? set.name + "/" : string.Empty;
                    var item = AddChild(parent, setName + action.name, ref id);

                    // Add list of resolved controls.
                    foreach (var control in action.controls)
                        AddChild(item, control.path, ref id);
                }

                if (parent.children != null)
                    parent.children.Sort((a, b) => string.Compare(a.displayName, b.displayName, StringComparison.CurrentCultureIgnoreCase));
            }

            private TreeViewItem AddChild(TreeViewItem parent, string displayName, ref int id)
            {
                var item = new TreeViewItem
                {
                    id = id++,
                    depth = parent.depth + 1,
                    displayName = displayName
                };
                parent.AddChild(item);
                return item;
            }

            private List<InputDeviceDescription> m_UnsupportedDevices;
            private List<InputAction> m_EnabledActions = new List<InputAction>();

            class DeviceItem : TreeViewItem
            {
                public InputDevice device;
            }

            class ConfigurationItem : TreeViewItem
            {
                public string name;
            }

            //class ActionItem : TreeViewItem
            //{
            //public InputAction action;
            //}
        }
    }
}
#endif // UNITY_EDITOR
