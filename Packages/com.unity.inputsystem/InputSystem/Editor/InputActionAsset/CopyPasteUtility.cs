#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class CopyPasteUtility
    {
        private const string k_InputAssetMarker = "INPUTASSET ";

        private InspectorTree m_Tree;
        private ActionMapsTree m_ActionMapsTree;
        private ActionsTree m_ActionsTree;

        private SerializedObject m_SerializedObject;
        private Action m_Apply;

        private readonly GUIContent m_CutGUI = EditorGUIUtility.TrTextContent("Cut");
        private readonly GUIContent m_CopyGUI = EditorGUIUtility.TrTextContent("Copy");
        private readonly GUIContent m_PasteGUI = EditorGUIUtility.TrTextContent("Paste");
        private readonly GUIContent m_DeleteGUI = EditorGUIUtility.TrTextContent("Delete");
        private readonly GUIContent m_DuplicateGUI = EditorGUIUtility.TrTextContent("Duplicate");
        private readonly GUIContent m_RenameGUI = EditorGUIUtility.TrTextContent("Rename");

        public CopyPasteUtility(Action apply, ActionMapsTree actionMapsTree, ActionsTree actionsTree, SerializedObject serializedObject)
        {
            m_Apply = apply;
            m_ActionMapsTree = actionMapsTree;
            m_ActionsTree = actionsTree;
            m_SerializedObject = serializedObject;
        }

        public CopyPasteUtility(InspectorTree tree)
        {
            m_Tree = tree;
            m_Apply = () =>
            {
                if (m_Tree != null)
                    m_Tree.Reload();
            };
        }

        private void HandleCopyEvent()
        {
            if (!CanCopySelection())
            {
                EditorGUIUtility.systemCopyBuffer = null;
                EditorApplication.Beep();
                return;
            }

            var selectedRows = GetSelectedRows();
            var rowTypes = selectedRows.Select(r => r.GetType()).Distinct().ToList();

            // Don't allow to copy different type. It will hard to handle pasting
            if (rowTypes.Count() > 1)
            {
                EditorGUIUtility.systemCopyBuffer = null;
                EditorApplication.Beep();
                return;
            }

            var copyList = new StringBuilder(k_InputAssetMarker);
            foreach (var row in selectedRows)
            {
                copyList.Append(row.GetType().Name + "\n");
                copyList.Append(row.SerializeToString());
                copyList.Append(k_InputAssetMarker);

                if (m_ActionMapsTree != null && m_ActionMapsTree.HasFocus())
                {
                    CopyChildrenItems(m_ActionsTree.GetRootElement(), copyList);
                }
                if (row.hasChildren)
                {
                    CopyChildrenItems(row, copyList);
                }
            }
            EditorGUIUtility.systemCopyBuffer = copyList.ToString();
        }

        private static void CopyChildrenItems(TreeViewItem parent, StringBuilder result)
        {
            foreach (var treeViewItem in parent.children)
            {
                var item = (ActionTreeViewItem)treeViewItem;
                result.Append(item.GetType().Name + "\n");
                result.Append(item.SerializeToString());
                result.Append(k_InputAssetMarker);
                if (item.hasChildren)
                {
                    CopyChildrenItems(item, result);
                }
            }
        }

        private bool CanCopySelection()
        {
            var selectedRows = GetSelectedRows();
            var rowTypes = selectedRows.Select(r => r.GetType()).Distinct().ToList();
            if (rowTypes.Count != 1)
                return false;
            if (rowTypes.Single() == typeof(CompositeTreeItem))
                return false;
            return true;
        }

        private void HandlePasteEvent()
        {
            var copyBufferString = EditorGUIUtility.systemCopyBuffer;
            var elements = copyBufferString.Split(new[] { k_InputAssetMarker }, StringSplitOptions.RemoveEmptyEntries);
            if (!copyBufferString.StartsWith(k_InputAssetMarker))
                return;
            SerializedProperty currentActionMapProperty = null;
            var selectedActionMap = GetSelectedActionMap();
            if (selectedActionMap != null)
                currentActionMapProperty = selectedActionMap.elementProperty;
            for (var i = 0; i < elements.Length; i++)
            {
                var row = elements[i];

                if (IsRowOfType<ActionMapTreeItem>(ref row))
                {
                    if (m_SerializedObject == null)
                        throw new InvalidOperationException("Pasting action map is not a valid operation");

                    currentActionMapProperty = InputActionSerializationHelpers.AddActionMapFromSavedProperties(m_SerializedObject, GetParameterDictionary(row));
                    m_Apply();
                    continue;
                }

                if (IsRowOfType<ActionTreeItem>(ref row))
                {
                    var newActionProperty = InputActionSerializationHelpers.AddActionFromSavedProperties(GetParameterDictionary(row), currentActionMapProperty);

                    while (i + 1 < elements.Length)
                    {
                        try
                        {
                            var nextRow = elements[i + 1];
                            if (nextRow.StartsWith(typeof(BindingTreeItem).Name))
                            {
                                nextRow = nextRow.Substring(typeof(BindingTreeItem).Name.Length);
                            }
                            else if (nextRow.StartsWith(typeof(CompositeGroupTreeItem).Name))
                            {
                                nextRow = nextRow.Substring(typeof(CompositeGroupTreeItem).Name.Length);
                            }
                            else if (nextRow.StartsWith(typeof(CompositeTreeItem).Name))
                            {
                                nextRow = nextRow.Substring(typeof(CompositeTreeItem).Name.Length);
                            }
                            else
                            {
                                break;
                            }
                            InputActionSerializationHelpers.AddBindingFromSavedProperties(GetParameterDictionary(nextRow), newActionProperty, currentActionMapProperty);
                            i++;
                        }
                        catch (ArgumentException e)
                        {
                            Debug.LogException(e);
                            break;
                        }
                    }
                    m_Apply();
                    continue;
                }

                if (IsRowOfType<BindingTreeItem>(ref row)
                    || IsRowOfType<CompositeGroupTreeItem>(ref row)
                    || IsRowOfType<CompositeTreeItem>(ref row))
                {
                    var selectedRow = GetSelectedAction();
                    if (selectedRow == null)
                    {
                        EditorApplication.Beep();
                        continue;
                    }

                    selectedRow.AddBindingFromSavedProperties(GetParameterDictionary(row));
                    m_Apply();
                    continue;
                }
            }
        }

        static Dictionary<string, string> GetParameterDictionary(string data)
        {
            var result = new Dictionary<string, string>();
            foreach (var row in data.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = row.IndexOf('=');
                var key = row.Substring(0, idx).Trim();
                var value = row.Substring(idx + 1).Trim();
                result.Add(key, value);
            }
            return result;
        }

        private static bool IsRowOfType<T>(ref string row)
        {
            if (row.StartsWith(typeof(T).Name))
            {
                row = row.Substring(typeof(T).Name.Length);
                return true;
            }
            return false;
        }

        public static bool IsValidCommand(string currentCommandName)
        {
            return Event.current.commandName == "Copy"
                || Event.current.commandName == "Paste"
                || Event.current.commandName == "Cut"
                || Event.current.commandName == "Duplicate"
                || Event.current.commandName == "Delete";
        }

        public void HandleCommandEvent(string currentCommandName)
        {
            switch (Event.current.commandName)
            {
                case "Copy":
                    HandleCopyEvent();
                    Event.current.Use();
                    break;
                case "Paste":
                    HandlePasteEvent();
                    Event.current.Use();
                    break;
                case "Cut":
                    HandleCopyEvent();
                    DeleteSelectedRows();
                    Event.current.Use();
                    break;
                case "Duplicate":
                    HandleCopyEvent();
                    HandlePasteEvent();
                    Event.current.Use();
                    break;
                case "Delete":
                    DeleteSelectedRows();
                    Event.current.Use();
                    break;
            }
        }

        private void DeleteSelectedRows()
        {
            var rows = GetSelectedRows().ToArray();
            var rowTypes = rows.Select(r => r.GetType()).Distinct().ToList();
            // Don't allow to delete different types at once because it's hard to handle.
            if (rowTypes.Count() > 1)
            {
                EditorApplication.Beep();
                return;
            }

            // Remove composite bindings
            foreach (var compositeGroup in FindRowsToDeleteOfType<CompositeGroupTreeItem>(rows))
            {
                var action = (compositeGroup.parent as ActionTreeItem);
                for (var i = compositeGroup.children.Count - 1; i >= 0; i--)
                {
                    var composite = (CompositeTreeItem)compositeGroup.children[i];
                    action.RemoveBinding(composite.index);
                }
                action.RemoveBinding(compositeGroup.index);
            }

            // Remove bindings
            foreach (var bindingRow in FindRowsToDeleteOfType<BindingTreeItem>(rows))
            {
                var action = bindingRow.parent as ActionTreeItem;
                action.RemoveBinding(bindingRow.index);
            }

            // Remove actions
            foreach (var actionRow in FindRowsToDeleteOfType<ActionTreeItem>(rows))
            {
                var action = actionRow;
                ActionMapTreeItem actionMap;
                if (m_Tree != null)
                {
                    actionMap = actionRow.parent as ActionMapTreeItem;
                }
                else
                {
                    actionMap = m_ActionMapsTree.GetSelectedActionMap();
                }

                var bindingsCount = InputActionSerializationHelpers.GetBindingCount(actionMap.bindingsProperty, action.actionName);
                for (var i = bindingsCount - 1; i >= 0; i--)
                {
                    action.RemoveBinding(i);
                }
                actionMap.DeleteAction(actionRow.index);
            }

            //Remove action maps
            foreach (var mapRow in FindRowsToDeleteOfType<ActionMapTreeItem>(rows))
            {
                if (m_SerializedObject == null)
                    throw new InvalidOperationException("Deleting action map is not a valid operation");
                InputActionSerializationHelpers.DeleteActionMap(m_SerializedObject, mapRow.index);
            }

            SetEmptySelection();
            m_Apply();
        }

        static IEnumerable<T> FindRowsToDeleteOfType<T>(ActionTreeViewItem[] rows)
        {
            return rows.Where(r => r.GetType() == typeof(T)).OrderByDescending(r => r.index).Cast<T>();
        }

        public void AddOptionsToMenu(GenericMenu menu)
        {
            var canCopySelection = CanCopySelection();
            menu.AddSeparator("");
            if (canCopySelection)
            {
                menu.AddItem(m_CutGUI, false, () => EditorApplication.ExecuteMenuItem("Edit/Cut"));
                menu.AddItem(m_CopyGUI, false, () => EditorApplication.ExecuteMenuItem("Edit/Copy"));
            }
            else
            {
                menu.AddDisabledItem(m_CutGUI, false);
                menu.AddDisabledItem(m_CopyGUI, false);
            }
            menu.AddItem(m_PasteGUI, false, () => EditorApplication.ExecuteMenuItem("Edit/Paste"));
            menu.AddSeparator("");
            if (CanRenameCurrentSelection())
            {
                menu.AddItem(m_RenameGUI, false, BeginRename);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent(m_RenameGUI));
            }
            if (canCopySelection)
            {
                menu.AddItem(m_DuplicateGUI, false, () => EditorApplication.ExecuteMenuItem("Edit/Duplicate"));
            }
            else
            {
                menu.AddDisabledItem(m_DuplicateGUI, false);
            }
            menu.AddItem(m_DeleteGUI, false, () => EditorApplication.ExecuteMenuItem("Edit/Delete"));
        }

        IEnumerable<ActionTreeViewItem> GetSelectedRows()
        {
            if (m_Tree != null && m_Tree.HasFocus())
                return m_Tree.GetSelectedRows();
            if (m_ActionMapsTree != null && m_ActionMapsTree.HasFocus())
                return m_ActionMapsTree.GetSelectedRows();
            if (m_ActionsTree != null && m_ActionsTree.HasFocus())
                return m_ActionsTree.GetSelectedRows();
            return null;
        }

        ActionTreeItem GetSelectedAction()
        {
            if (m_Tree != null)
                return m_Tree.GetSelectedAction();
            if (m_ActionsTree != null)
                return m_ActionsTree.GetSelectedAction();
            return null;
        }

        ActionMapTreeItem GetSelectedActionMap()
        {
            if (m_Tree != null)
                return m_Tree.GetSelectedActionMap();
            if (m_ActionMapsTree != null)
                return m_ActionMapsTree.GetSelectedActionMap();
            return null;
        }

        bool CanRenameCurrentSelection()
        {
            if (m_Tree != null && m_Tree.HasFocus())
                return m_Tree.CanRenameCurrentSelection();
            if (m_ActionMapsTree != null && m_ActionMapsTree.HasFocus())
                return m_ActionMapsTree.CanRenameCurrentSelection();
            if (m_ActionsTree != null && m_ActionsTree.HasFocus())
                return m_ActionsTree.CanRenameCurrentSelection();
            return false;
        }

        void SetEmptySelection()
        {
            if (m_Tree != null && m_Tree.HasFocus())
                m_Tree.SetSelection(new int[0]);
            if (m_ActionMapsTree != null && m_ActionMapsTree.HasFocus())
                m_ActionMapsTree.SetSelection(new int[0]);
            if (m_ActionsTree != null && m_ActionsTree.HasFocus())
                m_ActionsTree.SetSelection(new int[0]);
            ;
        }

        void BeginRename()
        {
            if (m_Tree != null && m_Tree.HasFocus())
                m_Tree.BeginRename(m_Tree.GetSelectedRow());
            if (m_ActionMapsTree != null && m_ActionMapsTree.HasFocus())
                m_ActionMapsTree.BeginRename(m_ActionMapsTree.GetSelectedRow());
            if (m_ActionsTree != null && m_ActionsTree.HasFocus())
                m_ActionsTree.BeginRename(m_ActionsTree.GetSelectedRow());
        }
    }
}
#endif // UNITY_EDITOR
