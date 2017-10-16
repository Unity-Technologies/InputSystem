#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

////TODO: find way to automatically dock the state windows next to their InputDeviceDebuggerWindows
////      (probably needs an extension to the editor UI APIs as the only programmatic docking controls
////      seem to be through GetWindow)

namespace ISX
{
    // Additional window that we can pop open to inspect or even edit raw state (either
    // on events or on controls/devices).
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
            Debug.Assert(eventPtr.IsA<StateEvent>() || eventPtr.IsA<DeltaEvent>());

            ////TODO: support delta events
            if (eventPtr.IsA<DeltaEvent>())
                throw new NotImplementedException("delta event support not yet implemented");

            m_Control = control;

            // Copy event data.
            var eventSize = eventPtr.sizeInBytes;
            m_StateData = new byte[eventSize];
            fixed(byte* stateDataPtr = m_StateData)
            {
                var stateEventPtr = StateEvent.From(eventPtr);
                UnsafeUtility.MemCpy(new IntPtr(stateDataPtr), stateEventPtr->state, eventSize);
            }
        }

        public unsafe void InitializeWithControl(InputControl control)
        {
            m_Control = control;

            ////TODO: have a dropdown that allows inspecting all available state for the control (for all update slices including editor)

            // Copy current state.
            var stateSize = control.m_StateBlock.alignedSizeInBytes;
            m_StateData = new byte[stateSize];
            fixed(byte* stateDataPtr = m_StateData)
            {
                UnsafeUtility.MemCpy(new IntPtr(stateDataPtr), control.currentValuePtr, stateSize);
            }
        }

        public void OnGUI()
        {
            if (m_Control == null)
                m_ShowRawBytes = true;

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_ShowRawBytes = GUILayout.Toggle(m_ShowRawBytes, Contents.showRawBytes, EditorStyles.toolbarButton,
                    GUILayout.Width(150));
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
                    m_ControlTree.stateBuffer = m_StateData;
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

            var numBytes = m_StateData.Length;
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
                            hex = m_StateData[currentByte].ToString("X2") + hex;
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
        [SerializeField] private byte[] m_StateData;

        [SerializeField] private bool m_ShowRawBytes;
        [SerializeField] private TreeViewState m_ControlTreeState;
        [SerializeField] private MultiColumnHeaderState m_ControlTreeHeaderState;
        [SerializeField] private Vector2 m_ControlTreeScrollPosition;
        [SerializeField] private Vector2 m_HexDumpScrollPosition;

        [NonSerialized] private InputControlTreeView m_ControlTree;

        ////FIXME: we lose this on domain reload; how should we recover?
        [NonSerialized] private InputControl m_Control;

        ////TODO: allow setting a C# struct type that we can use to display the layout of the data

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
        }
    }
}
#endif // UNITY_EDITOR
