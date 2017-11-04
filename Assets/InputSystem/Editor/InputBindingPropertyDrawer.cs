#if UNITY_EDITOR
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ISX.Editor
{
    // Instead of letting users fiddle around with strings in the inspector, this
    // presents an interface that allows to automatically construct the path
    // strings. The user can still enter a plain string manually in the popup
    // window we display.
    //
    // Normally just renders a visualization of the binding. However, if the mouse
    // is hovered over the binding, displays buttons to modify the binding.
    [CustomPropertyDrawer(typeof(InputBinding))]
    public class InputBindingPropertyDrawer : PropertyDrawer
    {
        private const int kPickButtonWidth = 50;
        private const int kModifyButtonWidth = 50;

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            ////FIXME: this does not work as expected...
            // Find out if we should display our modification buttons.
            var haveMouseOver = rect.Contains(Event.current.mousePosition);

            var pathProperty = property.FindPropertyRelative("path");
            var modifiersProperty = property.FindPropertyRelative("modifiers");
            var flagsProperty = property.FindPropertyRelative("flags");

            var path = pathProperty.stringValue;
            var modifiers = modifiersProperty.stringValue;
            var flags = (InputBinding.Flags)flagsProperty.intValue;

            var pathContent = GetContentForPath(path, modifiers, flags);

            //Debug.Log($"Rect: {rect} Mouse: {Event.current.mousePosition} Over: {haveMouseOver} Path: {pathContent.text}");

            var pathRect = rect;
            EditorGUI.LabelField(pathRect, pathContent);

            if (haveMouseOver)
            {
                // We draw the buttons *over* the path as hover UIs.
                var modifyButtonRect = new Rect(rect.x + rect.width - 4 - kModifyButtonWidth, rect.y, kModifyButtonWidth, rect.height);
                var pathButtonRect = new Rect(modifyButtonRect.x - 4 - kPickButtonWidth, rect.y, kPickButtonWidth, rect.height);

                if (EditorGUI.DropdownButton(pathButtonRect, Contents.pick, FocusType.Keyboard))
                {
                    PopupWindow.Show(pathButtonRect, new InputControlPicker(pathProperty));
                }

                if (EditorGUI.DropdownButton(modifyButtonRect, Contents.modify, FocusType.Keyboard))
                {
                    PopupWindow.Show(modifyButtonRect, new ModifyPopupWindow(property));
                }
            }

            EditorGUI.EndProperty();

            ////REVIEW: this shouldn't be necessary if we can get mousemove events
            ////REVIEW: is there a better solution than this?
            // While we the mouse is on us, repaint continuously to make our
            // hover effect work.
            if (haveMouseOver)
                EditorWindow.mouseOverWindow.Repaint();
        }

        private GUIContent GetContentForPath(string path, string modifiers, InputBinding.Flags flags)
        {
            if (s_UsageRegex == null)
                s_UsageRegex = new Regex("\\*/{([A-Za-z0-9]+)}");
            if (s_ControlRegex == null)
                s_ControlRegex = new Regex("<([A-Za-z0-9]+)>/([A-Za-z0-9]+(/[A-Za-z0-9]+)*)");

            var text = path;

            ////TODO: make this less GC heavy
            ////TODO: prettify control names (e.g. "rightTrigger" should read "Right Trigger"); have explicit display names?

            var usageMatch = s_UsageRegex.Match(path);
            if (usageMatch.Success)
            {
                text = usageMatch.Groups[1].Value;
            }
            else
            {
                var controlMatch = s_ControlRegex.Match(path);
                if (controlMatch.Success)
                {
                    var device = controlMatch.Groups[1].Value;
                    var control = controlMatch.Groups[2].Value;

                    ////TODO: would be nice to include template name to print something like "Gamepad A Button" instead of "Gamepad A" (or whatever)

                    text = $"{device} {control}";
                }
            }

            ////REVIEW: would be nice to have icons for these

            // Show modifiers.
            if (!string.IsNullOrEmpty(modifiers))
            {
                var modifierList = InputTemplate.ParseNameAndParameterList(modifiers);
                var modifierString = string.Join(" OR ", modifierList.Select(x => x.name));
                text = $"{modifierString} {text}";
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
            public static GUIContent combine = new GUIContent("Combines with previous binding");
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

            private SerializedProperty m_FlagsProperty;
            private SerializedProperty m_ModifiersProperty;
            private InputBinding.Flags m_Flags;
            private InputTemplate.NameAndParameters[] m_Modifiers;
            private Vector2 m_ScrollPosition;
            private GUIContent[] m_ModifierChoices;
            private int m_SelectedModifier;

            public ModifyPopupWindow(SerializedProperty bindingProperty)
            {
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
                m_ScrollPosition = GUI.BeginScrollView(rect, m_ScrollPosition, rect);

                var combineToggleRect = rect;
                combineToggleRect.x += kPaddingLeft;
                combineToggleRect.y += kPaddingTop;
                combineToggleRect.height = kCombineToggleHeight;
                combineToggleRect.width -= kPaddingLeft;

                ////TODO: disable toggle if property is first in list (bit tricky to find out from the SerializedProperty)

                // Combine-with-previous flag.
                var currentCombineSetting = (m_Flags & InputBinding.Flags.ThisAndPreviousCombine) ==
                    InputBinding.Flags.ThisAndPreviousCombine;
                var newCombineSetting = EditorGUI.ToggleLeft(combineToggleRect, Contents.combine, currentCombineSetting);
                if (currentCombineSetting != newCombineSetting)
                {
                    if (newCombineSetting)
                        m_Flags |= InputBinding.Flags.ThisAndPreviousCombine;
                    else
                        m_Flags &= ~InputBinding.Flags.ThisAndPreviousCombine;

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
