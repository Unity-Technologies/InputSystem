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
        const string kInputAssetMarker = "INPUTASSET\n";
        InputActionListTreeView m_TreeView;
        ActionInspectorWindow m_Window;
        SerializedObject m_SerializedObject;

        GUIContent m_CutGUI = EditorGUIUtility.TrTextContent("Cut");
        GUIContent m_CopyGUI = EditorGUIUtility.TrTextContent("Copy");
        GUIContent m_PasteGUI = EditorGUIUtility.TrTextContent("Paste");
        GUIContent m_DeleteGUI = EditorGUIUtility.TrTextContent("Delete");
        GUIContent m_Duplicate = EditorGUIUtility.TrTextContent("Duplicate");

        public CopyPasteUtility(ActionInspectorWindow window, InputActionListTreeView tree, SerializedObject serializedObject)
        {
            m_Window = window;
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

            var copyList = new StringBuilder(kInputAssetMarker);
            foreach (var selectedRow in selectedRows)
            {
                copyList.Append(selectedRow.GetType().Name);
                copyList.Append(selectedRow.SerializeToString());
                copyList.Append(kInputAssetMarker);

                if (selectedRow is ActionTreeItem && selectedRow.children != null && selectedRow.children.Count > 0)
                {
                    var action = selectedRow as ActionTreeItem;

                    foreach (var child in action.children)
                    {
                        copyList.Append(child.GetType().Name);
                        copyList.Append((child as BindingTreeItem).SerializeToString());
                        copyList.Append(kInputAssetMarker);
                        // Copy composites
                        if (child.hasChildren)
                        {
                            foreach (var innerChild in child.children)
                            {
                                copyList.Append(innerChild.GetType().Name);
                                copyList.Append((innerChild as BindingTreeItem).SerializeToString());
                                copyList.Append(kInputAssetMarker);
                            }
                        }
                    }
                }
                if (selectedRow is CompositeGroupTreeItem && selectedRow.children != null && selectedRow.children.Count > 0)
                {
                    var composite = selectedRow as CompositeGroupTreeItem;

                    foreach (var child in composite.children)
                    {
                        if (!(child is CompositeTreeItem))
                            continue;
                        copyList.Append(child.GetType().Name);
                        copyList.Append((child as CompositeTreeItem).SerializeToString());
                        copyList.Append(kInputAssetMarker);
                    }
                }
            }
            EditorGUIUtility.systemCopyBuffer = copyList.ToString();
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
            var json = EditorGUIUtility.systemCopyBuffer;
            var elements = json.Split(new[] { kInputAssetMarker }, StringSplitOptions.RemoveEmptyEntries);
            if (!json.StartsWith(kInputAssetMarker))
                return;
            for (var i = 0; i < elements.Length; i++)
            {
                var row = elements[i];

                if (IsRowOfType<ActionMapTreeItem>(ref row))
                {
                    var map = JsonUtility.FromJson<InputActionMap>(row);
                    InputActionSerializationHelpers.AddActionMapFromObject(m_SerializedObject, map);
                    m_Window.Apply();
                    continue;
                }

                if (IsRowOfType<ActionTreeItem>(ref row))
                {
                    var action = JsonUtility.FromJson<InputAction>(row);
                    var actionMap = m_TreeView.GetSelectedActionMap();
                    var newActionProperty = actionMap.AddActionFromObject(action);

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
                            var binding = JsonUtility.FromJson<InputBinding>(nextRow);
                            InputActionSerializationHelpers.AppendBindingFromObject(binding, newActionProperty, actionMap.elementProperty);
                            i++;
                        }
                        catch (ArgumentException e)
                        {
                            Debug.LogException(e);
                            break;
                        }
                    }
                    m_Window.Apply();

                    continue;
                }

                if (IsRowOfType<BindingTreeItem>(ref row)
                    || IsRowOfType<CompositeGroupTreeItem>(ref row)
                    || IsRowOfType<CompositeTreeItem>(ref row))
                {
                    var binding = JsonUtility.FromJson<InputBinding>(row);
                    var selectedRow = m_TreeView.GetSelectedAction();
                    if (selectedRow == null)
                    {
                        EditorApplication.Beep();
                        continue;
                    }

                    selectedRow.AppendBindingFromObject(binding);
                    m_Window.Apply();
                    continue;
                }
            }
        }

        bool IsRowOfType<T>(ref string row)
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
                for (var i = actionRow.bindingsCount - 1; i >= 0; i--)
                {
                    action.RemoveBinding(i);
                }
                actionMap.DeleteAction(actionRow.index);
            }

            //Remove action maps
            foreach (var mapRow in FindRowsToDeleteOfType<ActionMapTreeItem>(rows))
            {
                InputActionSerializationHelpers.DeleteActionMap(m_SerializedObject, mapRow.index);
            }

            m_Window.Apply();
            m_Window.OnSelectionChanged();
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
