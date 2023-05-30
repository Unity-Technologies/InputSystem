#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Editor.Lists;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class NameAndParametersListView : ViewBase<InputActionsEditorState>
    {
        private readonly VisualElement m_Root;
        private readonly Func<InputActionsEditorState, IEnumerable<ParameterListView>> m_ParameterListViewSelector;
        private VisualElement m_ContentContainer;

        public NameAndParametersListView(VisualElement root, StateContainer stateContainer,
                                         Func<InputActionsEditorState, IEnumerable<ParameterListView>> parameterListViewSelector)
            : base(stateContainer)
        {
            m_Root = root;
            m_ParameterListViewSelector = parameterListViewSelector;

            CreateSelector(state => state);
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
            foreach (var parameterListView in parameterListViews)
            {
                new NameAndParametersListViewItem(m_ContentContainer, parameterListView);
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

    internal class NameAndParametersListViewItem
    {
        public NameAndParametersListViewItem(VisualElement root, ParameterListView parameterListView)
        {
            var itemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                InputActionsEditorConstants.PackagePath +
                InputActionsEditorConstants.ResourcesPath +
                InputActionsEditorConstants.NameAndParametersListViewItemUxml);

            var container = itemTemplate.CloneTree();
            root.Add(container);

            var header = container.Q<Toggle>();

            var moveItemUpButton = new Button();
            moveItemUpButton.AddToClassList("up");
            moveItemUpButton.AddToClassList("name-and-parameters-list-foldout-button");

            var moveItemDownButton = new Button();
            moveItemDownButton.AddToClassList("down");
            moveItemDownButton.AddToClassList("name-and-parameters-list-foldout-button");

            var deleteItemButton = new Button();
            deleteItemButton.AddToClassList("delete");
            deleteItemButton.AddToClassList("name-and-parameters-list-foldout-button");

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
