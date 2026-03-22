#if UNITY_EDITOR
using System.IO;
using UnityEngine.InputSystem.Utilities;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

////TODO: support for multi-editing

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Custom editor that allows modifying importer settings for an <see cref="InputActionImporter"/>.
    /// </summary>
    [CustomEditor(typeof(InputActionImporter))]
    internal class InputActionImporterEditor : ScriptedImporterEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var inputActionAsset = GetAsset();

            if (inputActionAsset == null)
            {
                root.Add(new HelpBox(
                    "The currently selected object is not an editable input action asset.",
                    HelpBoxMessageType.Info));
            }

            var editButton = new Button(() => OpenEditor(inputActionAsset))
            {
                text = GetOpenEditorButtonText(inputActionAsset)
            };
            editButton.style.height = 30;
            editButton.style.marginTop = 4;
            editButton.style.marginBottom = 4;
            editButton.SetEnabled(inputActionAsset != null);
            root.Add(editButton);

            var projectWideContainer = new VisualElement();
            projectWideContainer.style.marginTop = 6;
            projectWideContainer.style.marginBottom = 6;
            root.Add(projectWideContainer);
            BuildProjectWideSection(projectWideContainer, inputActionAsset);

            BuildCodeGenerationSection(root, inputActionAsset);

            root.Add(new IMGUIContainer(() =>
            {
                serializedObject.ApplyModifiedProperties();
                ApplyRevertGUI();
            }));

            return root;
        }

        private void BuildProjectWideSection(VisualElement container, InputActionAsset inputActionAsset)
        {
            container.Clear();

            var currentActions = InputSystem.actions;

            if (currentActions == inputActionAsset)
            {
                container.Add(new HelpBox(
                    "These actions are assigned as the Project-wide Input Actions.",
                    HelpBoxMessageType.Info));
                return;
            }

            var message = "These actions are not assigned as the Project-wide Input Actions for the Input System.";
            if (currentActions != null)
            {
                var currentPath = AssetDatabase.GetAssetPath(currentActions);
                if (!string.IsNullOrEmpty(currentPath))
                    message += $" The actions currently assigned as the Project-wide Input Actions are: {currentPath}. ";
            }

            container.Add(new HelpBox(message, HelpBoxMessageType.Warning));

            var assignButton = new Button(() =>
            {
                InputSystem.actions = inputActionAsset;
                BuildProjectWideSection(container, inputActionAsset);
            })
            {
                text = "Assign as the Project-wide Input Actions"
            };
            assignButton.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
            container.Add(assignButton);
        }

        private void BuildCodeGenerationSection(VisualElement root, InputActionAsset inputActionAsset)
        {
            var generateField = new PropertyField(
                serializedObject.FindProperty("m_GenerateWrapperCode"), "Generate C# Class");
            root.Add(generateField);

            var codeGenContainer = new VisualElement();
            root.Add(codeGenContainer);

            // File path with browse button
            string defaultFileName = "";
            if (inputActionAsset != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(inputActionAsset);
                defaultFileName = Path.ChangeExtension(assetPath, ".cs");
            }

            var pathRow = new VisualElement();
            pathRow.style.flexDirection = FlexDirection.Row;
            pathRow.style.alignItems = Align.Center;
            codeGenContainer.Add(pathRow);

            var pathField = new TextField("C# Class File") { bindingPath = "m_WrapperCodePath" };
            pathField.style.flexGrow = 1;
            SetupPlaceholder(pathField, defaultFileName);
            pathRow.Add(pathField);

            var browseButton = new Button(() =>
            {
                var fileName = EditorUtility.SaveFilePanel("Location for generated C# file",
                    Path.GetDirectoryName(defaultFileName),
                    Path.GetFileName(defaultFileName), "cs");
                if (!string.IsNullOrEmpty(fileName))
                {
                    if (fileName.StartsWith(Application.dataPath))
                        fileName = "Assets/" + fileName.Substring(Application.dataPath.Length + 1);

                    var prop = serializedObject.FindProperty("m_WrapperCodePath");
                    prop.stringValue = fileName;
                    serializedObject.ApplyModifiedProperties();
                }
            })
            {
                text = "…"
            };
            browseButton.style.width = 25;
            browseButton.style.minWidth = 25;
            pathRow.Add(browseButton);

            // Class name
            string typeName = inputActionAsset != null
                ? CSharpCodeHelpers.MakeTypeName(inputActionAsset.name)
                : null;

            var classNameField = new TextField("C# Class Name") { bindingPath = "m_WrapperClassName" };
            SetupPlaceholder(classNameField, typeName ?? "<Class name>");
            codeGenContainer.Add(classNameField);

            var classNameError = new HelpBox("Must be a valid C# identifier", HelpBoxMessageType.Error);
            codeGenContainer.Add(classNameError);

            var classNameProp = serializedObject.FindProperty("m_WrapperClassName");
            classNameError.style.display = !CSharpCodeHelpers.IsEmptyOrProperIdentifier(classNameProp.stringValue)
                ? DisplayStyle.Flex : DisplayStyle.None;

            classNameField.RegisterValueChangedCallback(evt =>
            {
                classNameError.style.display = !CSharpCodeHelpers.IsEmptyOrProperIdentifier(evt.newValue)
                    ? DisplayStyle.Flex : DisplayStyle.None;
            });

            // Namespace
            var namespaceField = new TextField("C# Class Namespace") { bindingPath = "m_WrapperCodeNamespace" };
            SetupPlaceholder(namespaceField, "<Global namespace>");
            codeGenContainer.Add(namespaceField);

            var namespaceError = new HelpBox("Must be a valid C# namespace name", HelpBoxMessageType.Error);
            codeGenContainer.Add(namespaceError);

            var namespaceProp = serializedObject.FindProperty("m_WrapperCodeNamespace");
            namespaceError.style.display = !CSharpCodeHelpers.IsEmptyOrProperNamespaceName(namespaceProp.stringValue)
                ? DisplayStyle.Flex : DisplayStyle.None;

            namespaceField.RegisterValueChangedCallback(evt =>
            {
                namespaceError.style.display = !CSharpCodeHelpers.IsEmptyOrProperNamespaceName(evt.newValue)
                    ? DisplayStyle.Flex : DisplayStyle.None;
            });

            // Show/hide code gen fields based on toggle
            var generateProp = serializedObject.FindProperty("m_GenerateWrapperCode");
            codeGenContainer.style.display = generateProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;

            generateField.RegisterValueChangeCallback(evt =>
            {
                codeGenContainer.style.display = evt.changedProperty.boolValue
                    ? DisplayStyle.Flex : DisplayStyle.None;
            });
        }

        private static void SetupPlaceholder(TextField textField, string placeholder)
        {
            if (string.IsNullOrEmpty(placeholder))
                return;

            var placeholderLabel = new Label(placeholder);
            placeholderLabel.pickingMode = PickingMode.Ignore;
            placeholderLabel.style.position = Position.Absolute;
            placeholderLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            placeholderLabel.style.opacity = 0.5f;
            placeholderLabel.style.paddingLeft = 2;

            textField.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                var textInput = textField.Q("unity-text-input");
                if (textInput != null && placeholderLabel.parent != textInput)
                {
                    textInput.Add(placeholderLabel);
                    UpdatePlaceholder(textField, placeholderLabel);
                }
            });

            textField.RegisterValueChangedCallback(_ => UpdatePlaceholder(textField, placeholderLabel));
            textField.RegisterCallback<FocusInEvent>(_ => placeholderLabel.style.display = DisplayStyle.None);
            textField.RegisterCallback<FocusOutEvent>(_ => UpdatePlaceholder(textField, placeholderLabel));
        }

        private static void UpdatePlaceholder(TextField textField, Label placeholder)
        {
            placeholder.style.display = string.IsNullOrEmpty(textField.value)
                ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private InputActionAsset GetAsset()
        {
            return assetTarget as InputActionAsset;
        }

        protected override bool ShouldHideOpenButton()
        {
            return IsProjectWideActionsAsset();
        }

        private bool IsProjectWideActionsAsset()
        {
            return IsProjectWideActionsAsset(GetAsset());
        }

        private static bool IsProjectWideActionsAsset(InputActionAsset asset)
        {
            return !ReferenceEquals(asset, null) && InputSystem.actions == asset;
        }

        private string GetOpenEditorButtonText(InputActionAsset asset)
        {
            if (IsProjectWideActionsAsset(asset))
                return "Edit in Project Settings Window";

            return "Edit Asset";
        }

        private static void OpenEditor(InputActionAsset asset)
        {
            if (IsProjectWideActionsAsset(asset))
            {
                SettingsService.OpenProjectSettings(InputSettingsPath.kSettingsRootPath);
                return;
            }

            InputActionsEditorWindow.OpenEditor(asset);
        }
    }
}
#endif // UNITY_EDITOR
