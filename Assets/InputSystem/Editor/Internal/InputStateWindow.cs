#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

////TODO: find way to automatically dock the state windows next to their InputDeviceDebuggerWindows
////      (probably needs an extension to the editor UI APIs as the only programmatic docking controls
////      seem to be through GetWindow)

////TODO: allow setting a C# struct type that we can use to display the layout of the data

////FIXME: need to prevent extra controls appended at end from reading beyond the state buffer

namespace ISX
{
    // Additional window that we can pop open to inspect raw state (either on events or on controls/devices).
    internal class InputStateWindow : EditorWindow
    {
        private const int kBytesPerHexGroup = 1;
        private const int kHexGroupsPerLine = 8;
        private const int kHexDumpLineHeight = 25;
        private const int kOffsetLabelWidth = 30;
        private const int kHexGroupWidth = 25;

        public unsafe void InitializeWithEvent(InputEventPtr eventPtr, InputControl control)
        {
            // Must be an event carrying state.
            Debug.Assert(eventPtr.IsA<StateEvent>() || eventPtr.IsA<DeltaStateEvent>());

            ////TODO: support delta events
            if (eventPtr.IsA<DeltaStateEvent>())
                throw new NotImplementedException("delta event support not yet implemented");

            m_Control = control;

            // Copy event data.
            var stateEventPtr = StateEvent.From(eventPtr);
            var stateSize = stateEventPtr->stateSizeInBytes;
            var buffer = new byte[stateSize];
            fixed(byte* stateDataPtr = buffer)
            {
                UnsafeUtility.MemCpy(new IntPtr(stateDataPtr), stateEventPtr->state, stateSize);
            }
            m_StateBuffers = new byte[1][];
            m_StateBuffers[0] = buffer;
            m_SelectedStateBuffer = BufferSelector.Default;
        }

        public unsafe void InitializeWithControl(InputControl control)
        {
            m_Control = control;
            m_SelectedStateBuffer = BufferSelector.Default;

            var bufferChoices = new List<GUIContent>();
            var bufferChoiceValues = new List<int>();

            // Copy front and back buffer state for each update that has valid buffers.
            var device = control.device;
            var stateSize = control.m_StateBlock.alignedSizeInBytes;
            var stateOffset = control.m_StateBlock.byteOffset;
            m_StateBuffers = new byte[(int)BufferSelector.COUNT][];
            for (var i = 0; i < (int)BufferSelector.COUNT; ++i)
            {
                var selector = (BufferSelector)i;
                var deviceState = TryGetDeviceState(device, selector);
                if (deviceState == IntPtr.Zero)
                    continue;

                var buffer = new byte[stateSize];
                fixed(byte* stateDataPtr = buffer)
                {
                    UnsafeUtility.MemCpy(new IntPtr(stateDataPtr), deviceState + (int)stateOffset, stateSize);
                }
                m_StateBuffers[i] = buffer;

                if (m_StateBuffers[(int)m_SelectedStateBuffer] == null)
                    m_SelectedStateBuffer = selector;

                bufferChoices.Add(Contents.bufferChoices[i]);
                bufferChoiceValues.Add(i);
            }

            m_BufferChoices = bufferChoices.ToArray();
            m_BufferChoiceValues = bufferChoiceValues.ToArray();
        }

        private static IntPtr TryGetDeviceState(InputDevice device, BufferSelector selector)
        {
            var manager = InputSystem.s_Manager;
            var deviceIndex = device.m_DeviceIndex;

            switch (selector)
            {
                case BufferSelector.DynamicUpdateFrontBuffer:
                    if (manager.m_StateBuffers.m_DynamicUpdateBuffers.valid)
                        return manager.m_StateBuffers.m_DynamicUpdateBuffers.GetFrontBuffer(deviceIndex);
                    break;
                case BufferSelector.DynamicUpdateBackBuffer:
                    if (manager.m_StateBuffers.m_DynamicUpdateBuffers.valid)
                        return manager.m_StateBuffers.m_DynamicUpdateBuffers.GetBackBuffer(deviceIndex);
                    break;
                case BufferSelector.FixedUpdateFrontBuffer:
                    if (manager.m_StateBuffers.m_FixedUpdateBuffers.valid)
                        return manager.m_StateBuffers.m_FixedUpdateBuffers.GetFrontBuffer(deviceIndex);
                    break;
                case BufferSelector.FixedUpdateBackBuffer:
                    if (manager.m_StateBuffers.m_FixedUpdateBuffers.valid)
                        return manager.m_StateBuffers.m_FixedUpdateBuffers.GetBackBuffer(deviceIndex);
                    break;
                case BufferSelector.EditorUpdateFrontBuffer:
                    if (manager.m_StateBuffers.m_EditorUpdateBuffers.valid)
                        return manager.m_StateBuffers.m_EditorUpdateBuffers.GetFrontBuffer(deviceIndex);
                    break;
                case BufferSelector.EditorUpdateBackBuffer:
                    if (manager.m_StateBuffers.m_EditorUpdateBuffers.valid)
                        return manager.m_StateBuffers.m_EditorUpdateBuffers.GetBackBuffer(deviceIndex);
                    break;
            }

            return IntPtr.Zero;
        }

        public void OnGUI()
        {
            if (m_Control == null)
                m_ShowRawBytes = true;

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_ShowRawBytes = GUILayout.Toggle(m_ShowRawBytes, Contents.showRawBytes, EditorStyles.toolbarButton,
                    GUILayout.Width(150));

            // If we have multiple state buffers to choose from, add dropdown that allows
            // selecting which buffer to display.
            if (m_StateBuffers.Length > 1)
            {
                var selectedBuffer = (BufferSelector)EditorGUILayout.IntPopup((int)m_SelectedStateBuffer, m_BufferChoices,
                        m_BufferChoiceValues, EditorStyles.toolbarPopup);
                if (selectedBuffer != m_SelectedStateBuffer)
                {
                    m_SelectedStateBuffer = selectedBuffer;
                    m_ControlTree = null;
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (m_ShowRawBytes)
            {
                DrawHexDump();
            }
            else
            {
                if (m_ControlTree == null)
                {
                    m_ControlTree = InputControlTreeView.Create(m_Control, ref m_ControlTreeState, ref m_ControlTreeHeaderState);
                    m_ControlTree.stateBuffer = m_StateBuffers[(int)m_SelectedStateBuffer];
                    m_ControlTree.ExpandAll();
                }

                m_ControlTreeScrollPosition = EditorGUILayout.BeginScrollView(m_ControlTreeScrollPosition);
                var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
                m_ControlTree.OnGUI(rect);
                EditorGUILayout.EndScrollView();
            }
        }

        public void DrawHexDump()
        {
            m_HexDumpScrollPosition = EditorGUILayout.BeginScrollView(m_HexDumpScrollPosition);

            var stateBuffer = m_StateBuffers[(int)m_SelectedStateBuffer];
            var numBytes = stateBuffer.Length;
            var numHexGroups = numBytes / kBytesPerHexGroup + (numBytes % kBytesPerHexGroup > 0 ? 1 : 0);
            var numLines = numHexGroups / kHexGroupsPerLine + (numHexGroups % kHexGroupsPerLine > 0 ? 1 : 0);
            var currentOffset = 0;
            var currentLineRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
            currentLineRect.height = kHexDumpLineHeight;
            var currentHexGroup = 0;
            var currentByte = 0;

            ////REVIEW: what would be totally awesome is if this not just displayed a hex dump but also the correlation to current
            ////        control offset assignments

            for (var line = 0; line < numLines; ++line)
            {
                // Draw offset.
                var offsetLabelRect = currentLineRect;
                offsetLabelRect.width = kOffsetLabelWidth;
                GUI.Label(offsetLabelRect, currentOffset.ToString(), Styles.offsetLabel);
                currentOffset += kBytesPerHexGroup * kHexGroupsPerLine;

                // Draw hex groups.
                var hexGroupRect = offsetLabelRect;
                hexGroupRect.x += kOffsetLabelWidth + 10;
                hexGroupRect.width = kHexGroupWidth;
                for (var group = 0;
                     group < kHexGroupsPerLine && currentHexGroup < numHexGroups;
                     ++group, ++currentHexGroup)
                {
                    // Convert bytes to hex.
                    var hex = string.Empty;
                    for (var i = 0; i < kBytesPerHexGroup; ++i)
                    {
                        if (currentByte >= numBytes)
                            hex = "  " + hex;
                        else
                            hex = stateBuffer[currentByte].ToString("X2") + hex;
                        ++currentByte;
                    }

                    ////TODO: draw alternating backgrounds for the hex groups

                    GUI.Label(hexGroupRect, hex);
                    hexGroupRect.x += kHexGroupWidth;
                }

                currentLineRect.y += kHexDumpLineHeight;
            }

            EditorGUILayout.EndScrollView();
        }

        // We copy the state we're inspecting to a buffer we own so that we're safe
        // against any mutations.
        // When inspecting controls (as opposed to events), we copy all their various
        // state buffers and allow switching between them.
        [SerializeField] private byte[][] m_StateBuffers;
        [SerializeField] private BufferSelector m_SelectedStateBuffer;

        [SerializeField] private bool m_ShowRawBytes;
        [SerializeField] private TreeViewState m_ControlTreeState;
        [SerializeField] private MultiColumnHeaderState m_ControlTreeHeaderState;
        [SerializeField] private Vector2 m_ControlTreeScrollPosition;
        [SerializeField] private Vector2 m_HexDumpScrollPosition;

        [NonSerialized] private InputControlTreeView m_ControlTree;
        [NonSerialized] private GUIContent[] m_BufferChoices;
        [NonSerialized] private int[] m_BufferChoiceValues;

        ////FIXME: we lose this on domain reload; how should we recover?
        [NonSerialized] private InputControl m_Control;

        private enum BufferSelector
        {
            DynamicUpdateFrontBuffer,
            DynamicUpdateBackBuffer,
            FixedUpdateFrontBuffer,
            FixedUpdateBackBuffer,
            EditorUpdateFrontBuffer,
            EditorUpdateBackBuffer,
            COUNT,
            Default = DynamicUpdateFrontBuffer
        }

        private static class Styles
        {
            public static GUIStyle offsetLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperRight,
                fontStyle = FontStyle.BoldAndItalic,
                font = EditorStyles.boldFont,
                fontSize = EditorStyles.boldFont.fontSize - 2,
                normal = new GUIStyleState { textColor = Color.black }
            };
        }

        private static class Contents
        {
            public static GUIContent showRawBytes = new GUIContent("Display Raw Bytes");
            public static GUIContent[] bufferChoices =
            {
                new GUIContent("Dynamic Update (Current)"),
                new GUIContent("Dynamic Update (Previous)"),
                new GUIContent("Fixed Update (Current)"),
                new GUIContent("Fixed Update (Previous)"),
                new GUIContent("Editor (Current)"),
                new GUIContent("Editor (Previous)")
            };
        }
    }
}
#endif // UNITY_EDITOR
