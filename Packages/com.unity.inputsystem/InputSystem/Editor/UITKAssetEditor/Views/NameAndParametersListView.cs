#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Editor.Lists;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class NameAndParametersListView : ViewBase<InputActionsEditorState>
    {
        private readonly Func<InputActionsEditorState, IEnumerable<ParameterListView>> m_ParameterListViewSelector;
        private VisualElement m_ContentContainer;
        private readonly Label m_NoParamsLabel;

        private SerializedProperty m_ListProperty;

        public NameAndParametersListView(VisualElement root, StateContainer stateContainer, SerializedProperty listProperty,
                                         Func<InputActionsEditorState, IEnumerable<ParameterListView>> parameterListViewSelector)
            : base(root, stateContainer)
        {
            m_ListProperty = listProperty;
            m_NoParamsLabel = root.Q<Label>("no-parameters-added-label");
            m_ParameterListViewSelector = parameterListViewSelector;

            CreateSelector(state => state);
        }

        public void OnAddElement(string name)
        {
            if (m_ListProperty == null)
                return;

            var interactionsOrProcessorsList = NameAndParameters.ParseMultiple(m_ListProperty.stringValue).ToList();
            var newElement = new NameAndParameters() { name = name};
            interactionsOrProcessorsList.Add(newElement);

            m_ListProperty.stringValue = NameAndParameters.ToSerializableString(interactionsOrProcessorsList);
            m_ListProperty.serializedObject.ApplyModifiedProperties();
        }

        private void MoveElementUp(int index)
        {
            var newIndex = index - 1;
            SwapElement(index, newIndex);
        }

        private void MoveElementDown(int index)
        {
            var newIndex = index + 1;
            SwapElement(index, newIndex);
        }

        private void SwapElement(int oldIndex, int newIndex)
        {
            var interactionsOrProcessors = NameAndParameters.ParseMultiple(m_ListProperty.stringValue).ToArray();
            var oldIndexIsValid = oldIndex >= 0 && oldIndex < interactionsOrProcessors.Length;
            var newIndexIsValid = newIndex >= 0 && newIndex < interactionsOrProcessors.Length;
            if (interactionsOrProcessors.Length == 0 || !newIndexIsValid || !oldIndexIsValid)
                return;
            MemoryHelpers.Swap(ref interactionsOrProcessors[oldIndex], ref interactionsOrProcessors[newIndex]);
            m_ListProperty.stringValue = NameAndParameters.ToSerializableString(interactionsOrProcessors);
            m_ListProperty.serializedObject.ApplyModifiedProperties();
        }

        private void DeleteElement(int index)
        {
            var interactionsOrProcessorsList = NameAndParameters.ParseMultiple(m_ListProperty.stringValue).ToList();
            interactionsOrProcessorsList.RemoveAt(index);
            m_ListProperty.stringValue = NameAndParameters.ToSerializableString(interactionsOrProcessorsList);
            m_ListProperty.serializedObject.ApplyModifiedProperties();
        }

        private void OnParametersChanged(ParameterListView listView, int index)
        {
            var interactionsOrProcessorsList = NameAndParameters.ParseMultiple(m_ListProperty.stringValue).ToList();
            interactionsOrProcessorsList[index] = new NameAndParameters { name = interactionsOrProcessorsList[index].name, parameters = listView.GetParameters() };
            m_ListProperty.stringValue = NameAndParameters.ToSerializableString(interactionsOrProcessorsList);
            m_ListProperty.serializedObject.ApplyModifiedProperties();
        }

        public override void RedrawUI(InputActionsEditorState state)
        {
            if (m_ContentContainer != null)
                rootElement.Remove(m_ContentContainer);

            m_ContentContainer = new VisualElement();
            rootElement.Add(m_ContentContainer);

            var parameterListViews = m_ParameterListViewSelector(state).ToList();
            if (parameterListViews.Count == 0)
            {
                m_NoParamsLabel.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                return;
            }

            m_NoParamsLabel.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            m_ContentContainer.Clear();
            for (int i = 0; i < parameterListViews.Count; i++)
            {
                var index = i;
                var buttonProperties = new ButtonProperties()
                {
                    onClickDown = () => MoveElementDown(index),
                    onClickUp = () => MoveElementUp(index),
                    onDelete = () => DeleteElement(index),
                    isDownButtonActive = index < parameterListViews.Count - 1,
                    isUpButtonActive = index > 0
                };
                new NameAndParametersListViewItem(m_ContentContainer, parameterListViews[i], buttonProperties);
                parameterListViews[i].onChange += () => OnParametersChanged(parameterListViews[index], index);
            }
        }

        public override void DestroyView()
        {
            if (m_ContentContainer != null)
            {
                rootElement.Remove(m_ContentContainer);
                m_ContentContainer = null;
            }
        }
    }

    internal struct ButtonProperties
    {
        public Action onClickUp;
        public Action onClickDown;
        public Action onDelete;
        public bool isUpButtonActive;
        public bool isDownButtonActive;
    }

    internal class NameAndParametersListViewItem
    {
        public NameAndParametersListViewItem(VisualElement root, ParameterListView parameterListView, ButtonProperties buttonProperties)
        {
            var itemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                InputActionsEditorConstants.PackagePath +
                InputActionsEditorConstants.ResourcesPath +
                InputActionsEditorConstants.NameAndParametersListViewItemUxml);

            var container = itemTemplate.CloneTree();
            root.Add(container);

            var header = container.Q<Toggle>();

            var moveItemUpButton = new Button();
            moveItemUpButton.AddToClassList(EditorGUIUtility.isProSkin ? "upDarkTheme" : "up");
            moveItemUpButton.AddToClassList("name-and-parameters-list-foldout-button");
            moveItemUpButton.SetEnabled(buttonProperties.isUpButtonActive);
            moveItemUpButton.clicked += buttonProperties.onClickUp;

            var moveItemDownButton = new Button();
            moveItemDownButton.AddToClassList(EditorGUIUtility.isProSkin ? "downDarkTheme" : "down");
            moveItemDownButton.AddToClassList("name-and-parameters-list-foldout-button");
            moveItemDownButton.SetEnabled(buttonProperties.isDownButtonActive);
            moveItemDownButton.clicked += buttonProperties.onClickDown;

            var deleteItemButton = new Button();
            deleteItemButton.AddToClassList(EditorGUIUtility.isProSkin ? "deleteDarkTheme" : "delete");
            deleteItemButton.AddToClassList("name-and-parameters-list-foldout-button");
            deleteItemButton.clicked += buttonProperties.onDelete;

            header.Add(moveItemUpButton);
            header.Add(moveItemDownButton);
            header.Add(deleteItemButton);

            var foldout = container.Q<Foldout>("Foldout");
            foldout.text = parameterListView.name;
            parameterListView.OnDrawVisualElements(foldout);

            foldout.Add(new IMGUIContainer(parameterListView.OnGUI));
        }
    }
}

#endif
