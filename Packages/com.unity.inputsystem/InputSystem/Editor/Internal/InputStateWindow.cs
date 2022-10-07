#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.InputSystem.LowLevel;

////TODO: add ability to single-step through events

////TODO: annotate raw memory view with control offset and ranges (probably easiest to put the control tree and raw memory view side by side)

////TODO: find way to automatically dock the state windows next to their InputDeviceDebuggerWindows
////      (probably needs an extension to the editor UI APIs as the only programmatic docking controls
////      seem to be through GetWindow)

////TODO: allow setting a C# struct type that we can use to display the layout of the data

////TODO: for delta state events, highlight the controls included in the event (or show only those)

////FIXME: need to prevent extra controls appended at end from reading beyond the state buffer

namespace UnityEngine.InputSystem.Editor
{
    // Additional window that we can pop open to inspect raw state (either on events or on controls/devices).
    internal class InputStateWindow : EditorWindow
    {
        private const int kBytesPerHexGroup = 1;
        private const int kHexGroupsPerLine = 8;
        private const int kHexDumpLineHeight = 25;
        private const int kOffsetLabelWidth = 30;
        private const int kHexGroupWidth = 25;
        private const int kBitGroupWidth = 75;

        void Update()
        {
            if (m_PollControlState && m_Control != null)
            {
                PollBuffersFromControl(m_Control);
                Repaint();
            }
        }

        public void InitializeWithEvent(InputEventPtr eventPtr, InputControl control)
        {
            m_Control = control;
            m_PollControlState = false;
            m_StateBuffers = new byte[1][];
            m_StateBuffers[0] = GetEventStateBuffer(eventPtr, control);
            m_SelectedStateBuffer = 0;

            titleContent = new GUIContent(control.displayName);
        }

        public void InitializeWithEvents(InputEventPtr[] eventPtrs, InputControl control)
        {
            var numEvents = eventPtrs.Length;

            m_Control = control;
            m_PollControlState = false;
            m_StateBuffers = new byte[numEvents][];
            for (var i = 0; i < numEvents; ++i)
                m_StateBuffers[i] = GetEventStateBuffer(eventPtrs[i], control);
            m_CompareStateBuffers = true;
            m_ShowDifferentOnly = true;

            titleContent = new GUIContent(control.displayName);
        }

        private unsafe byte[] GetEventStateBuffer(InputEventPtr eventPtr, InputControl control)
        {
            // Must be an event carrying state.
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                throw new ArgumentException("Event must be state or delta event", nameof(eventPtr));

            // Get state data.
            void* dataPtr;
            uint dataSize;
            uint stateSize;
            uint stateOffset = 0;

            if (eventPtr.IsA<DeltaStateEvent>())
            {
                var deltaEventPtr = DeltaStateEvent.From(eventPtr);
                stateSize = control.stateBlock.alignedSizeInBytes;
                stateOffset = deltaEventPtr->stateOffset;
                dataPtr = deltaEventPtr->deltaState;
                dataSize = deltaEventPtr->deltaStateSizeInBytes;
            }
            else
            {
                var stateEventPtr = StateEvent.From(eventPtr);
                dataSize = stateSize = stateEventPtr->stateSizeInBytes;
                dataPtr = stateEventPtr->state;
            }

            // Copy event data.
            var buffer = new byte[stateSize];
            fixed(byte* bufferPtr = buffer)
            {
                UnsafeUtility.MemCpy(bufferPtr + stateOffset, dataPtr, dataSize);
            }

            return buffer;
        }

        public unsafe void InitializeWithControl(InputControl control)
        {
            m_Control = control;
            m_PollControlState = true;
            m_SelectedStateBuffer = (int)BufferSelector.Default;

            PollBuffersFromControl(control, selectBuffer: true);

            titleContent = new GUIContent(control.displayName);
        }

        private unsafe void PollBuffersFromControl(InputControl control, bool selectBuffer = false)
        {
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
                if (deviceState == null)
                    continue;

                var buffer = new byte[stateSize];
                fixed(byte* stateDataPtr = buffer)
                {
                    UnsafeUtility.MemCpy(stateDataPtr, (byte*)deviceState + (int)stateOffset, stateSize);
                }
                m_StateBuffers[i] = buffer;

                if (selectBuffer && m_StateBuffers[m_SelectedStateBuffer] == null)
                    m_SelectedStateBuffer = (int)selector;

                bufferChoices.Add(Contents.bufferChoices[i]);
                bufferChoiceValues.Add(i);
            }

            m_BufferChoices = bufferChoices.ToArray();
            m_BufferChoiceValues = bufferChoiceValues.ToArray();
        }

        private static unsafe void* TryGetDeviceState(InputDevice device, BufferSelector selector)
        {
            var manager = InputSystem.s_Manager;
            var deviceIndex = device.m_DeviceIndex;

            switch (selector)
            {
                case BufferSelector.PlayerUpdateFrontBuffer:
                    if (manager.m_StateBuffers.m_PlayerStateBuffers.valid)
                        return manager.m_StateBuffers.m_PlayerStateBuffers.GetFrontBuffer(deviceIndex);
                    break;
                case BufferSelector.PlayerUpdateBackBuffer:
                    if (manager.m_StateBuffers.m_PlayerStateBuffers.valid)
                        return manager.m_StateBuffers.m_PlayerStateBuffers.GetBackBuffer(deviceIndex);
                    break;
                case BufferSelector.EditorUpdateFrontBuffer:
                    if (manager.m_StateBuffers.m_EditorStateBuffers.valid)
                        return manager.m_StateBuffers.m_EditorStateBuffers.GetFrontBuffer(deviceIndex);
                    break;
                case BufferSelector.EditorUpdateBackBuffer:
                    if (manager.m_StateBuffers.m_EditorStateBuffers.valid)
                        return manager.m_StateBuffers.m_EditorStateBuffers.GetBackBuffer(deviceIndex);
                    break;
                case BufferSelector.NoiseMaskBuffer:
                    return manager.m_StateBuffers.noiseMaskBuffer;
                case BufferSelector.ResetMaskBuffer:
                    return manager.m_StateBuffers.resetMaskBuffer;
            }

            return null;
        }

        public void OnGUI()
        {
            if (m_Control == null)
                m_ShowRawBytes = true;

            // If our state is no longer valid, just close the window.
            if (m_StateBuffers == null)
            {
                Close();
                return;
            }

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_PollControlState = GUILayout.Toggle(m_PollControlState, Contents.live, EditorStyles.toolbarButton);

            m_ShowRawBytes = GUILayout.Toggle(m_ShowRawBytes, Contents.showRawMemory, EditorStyles.toolbarButton,
                GUILayout.Width(150));

            m_ShowAsBits = GUILayout.Toggle(m_ShowAsBits, Contents.showBits, EditorStyles.toolbarButton);

            if (m_CompareStateBuffers)
            {
                var showDifferentOnly = GUILayout.Toggle(m_ShowDifferentOnly, Contents.showDifferentOnly,
                    EditorStyles.toolbarButton, GUILayout.Width(150));
                if (showDifferentOnly != m_ShowDifferentOnly && m_ControlTree != null)
                {
                    m_ControlTree.showDifferentOnly = showDifferentOnly;
                    m_ControlTree.Reload();
                }

                m_ShowDifferentOnly = showDifferentOnly;
            }

            // If we have multiple state buffers to choose from and we're not comparing them to each other,
            // add dropdown that allows selecting which buffer to display.
            if (m_StateBuffers.Length > 1 && !m_CompareStateBuffers)
            {
                var selectedBuffer = EditorGUILayout.IntPopup(m_SelectedStateBuffer, m_BufferChoices,
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
                    if (m_CompareStateBuffers)
                    {
                        m_ControlTree = InputControlTreeView.Create(m_Control, m_StateBuffers.Length, ref m_ControlTreeState, ref m_ControlTreeHeaderState);
                        m_ControlTree.multipleStateBuffers = m_StateBuffers;
                        m_ControlTree.showDifferentOnly = m_ShowDifferentOnly;
                    }
                    else
                    {
                        m_ControlTree = InputControlTreeView.Create(m_Control, 1, ref m_ControlTreeState, ref m_ControlTreeHeaderState);
                        m_ControlTree.stateBuffer = m_StateBuffers[m_SelectedStateBuffer];
                    }
                    m_ControlTree.Reload();
                    m_ControlTree.ExpandAll();
                }

                var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
                m_ControlTree.OnGUI(rect);
            }
        }

        private byte[] TryGetBackBufferForCurrentlySelected()
        {
            if (m_StateBuffers.Length != (int)BufferSelector.COUNT)
                return null;

            switch ((BufferSelector)m_SelectedStateBuffer)
            {
                case BufferSelector.PlayerUpdateFrontBuffer:
                    return m_StateBuffers[(int)BufferSelector.PlayerUpdateBackBuffer];
                case BufferSelector.EditorUpdateFrontBuffer:
                    return m_StateBuffers[(int)BufferSelector.EditorUpdateBackBuffer];
                default:
                    return null;
            }
        }

        private string FormatByte(byte value)
        {
            if (m_ShowAsBits)
                return Convert.ToString(value, 2).PadLeft(8, '0');
            else
                return value.ToString("X2");
        }

        ////TODO: support dumping multiple state side-by-side when comparing
        private void DrawHexDump()
        {
            m_HexDumpScrollPosition = EditorGUILayout.BeginScrollView(m_HexDumpScrollPosition);

            var stateBuffer = m_StateBuffers[m_SelectedStateBuffer];
            var prevStateBuffer = TryGetBackBufferForCurrentlySelected();
            if (prevStateBuffer != null && prevStateBuffer.Length != stateBuffer.Length) // we assume they're same length, otherwise ignore prev buffer
                prevStateBuffer = null;
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
                hexGroupRect.width = m_ShowAsBits ? kBitGroupWidth : kHexGroupWidth;
                for (var group = 0;
                     group < kHexGroupsPerLine && currentHexGroup < numHexGroups;
                     ++group, ++currentHexGroup)
                {
                    // Convert bytes to hex.
                    var hex = string.Empty;

                    for (var i = 0; i < kBytesPerHexGroup; ++i, ++currentByte)
                    {
                        if (currentByte >= numBytes)
                        {
                            hex += "  ";
                            continue;
                        }

                        var current = FormatByte(stateBuffer[currentByte]);
                        if (prevStateBuffer == null)
                        {
                            hex += current;
                            continue;
                        }

                        var prev = FormatByte(prevStateBuffer[currentByte]);
                        if (prev.Length != current.Length)
                        {
                            hex += current;
                            continue;
                        }

                        for (var j = 0; j < current.Length; ++j)
                        {
                            if (current[j] != prev[j])
                                hex += $"<color=#C84B31FF>{current[j]}</color>";
                            else
                                hex += current[j];
                        }
                    }

                    ////TODO: draw alternating backgrounds for the hex groups

                    GUI.Label(hexGroupRect, hex, style: Styles.hexLabel);
                    hexGroupRect.x += m_ShowAsBits ? kBitGroupWidth : kHexGroupWidth;
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
        [SerializeField] private int m_SelectedStateBuffer;
        [SerializeField] private bool m_CompareStateBuffers;
        [SerializeField] private bool m_ShowDifferentOnly;
        [SerializeField] private bool m_ShowRawBytes;
        [SerializeField] private bool m_ShowAsBits;
        [SerializeField] private bool m_PollControlState;
        [SerializeField] private TreeViewState m_ControlTreeState;
        [SerializeField] private MultiColumnHeaderState m_ControlTreeHeaderState;
        [SerializeField] private Vector2 m_HexDumpScrollPosition;

        [NonSerialized] private InputControlTreeView m_ControlTree;
        [NonSerialized] private GUIContent[] m_BufferChoices;
        [NonSerialized] private int[] m_BufferChoiceValues;

        ////FIXME: we lose this on domain reload; how should we recover?
        [NonSerialized] private InputControl m_Control;

        private enum BufferSelector
        {
            PlayerUpdateFrontBuffer,
            PlayerUpdateBackBuffer,
            EditorUpdateFrontBuffer,
            EditorUpdateBackBuffer,
            NoiseMaskBuffer,
            ResetMaskBuffer,
            COUNT,
            Default = PlayerUpdateFrontBuffer
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

            public static GUIStyle hexLabel = new GUIStyle
            {
                fontStyle = FontStyle.Normal,
                font = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font,
                fontSize = EditorStyles.label.fontSize + 2,
                normal = new GUIStyleState { textColor = Color.white },
                richText = true
            };
        }

        private static class Contents
        {
            public static GUIContent live = new GUIContent("Live");
            public static GUIContent showRawMemory = new GUIContent("Display Raw Memory");
            public static GUIContent showBits = new GUIContent("Bits/Hex");
            public static GUIContent showDifferentOnly = new GUIContent("Show Only Differences");
            public static GUIContent[] bufferChoices =
            {
                new GUIContent("Player (Current)"),
                new GUIContent("Player (Previous)"),
                new GUIContent("Editor (Current)"),
                new GUIContent("Editor (Previous)"),
                new GUIContent("Noise Mask"),
                new GUIContent("Reset Mask")
            };
        }
    }
}
#endif // UNITY_EDITOR
