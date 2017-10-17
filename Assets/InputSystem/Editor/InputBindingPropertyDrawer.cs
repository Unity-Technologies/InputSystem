#if UNITY_EDITOR
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

//make the selector for controls a popup window with a search function

namespace ISX
{
    // Instead of letting users fiddle around with strings in the inspector, this
    // presents an interface that allows to automatically construct the path
    // strings. The user can still enter a plain string manually in the popup
    // window we display.
    [CustomPropertyDrawer(typeof(InputBinding))]
    public class InputBindingPropertyDrawer : PropertyDrawer
    {
        private const int kPathLabelWidth = 200;
        private const int kPickButtonWidth = 50;
        private const int kModifyButtonWidth = 50;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var pathProperty = property.FindPropertyRelative("path");
            var modifiersProperty = property.FindPropertyRelative("modifiers");
            var flagsProperty = property.FindPropertyRelative("flags");

            var path = pathProperty.stringValue;
            var modifiers = modifiersProperty.stringValue;
            var flags = flagsProperty.intValue;

            var pathContent = GetContentForPath(path, modifiers, flags);

            var pathRect = new Rect(position.x, position.y, kPathLabelWidth, position.height);
            var pathButtonRect = new Rect(position.x + kPathLabelWidth + 4, position.y, kPickButtonWidth, position.height);
            var modifyButtonRect = new Rect(position.x + kPathLabelWidth + 4 + kPickButtonWidth + 4, position.y,
                    kModifyButtonWidth, position.height);

            EditorGUI.LabelField(pathRect, pathContent);
            if (EditorGUI.DropdownButton(pathButtonRect, Contents.pick, FocusType.Keyboard))
            {
                PopupWindow.Show(pathButtonRect, new InputBindingPathSelector(pathProperty));
            }

            ////TODO: I think this UI is crap but it'll do for now
            if (EditorGUI.DropdownButton(modifyButtonRect, Contents.modify, FocusType.Keyboard))
            {
                PopupWindow.Show(modifyButtonRect, new ModifyPopupWindow(property));
            }

            EditorGUI.EndProperty();
        }

        private GUIContent GetContentForPath(string path, string modifiers, int flags)
        {
            if (s_UsageRegex == null)
                s_UsageRegex = new Regex("\\*/{([A-Za-z0-9]+)}");
            if (s_ControlRegex == null)
                s_ControlRegex = new Regex("<([A-Za-z0-9]+)>/([A-Za-z0-9]+(/[A-Za-z0-9]+)*)");

            ////TODO: also show modifiers on string (e.g. "Hold Gamepad RightTrigger") (would be even nicer to have icons for them)
            ////TODO: for linked binding, add something like "  & Gamepad ButtonSouth" or "  + Gamepad ButtonSouth"

            var text = path;

            var usageMatch = s_UsageRegex.Match(path);
            if (usageMatch.Success)
            {
                text = usageMatch.Groups[1].Value;
            }

            var controlMatch = s_ControlRegex.Match(path);
            if (controlMatch.Success)
            {
                var device = controlMatch.Groups[1].Value;
                var control = controlMatch.Groups[2].Value;

                ////TODO: would be nice to print something like "Gamepad: A Button" instead of "Gamepad: A" (or whatever)

                text = $"{device} {control}";
            }

            if (!string.IsNullOrEmpty(modifiers))
            {
                var modifierList = InputTemplate.ParseNameAndParameterList(modifiers);
                var modifierString = string.Join(" or ", modifierList.Select(x => x.name));
                text = $"{modifierString} {text}";
            }

            return new GUIContent(text);
        }

        private static Regex s_UsageRegex;
        private static Regex s_ControlRegex;

        private static class Contents
        {
            public static GUIContent pick = new GUIContent("Pick");
            public static GUIContent modify = new GUIContent("Modify");
            public static GUIContent combine = new GUIContent("Combines with next binding");
            public static GUIContent modifiers = new GUIContent("Modifiers:");
            public static GUIContent addModifier = new GUIContent("Add:");
            public static GUIContent iconPlus = EditorGUIUtility.IconContent("Toolbar Plus", "Add new binding");
            public static GUIContent iconMinus = EditorGUIUtility.IconContent("Toolbar Minus", "Remove binding");
        }

        // This will most likely go away but for now it provides a way to customize an InputBinding
        // beyond its path. Provides access to flags and modifiers.
        private class ModifyPopupWindow : PopupWindowContent
        {
            private const int kPaddingTop = 10;
            private const int kPaddingLeft = 5;
            private const int kCombineToggleHeight = 20;
            private const int kAddModifierButtonWidth = 80;
            private const int kAddModifierButtonHeight = 20;
            private const int kModifiersLabelHeight = 20;
            private const int kModifierLineHeight = 20;

            private SerializedProperty m_BindingProperty;
            private SerializedProperty m_FlagsProperty;
            private SerializedProperty m_ModifiersProperty;
            private InputBinding.Flags m_Flags;
            private InputTemplate.NameAndParameters[] m_Modifiers;
            private Vector2 m_ScrollPosition;
            private GUIContent[] m_ModifierChoices;
            private int m_SelectedModifier;

            public ModifyPopupWindow(SerializedProperty bindingProperty)
            {
                m_BindingProperty = bindingProperty;
                m_FlagsProperty = bindingProperty.FindPropertyRelative("flags");
                m_ModifiersProperty = bindingProperty.FindPropertyRelative("modifiers");
                m_Flags = (InputBinding.Flags)m_FlagsProperty.intValue;
                m_ModifierChoices = InputSystem.ListModifiers().Select(x => new GUIContent(x)).ToArray();

                var modifierString = m_ModifiersProperty.stringValue;
                if (!string.IsNullOrEmpty(modifierString))
                    m_Modifiers = InputTemplate.ParseNameAndParameterList(modifierString);
            }

            public override void OnGUI(Rect rect)
            {
                GUI.BeginScrollView(rect, m_ScrollPosition, rect);

                var combineToggleRect = rect;
                combineToggleRect.x += kPaddingLeft;
                combineToggleRect.y += kPaddingTop;
                combineToggleRect.height = kCombineToggleHeight;
                combineToggleRect.width -= kPaddingLeft;

                // Combine-with-next flag.
                var currentCombineSetting = (m_Flags & InputBinding.Flags.ThisAndNextCombine) ==
                    InputBinding.Flags.ThisAndNextCombine;
                var newCombineSetting = EditorGUI.ToggleLeft(combineToggleRect, Contents.combine, currentCombineSetting);
                if (currentCombineSetting != newCombineSetting)
                {
                    if (newCombineSetting)
                        m_Flags |= InputBinding.Flags.ThisAndNextCombine;
                    else
                        m_Flags &= ~InputBinding.Flags.ThisAndNextCombine;

                    m_FlagsProperty.intValue = (int)m_Flags;
                    m_FlagsProperty.serializedObject.ApplyModifiedProperties();
                }

                // Modifiers section.
                var modifiersLabelRect = combineToggleRect;
                modifiersLabelRect.y += kCombineToggleHeight;
                modifiersLabelRect.height = kModifiersLabelHeight;

                GUI.Label(modifiersLabelRect, Contents.modifiers, EditorStyles.boldLabel);

                var nextModifierRect = modifiersLabelRect;
                nextModifierRect.width = combineToggleRect.width;
                nextModifierRect.height = kModifierLineHeight;
                nextModifierRect.x += 10;
                nextModifierRect.y += kModifiersLabelHeight;

                if (m_Modifiers != null)
                {
                    for (var i = 0; i < m_Modifiers.Length; ++i)
                    {
                        var name = m_Modifiers[i].name;

                        ////TODO: parameters

                        var labelRect = nextModifierRect;
                        labelRect.width = kAddModifierButtonWidth;
                        GUI.Label(labelRect, name);

                        var minusButtonRect = labelRect;
                        minusButtonRect.x += minusButtonRect.width + 3;
                        minusButtonRect.width = Contents.iconMinus.image.width;
                        minusButtonRect.height = Contents.iconMinus.image.height;

                        if (GUI.Button(minusButtonRect, Contents.iconMinus, GUIStyle.none))
                        {
                            ArrayHelpers.Erase(ref m_Modifiers, i);
                            ApplyModifiers();
                            return;
                        }

                        nextModifierRect.y += kModifierLineHeight;
                    }
                }

                var addModifierRect = nextModifierRect;
                addModifierRect.height = kAddModifierButtonHeight;
                addModifierRect.width = kAddModifierButtonWidth;

                var plusModifierRect = addModifierRect;
                plusModifierRect.x += kAddModifierButtonWidth + 3;
                plusModifierRect.width = Contents.iconPlus.image.width;
                plusModifierRect.height = Contents.iconPlus.image.height;

                // UI to add new modifier to binding.
                m_SelectedModifier = EditorGUI.Popup(addModifierRect, m_SelectedModifier, m_ModifierChoices);
                if (GUI.Button(plusModifierRect, Contents.iconPlus, GUIStyle.none))
                {
                    ArrayHelpers.Append(ref m_Modifiers,
                        new InputTemplate.NameAndParameters {name = m_ModifierChoices[m_SelectedModifier].text});
                    ApplyModifiers();
                }

                GUI.EndScrollView();
            }

            private void ApplyModifiers()
            {
                var modifiers = string.Join(",", m_Modifiers.Select(x => x.ToString()));
                m_ModifiersProperty.stringValue = modifiers;
                m_ModifiersProperty.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif // UNITY_EDITOR
