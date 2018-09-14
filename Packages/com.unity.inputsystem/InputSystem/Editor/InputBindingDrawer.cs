#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Experimental.Input.Utilities;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Experimental.Input.Layouts;

////TODO: reordering support for interactions

namespace UnityEngine.Experimental.Input.Editor
{
    // Instead of letting users fiddle around with strings in the inspector, this
    // presents an interface that allows to automatically construct the path
    // strings. The user can still enter a plain string manually in the popup
    // window we display.
    //
    // Normally just renders a visualization of the binding. However, if the mouse
    // is hovered over the binding, displays buttons to modify the binding.
    [CustomPropertyDrawer(typeof(InputBinding))]
    public class InputBindingDrawer : PropertyDrawer
    {
        private const int kPickButtonWidth = 50;
        private const int kModifyButtonWidth = 50;

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var pathProperty = property.FindPropertyRelative("m_Path");
            var interactionsProperty = property.FindPropertyRelative("m_Interactions");
            var flagsProperty = property.FindPropertyRelative("m_Flags");

            var path = pathProperty.stringValue;
            var interactions = interactionsProperty.stringValue;
            var flags = (InputBinding.Flags)flagsProperty.intValue;

            var pathContent = GetContentForPath(path, interactions, flags);

            var modifyButtonRect = new Rect(rect.x + rect.width - 4 - kModifyButtonWidth, rect.y, kModifyButtonWidth, rect.height);
            var pickButtonRect = new Rect(modifyButtonRect.x - 4 - kPickButtonWidth, rect.y, kPickButtonWidth, rect.height);
            var pathRect = new Rect(rect.x, rect.y, pickButtonRect.x - rect.x, rect.height);

            ////TODO: center the buttons properly vertically
            modifyButtonRect.y += 2;
            pickButtonRect.y += 2;

            EditorGUI.LabelField(pathRect, pathContent);

            // Pick button.
            if (EditorGUI.DropdownButton(pickButtonRect, Contents.pick, FocusType.Keyboard))
            {
                PopupWindow.Show(pickButtonRect,
                    new InputControlPickerPopup(pathProperty));
            }

            // Modify button.
            if (EditorGUI.DropdownButton(modifyButtonRect, Contents.modify, FocusType.Keyboard))
            {
                PopupWindow.Show(modifyButtonRect,
                    new ModifyPopupWindow(property));
            }

            EditorGUI.EndProperty();
        }

        ////TODO: move this out into a general routine that can take a path and construct a display name
        private static GUIContent GetContentForPath(string path, string interactions, InputBinding.Flags flags)
        {
            const int kUsageNameGroup = 1;
            const int kDeviceNameGroup = 1;
            const int kDeviceUsageGroup = 3;
            const int kControlPathGroup = 4;

            ////TODO: nuke the regex stuff in here

            if (s_UsageRegex == null)
                s_UsageRegex = new Regex("\\*/{([A-Za-z0-9]+)}");
            if (s_ControlRegex == null)
                s_ControlRegex = new Regex("<([A-Za-z0-9:\\-]+)>({([A-Za-z0-9]+)})?/([A-Za-z0-9]+(/[A-Za-z0-9]+)*)");

            var text = path;

            var usageMatch = s_UsageRegex.Match(path);
            if (usageMatch.Success)
            {
                text = usageMatch.Groups[kUsageNameGroup].Value;
            }
            else
            {
                var controlMatch = s_ControlRegex.Match(path);
                if (controlMatch.Success)
                {
                    var device = controlMatch.Groups[kDeviceNameGroup].Value;
                    var deviceUsage = controlMatch.Groups[kDeviceUsageGroup].Value;
                    var control = controlMatch.Groups[kControlPathGroup].Value;

                    ////TODO: would be nice to include layout name to print something like "Gamepad A Button" instead of "Gamepad A" (or whatever)

                    if (!string.IsNullOrEmpty(deviceUsage))
                        text = string.Format("{0} {1} {2}", deviceUsage, device, control);
                    else
                        text = string.Format("{0} {1}", device, control);
                }
            }

            ////REVIEW: would be nice to have icons for these

            // Show interactions.
            if (!string.IsNullOrEmpty(interactions))
            {
                var interactionList = InputControlLayout.ParseNameAndParameterList(interactions);
                var interactionString = string.Join(" OR ", interactionList.Select(x => x.name).ToArray());
                text = string.Format("{0} {1}", interactionString, text);
            }

            ////TODO: this looks ugly and not very obvious; find a better way
            // Show if linked with previous binding.
            if ((flags & InputBinding.Flags.ThisAndPreviousCombine) == InputBinding.Flags.ThisAndPreviousCombine)
            {
                text = "AND " + text;
            }

            return new GUIContent(text);
        }

        private static Regex s_UsageRegex;
        private static Regex s_ControlRegex;

        private static class Contents
        {
            public static GUIContent pick = new GUIContent("Pick");
            public static GUIContent modify = new GUIContent("Modify");
            public static GUIContent chain = new GUIContent("Chain with previous binding");
            public static GUIContent interactions = new GUIContent("Interactions");
        }

        // This will most likely go away but for now it provides a way to customize an InputBinding
        // beyond its path. Provides access to flags and interactions.
        private class ModifyPopupWindow : PopupWindowContent
        {
            private const int kPaddingTop = 10;
            private const int kPaddingLeftRight = 5;
            private const int kCombineToggleHeight = 20;

            private SerializedProperty m_FlagsProperty;
            private SerializedProperty m_InteractionsProperty;
            private InputBinding.Flags m_Flags;
            private InputControlLayout.NameAndParameters[] m_Interactions;
            private Vector2 m_ScrollPosition;
            private GUIContent[] m_InteractionChoices;
            private int m_SelectedInteraction;
            private ReorderableList m_InteractionListView;

            public Action<SerializedProperty> onApplyCallback;

            public ModifyPopupWindow(SerializedProperty bindingProperty)
            {
                m_FlagsProperty = bindingProperty.FindPropertyRelative("m_Flags");
                m_InteractionsProperty = bindingProperty.FindPropertyRelative("m_Interactions");
                m_Flags = (InputBinding.Flags)m_FlagsProperty.intValue;

                var interactions = InputSystem.ListInteractions().ToList();
                interactions.Sort();
                m_InteractionChoices = interactions.Select(x => new GUIContent(x)).ToArray();

                var interactionString = m_InteractionsProperty.stringValue;
                if (!string.IsNullOrEmpty(interactionString))
                    m_Interactions = InputControlLayout.ParseNameAndParameterList(interactionString);
                else
                    m_Interactions = new InputControlLayout.NameAndParameters[0];

                InitializeInteractionListView();
            }

            ////TODO: close with escape

            public override void OnGUI(Rect rect)
            {
                m_ScrollPosition = GUI.BeginScrollView(rect, m_ScrollPosition, rect);

                // Interactions section.
                var interactionListRect = rect;
                interactionListRect.x += kPaddingLeftRight;
                interactionListRect.y += kPaddingTop;
                interactionListRect.width -= kPaddingLeftRight * 2;
                interactionListRect.height = m_InteractionListView.GetHeight();
                m_InteractionListView.DoList(interactionListRect);

                ////TODO: draw box around following section

                // Chaining toggle.
                var chainingToggleRect = interactionListRect;
                chainingToggleRect.y += interactionListRect.height + 5;
                chainingToggleRect.height = kCombineToggleHeight;

                ////TODO: disable toggle if property is first in list (bit tricky to find out from the SerializedProperty)

                var currentCombineSetting = (m_Flags & InputBinding.Flags.ThisAndPreviousCombine) ==
                    InputBinding.Flags.ThisAndPreviousCombine;
                var newCombineSetting = EditorGUI.ToggleLeft(chainingToggleRect, Contents.chain, currentCombineSetting);
                if (currentCombineSetting != newCombineSetting)
                {
                    if (newCombineSetting)
                        m_Flags |= InputBinding.Flags.ThisAndPreviousCombine;
                    else
                        m_Flags &= ~InputBinding.Flags.ThisAndPreviousCombine;

                    m_FlagsProperty.intValue = (int)m_Flags;
                    m_FlagsProperty.serializedObject.ApplyModifiedProperties();

                    if (onApplyCallback != null)
                        onApplyCallback(m_FlagsProperty);
                }

                GUI.EndScrollView();
            }

            private void AddInteraction(object interactionNameString)
            {
                ArrayHelpers.Append(ref m_Interactions,
                    new InputControlLayout.NameAndParameters {name = (string)interactionNameString});
                m_InteractionListView.list = m_Interactions;
                ApplyInteractions();
            }

            private void ApplyInteractions()
            {
                var interactions = string.Join(",", m_Interactions.Select(x => x.ToString()).ToArray());
                m_InteractionsProperty.stringValue = interactions;
                m_InteractionsProperty.serializedObject.ApplyModifiedProperties();
                InitializeInteractionListView();

                if (onApplyCallback != null)
                    onApplyCallback(m_InteractionsProperty);
            }

            private void InitializeInteractionListView()
            {
                m_InteractionListView = new ReorderableList(m_Interactions, typeof(InputControlLayout.NameAndParameters));

                m_InteractionListView.drawHeaderCallback =
                    rect => EditorGUI.LabelField(rect, Contents.interactions);

                m_InteractionListView.drawElementCallback =
                    (rect, index, isActive, isFocused) =>
                {
                    ////TODO: parameters
                    EditorGUI.LabelField(rect, m_Interactions[index].name);
                };

                m_InteractionListView.onAddDropdownCallback =
                    (rect, list) =>
                {
                    var menu = new GenericMenu();
                    for (var i = 0; i < m_InteractionChoices.Length; ++i)
                        menu.AddItem(m_InteractionChoices[i], false, AddInteraction, m_InteractionChoices[i].text);
                    menu.ShowAsContext();
                };

                m_InteractionListView.onRemoveCallback =
                    (list) =>
                {
                    var indexToRemove = list.index;
                    if (indexToRemove == m_Interactions.Length - 1)
                        --list.index;
                    ArrayHelpers.EraseAt(ref m_Interactions, indexToRemove);
                    if (m_Interactions == null)
                        m_Interactions = new InputControlLayout.NameAndParameters[0];
                    list.list = m_Interactions;
                    ApplyInteractions();
                };
            }
        }
    }
}
#endif // UNITY_EDITOR
