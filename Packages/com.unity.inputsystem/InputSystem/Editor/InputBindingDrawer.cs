#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Experimental.Input.Utilities;
using UnityEditor;
using UnityEditorInternal;

////TODO: reordering support for modifiers

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

            var pathProperty = property.FindPropertyRelative("path");
            var modifiersProperty = property.FindPropertyRelative("modifiers");
            var flagsProperty = property.FindPropertyRelative("flags");

            var path = pathProperty.stringValue;
            var modifiers = modifiersProperty.stringValue;
            var flags = (InputBinding.Flags)flagsProperty.intValue;

            var pathContent = GetContentForPath(path, modifiers, flags);

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
                    new InputControlPicker(pathProperty) {onPickCallback = OnBindingModified});
            }

            // Modify button.
            if (EditorGUI.DropdownButton(modifyButtonRect, Contents.modify, FocusType.Keyboard))
            {
                PopupWindow.Show(modifyButtonRect,
                    new ModifyPopupWindow(property) {onApplyCallback = OnBindingModified});
            }

            EditorGUI.EndProperty();
        }

        private void OnBindingModified(SerializedProperty property)
        {
            var importerEditor = InputActionImporterEditor.FindFor(property.serializedObject);
            if (importerEditor != null)
                importerEditor.OnAssetModified();
        }

        ////TODO: move this out into a general routine that can take a path and construct a display name
        private GUIContent GetContentForPath(string path, string modifiers, InputBinding.Flags flags)
        {
            const int kUsageNameGroup = 1;
            const int kDeviceNameGroup = 1;
            const int kDeviceUsageGroup = 3;
            const int kControlPathGroup = 4;

            if (s_UsageRegex == null)
                s_UsageRegex = new Regex("\\*/{([A-Za-z0-9]+)}");
            if (s_ControlRegex == null)
                s_ControlRegex = new Regex("<([A-Za-z0-9:\\-]+)>({([A-Za-z0-9]+)})?/([A-Za-z0-9]+(/[A-Za-z0-9]+)*)");

            var text = path;

            ////TODO: make this less GC heavy
            ////TODO: prettify control names (e.g. "rightTrigger" should read "Right Trigger"); have explicit display names?

            ////REVIEW: This stuff here should really be based on general display functionality for controls
            ////        which should be available to game code in just the same way for on-screen display
            ////        purposes

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

            // Show modifiers.
            if (!string.IsNullOrEmpty(modifiers))
            {
                var modifierList = InputControlLayout.ParseNameAndParameterList(modifiers);
                var modifierString = string.Join(" OR ", modifierList.Select(x => x.name).ToArray());
                text = string.Format("{0} {1}", modifierString, text);
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
            public static GUIContent modifiers = new GUIContent("Modifiers");
        }

        // This will most likely go away but for now it provides a way to customize an InputBinding
        // beyond its path. Provides access to flags and modifiers.
        private class ModifyPopupWindow : PopupWindowContent
        {
            private const int kPaddingTop = 10;
            private const int kPaddingLeftRight = 5;
            private const int kCombineToggleHeight = 20;

            private SerializedProperty m_FlagsProperty;
            private SerializedProperty m_ModifiersProperty;
            private InputBinding.Flags m_Flags;
            private InputControlLayout.NameAndParameters[] m_Modifiers;
            private Vector2 m_ScrollPosition;
            private GUIContent[] m_ModifierChoices;
            private int m_SelectedModifier;
            private ReorderableList m_ModifierListView;

            public Action<SerializedProperty> onApplyCallback;

            public ModifyPopupWindow(SerializedProperty bindingProperty)
            {
                m_FlagsProperty = bindingProperty.FindPropertyRelative("flags");
                m_ModifiersProperty = bindingProperty.FindPropertyRelative("modifiers");
                m_Flags = (InputBinding.Flags)m_FlagsProperty.intValue;

                var modifiers = InputSystem.ListBindingModifiers().ToList();
                modifiers.Sort();
                m_ModifierChoices = modifiers.Select(x => new GUIContent(x)).ToArray();

                var modifierString = m_ModifiersProperty.stringValue;
                if (!string.IsNullOrEmpty(modifierString))
                    m_Modifiers = InputControlLayout.ParseNameAndParameterList(modifierString);
                else
                    m_Modifiers = new InputControlLayout.NameAndParameters[0];

                InitializeModifierListView();
            }

            ////TODO: close with escape

            public override void OnGUI(Rect rect)
            {
                m_ScrollPosition = GUI.BeginScrollView(rect, m_ScrollPosition, rect);

                // Modifiers section.
                var modifierListRect = rect;
                modifierListRect.x += kPaddingLeftRight;
                modifierListRect.y += kPaddingTop;
                modifierListRect.width -= kPaddingLeftRight * 2;
                modifierListRect.height = m_ModifierListView.GetHeight();
                m_ModifierListView.DoList(modifierListRect);

                ////TODO: draw box around following section

                // Chaining toggle.
                var chainingToggleRect = modifierListRect;
                chainingToggleRect.y += modifierListRect.height + 5;
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

            private void AddModifier(object modifierNameString)
            {
                ArrayHelpers.Append(ref m_Modifiers,
                    new InputControlLayout.NameAndParameters {name = (string)modifierNameString});
                m_ModifierListView.list = m_Modifiers;
                ApplyModifiers();
            }

            private void ApplyModifiers()
            {
                var modifiers = string.Join(",", m_Modifiers.Select(x => x.ToString()).ToArray());
                m_ModifiersProperty.stringValue = modifiers;
                m_ModifiersProperty.serializedObject.ApplyModifiedProperties();
                InitializeModifierListView();

                if (onApplyCallback != null)
                    onApplyCallback(m_ModifiersProperty);
            }

            private void InitializeModifierListView()
            {
                m_ModifierListView = new ReorderableList(m_Modifiers, typeof(InputControlLayout.NameAndParameters));

                m_ModifierListView.drawHeaderCallback =
                    (rect) => EditorGUI.LabelField(rect, Contents.modifiers);

                m_ModifierListView.drawElementCallback =
                    (rect, index, isActive, isFocused) =>
                    {
                        ////TODO: parameters
                        EditorGUI.LabelField(rect, m_Modifiers[index].name);
                    };

                m_ModifierListView.onAddDropdownCallback =
                    (rect, list) =>
                    {
                        var menu = new GenericMenu();
                        for (var i = 0; i < m_ModifierChoices.Length; ++i)
                            menu.AddItem(m_ModifierChoices[i], false, AddModifier, m_ModifierChoices[i].text);
                        menu.ShowAsContext();
                    };

                m_ModifierListView.onRemoveCallback =
                    (list) =>
                    {
                        var indexToRemove = list.index;
                        if (indexToRemove == m_Modifiers.Length - 1)
                            --list.index;
                        ArrayHelpers.EraseAt(ref m_Modifiers, indexToRemove);
                        if (m_Modifiers == null)
                            m_Modifiers = new InputControlLayout.NameAndParameters[0];
                        list.list = m_Modifiers;
                        ApplyModifiers();
                    };
            }
        }
    }
}
#endif // UNITY_EDITOR
