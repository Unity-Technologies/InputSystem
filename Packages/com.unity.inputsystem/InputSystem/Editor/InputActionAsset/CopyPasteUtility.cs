#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    class CopyPasteUtility
    {
        const string k_InputAssetMarker = "INPUTASSET ";
        InputActionListTreeView m_TreeView;
        SerializedObject m_SerializedObject;
        Action m_Apply;

        GUIContent m_CutGUI = EditorGUIUtility.TrTextContent("Cut");
        GUIContent m_CopyGUI = EditorGUIUtility.TrTextContent("Copy");
        GUIContent m_PasteGUI = EditorGUIUtility.TrTextContent("Paste");
        GUIContent m_DeleteGUI = EditorGUIUtility.TrTextContent("Delete");
        GUIContent m_Duplicate = EditorGUIUtility.TrTextContent("Duplicate");

        public CopyPasteUtility(Action apply, InputActionListTreeView tree, SerializedObject serializedObject)
        {
            m_Apply = apply;
            m_TreeView = tree;
            m_SerializedObject = serializedObject;
        }

        void HandleCopyEvent()
        {
            if (!CanCopySelection())
            {
                EditorGUIUtility.systemCopyBuffer = null;
                EditorApplication.Beep();
                return;
            }

            var selectedRows = m_TreeView.GetSelectedRows();
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
                if (row.hasChildren)
                {
                    CopyChildrenItems(row, copyList);
                }
            }
            EditorGUIUtility.systemCopyBuffer = copyList.ToString();
        }

        void CopyChildrenItems(InputTreeViewLine parent, StringBuilder result)
        {
            foreach (var treeViewItem in parent.children)
            {
                var item = (InputTreeViewLine)treeViewItem;
                result.Append(item.GetType().Name + "\n");
                result.Append(item.SerializeToString());
                result.Append(k_InputAssetMarker);
                if (item.hasChildren)
                {
                    CopyChildrenItems(item, result);
                }
            }
        }

        bool CanCopySelection()
        {
            var selectedRows = m_TreeView.GetSelectedRows();
            var rowTypes = selectedRows.Select(r => r.GetType()).Distinct().ToList();
            if (rowTypes.Count != 1)
                return false;
            if (rowTypes.Single() == typeof(CompositeTreeItem))
                return false;
            return true;
        }

        void HandlePasteEvent()
        {
            var copyBufferString = EditorGUIUtility.systemCopyBuffer;
            var elements = copyBufferString.Split(new[] { k_InputAssetMarker }, StringSplitOptions.RemoveEmptyEntries);
            if (!copyBufferString.StartsWith(k_InputAssetMarker))
                return;
            var currentActionMapProperty = m_TreeView.GetSelectedActionMap().elementProperty;
            for (var i = 0; i < elements.Length; i++)
            {
                var row = elements[i];

                if (IsRowOfType<ActionMapTreeItem>(ref row))
                {
                    if (m_SerializedObject == null)
                        throw new InvalidOperationException("Pasting action map is not a valid operation");

                    currentActionMapProperty = InputActionSerializationHelpers.AddActionMapFromObject(m_SerializedObject, GetParameterDictionary(row));
                    m_Apply();
                    continue;
                }

                if (IsRowOfType<ActionTreeItem>(ref row))
                {
                    var newActionProperty = InputActionSerializationHelpers.AddActionFromObject(GetParameterDictionary(row), currentActionMapProperty);

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
                            InputActionSerializationHelpers.AppendBindingFromObject(GetParameterDictionary(nextRow), newActionProperty, currentActionMapProperty);
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
                    var selectedRow = m_TreeView.GetSelectedAction();
                    if (selectedRow == null)
                    {
                        EditorApplication.Beep();
                        continue;
                    }

                    selectedRow.AppendBindingFromObject(GetParameterDictionary(row));
                    m_Apply();
                    continue;
                }
            }
        }

        Dictionary<string, string> GetParameterDictionary(string data)
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

        static bool IsRowOfType<T>(ref string row)
        {
            if (row.StartsWith(typeof(T).Name))
            {
                row = row.Substring(typeof(T).Name.Length);
                return true;
            }
            return false;
        }

        public bool IsValidCommand(string currentCommandName)
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

        void DeleteSelectedRows()
        {
            var rows = m_TreeView.GetSelectedRows().ToArray();
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
                var actionMap = actionRow.parent as ActionMapTreeItem;

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

            m_TreeView.SetSelection(new List<int>());
            m_Apply();
        }

        IEnumerable<T> FindRowsToDeleteOfType<T>(InputTreeViewLine[] rows)
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
            menu.AddItem(m_DeleteGUI, false, () => EditorApplication.ExecuteMenuItem("Edit/Delete"));
            if (canCopySelection)
            {
                menu.AddItem(m_Duplicate, false, () => EditorApplication.ExecuteMenuItem("Edit/Duplicate"));
            }
            else
            {
                menu.AddDisabledItem(m_Duplicate, false);
            }
        }
    }
}
#endif // UNITY_EDITOR
