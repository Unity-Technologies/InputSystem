#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class ActionMapsTree : InputActionTreeBase
    {
        private SerializedObject m_SerializedObject;

        public static ActionMapsTree CreateFromSerializedObject(Action applyAction, SerializedObject serializedObject, ref TreeViewState treeViewState)
        {
            if (treeViewState == null)
            {
                treeViewState = new TreeViewState();
            }
            var treeView = new ActionMapsTree(applyAction, treeViewState);
            treeView.m_SerializedObject = serializedObject;
            treeView.Reload();
            treeView.ExpandAll();
            return treeView;
        }

        static bool OnFoldoutDraw(Rect position, bool expandedState, GUIStyle style)
        {
            var indent = (int)(position.x / 15);
            position.x = 6 * indent + 8;
            return EditorGUI.Foldout(position, expandedState, GUIContent.none, style);
        }

        protected ActionMapsTree(Action applyAction, TreeViewState state)
            : base(applyAction, state)
        {
            ////REVIEW: good enough like this for 2018.2?
            #if UNITY_2018_3_OR_NEWER
            foldoutOverride += OnFoldoutDraw;
            #endif
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem
            {
                id = 0,
                depth = -1
            };
            root.children = new List<TreeViewItem>();
            if (m_SerializedObject != null)
            {
                m_SerializedObject.Update();
                BuildFromSerializedObject(root);
            }
            return root;
        }

        private void BuildFromSerializedObject(TreeViewItem root)
        {
            var actionMapArrayProperty = m_SerializedObject.FindProperty("m_ActionMaps");
            for (var i = 0; i < actionMapArrayProperty.arraySize; i++)
            {
                var actionMapProperty = actionMapArrayProperty.GetArrayElementAtIndex(i);
                var actionMapItem = new ActionMapTreeItem(actionMapProperty, i);
                root.AddChild(actionMapItem);
            }
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.dragAndDropPosition != DragAndDropPosition.BetweenItems)
                return DragAndDropVisualMode.None;

            var id = Int32.Parse(DragAndDrop.paths.First());
            var item = FindItem(id, rootItem);
            var row = (ActionMapTreeItem)item;

            if (!row.isDraggable || args.parentItem != row.parent)
            {
                return DragAndDropVisualMode.None;
            }

            if (args.performDrop)
            {
                var dstIndex = args.insertAtIndex;
                var srcIndex = row.index;

                if (dstIndex > srcIndex)
                {
                    dstIndex--;
                }

                InputActionSerializationHelpers.MoveActionMap(m_SerializedObject, srcIndex, dstIndex);
                m_ApplyAction();
                DragAndDrop.AcceptDrag();
            }
            return DragAndDropVisualMode.Move;
        }
    }
}
#endif // UNITY_EDITOR
