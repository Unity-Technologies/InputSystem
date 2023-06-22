#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
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
        private readonly VisualElement m_Root;
        private readonly Func<InputActionsEditorState, IEnumerable<ParameterListView>> m_ParameterListViewSelector;
        private VisualElement m_ContentContainer;

        private SerializedProperty m_ListProperty;

        public NameAndParametersListView(VisualElement root, StateContainer stateContainer, SerializedProperty listProperty,
                                         Func<InputActionsEditorState, IEnumerable<ParameterListView>> parameterListViewSelector)
            : base(stateContainer)
        {
            m_Root = root;
            m_ListProperty = listProperty;
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

            m_ListProperty.stringValue = ToSerializableString(interactionsOrProcessorsList);
            m_ListProperty.serializedObject.ApplyModifiedProperties();
        }

        private void MoveElement(int index, bool up)
        {
            var newIndex = index - 1;
            if (!up)
                newIndex = index + 1;
            SwapElement(index, newIndex);
        }

        private void SwapElement(int oldIndex, int newIndex)
        {
            var interactionsOrProcessorsList = NameAndParameters.ParseMultiple(m_ListProperty.stringValue).ToArray();
            newIndex = Math.Clamp(newIndex, 0, interactionsOrProcessorsList.Length - 1);
            MemoryHelpers.Swap(ref interactionsOrProcessorsList[oldIndex], ref interactionsOrProcessorsList[newIndex]);
            m_ListProperty.stringValue = ToSerializableString(interactionsOrProcessorsList);
            m_ListProperty.serializedObject.ApplyModifiedProperties();
        }

        private void DeleteElement(int index)
        {
            var interactionsOrProcessorsList = NameAndParameters.ParseMultiple(m_ListProperty.stringValue).ToList();
            interactionsOrProcessorsList.RemoveAt(index);
            m_ListProperty.stringValue = ToSerializableString(interactionsOrProcessorsList);
            m_ListProperty.serializedObject.ApplyModifiedProperties();
        }

        private void OnParametersChanged(ParameterListView listView, int index)
        {
            var interactionsOrProcessorsList = NameAndParameters.ParseMultiple(m_ListProperty.stringValue).ToList();
            interactionsOrProcessorsList[index] = new NameAndParameters { name = interactionsOrProcessorsList[index].name, parameters = listView.GetParameters() };
            m_ListProperty.stringValue = ToSerializableString(interactionsOrProcessorsList);
            m_ListProperty.serializedObject.ApplyModifiedProperties();
        }

        private static string ToSerializableString(IEnumerable<NameAndParameters> parametersForEachListItem)
        {
            if (parametersForEachListItem == null)
                return string.Empty;

            return string.Join(NamedValue.Separator,
                parametersForEachListItem.Select(x => x.ToString()).ToArray());
        }

        public override void RedrawUI(InputActionsEditorState state)
        {
            if (m_ContentContainer != null)
                m_Root.Remove(m_ContentContainer);

            m_ContentContainer = new VisualElement();
            m_Root.Add(m_ContentContainer);

            var parameterListViews = m_ParameterListViewSelector(state).ToList();
            if (parameterListViews.Count == 0)
            {
                m_Root.Q<Label>("no-parameters-added-label").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                return;
            }

            m_Root.Q<Label>("no-parameters-added-label").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            m_ContentContainer.Clear();
            for (int i = 0; i < parameterListViews.Count; i++)
            {
                var index = i;
                var buttonProperties = new ButtonProperties()
                {
                    onClickDown = () => MoveElement(index, false),
                    onClickUp = () => MoveElement(index, true),
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
                m_Root.Remove(m_ContentContainer);
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
            moveItemUpButton.AddToClassList(buttonProperties.isUpButtonActive ? "upActive" : "up");
            moveItemUpButton.AddToClassList("name-and-parameters-list-foldout-button");
            moveItemUpButton.clicked += buttonProperties.onClickUp;

            var moveItemDownButton = new Button();
            moveItemDownButton.AddToClassList(buttonProperties.isDownButtonActive ? "downActive" : "down");
            moveItemDownButton.AddToClassList("name-and-parameters-list-foldout-button");
            moveItemDownButton.clicked += buttonProperties.onClickDown;

            var deleteItemButton = new Button();
            deleteItemButton.AddToClassList("delete");
            deleteItemButton.AddToClassList("name-and-parameters-list-foldout-button");
            deleteItemButton.clicked += buttonProperties.onDelete;

            header.Add(moveItemUpButton);
            header.Add(moveItemDownButton);
            header.Add(deleteItemButton);

            var foldout = container.Q<Foldout>("Foldout");
            foldout.text = parameterListView.name;
            parameterListView.OnDrawVisualElements(foldout);
        }
    }
}

#endif
