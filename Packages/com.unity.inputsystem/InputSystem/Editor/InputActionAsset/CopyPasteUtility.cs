#if UNITY_EDITOR
using System;
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
        
        GUIContent m_CutGUI = new GUIContent("Cut");
        GUIContent m_CopyGUI = new GUIContent("Copy");
        GUIContent m_PasteGUI = new GUIContent("Paste");
        GUIContent m_DeleteGUI = new GUIContent("Delete");
        GUIContent m_Duplicate = new GUIContent("Duplicate");

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
                        if (!(child is BindingTreeItem))
                            continue;
                        copyList.Append(child.GetType().Name);
                        copyList.Append((child as BindingTreeItem).SerializeToString());
                        copyList.Append(kInputAssetMarker);
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
            return rowTypes.Count == 1;
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
                    var newActionProperty = InputActionSerializationHelpers.AddActionFromObject(action, actionMap.elementProperty);
                    m_Window.Apply();

                    while (i + 1 < elements.Length)
                    {
                        try
                        {
                            var nextRow = elements[i + 1];
                            if (!nextRow.StartsWith(typeof(BindingTreeItem).Name))
                            {
                                break;
                            }
                            nextRow = nextRow.Substring(typeof(BindingTreeItem).Name.Length);
                            var binding = JsonUtility.FromJson<InputBinding>(nextRow);
                            InputActionSerializationHelpers.AppendBindingFromObject(binding, newActionProperty, actionMap.elementProperty);
                            m_Window.Apply();
                            i++;
                        }
                        catch (ArgumentException e)
                        {
                            Debug.LogException(e);
                            break;
                        }
                    }
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

                    var actionMap = m_TreeView.GetSelectedActionMap();
                    InputActionSerializationHelpers.AppendBindingFromObject(binding, selectedRow.elementProperty, actionMap.elementProperty);
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
            foreach (var compositeGroup in rows.Where(r => r.GetType() == typeof(CompositeGroupTreeItem)).OrderByDescending(r => r.index).Cast<CompositeGroupTreeItem>())
            {
                var actionMapProperty = (compositeGroup.parent.parent as InputTreeViewLine).elementProperty;
                var actionProperty = (compositeGroup.parent as ActionTreeItem).elementProperty;
                for (var i = compositeGroup.children.Count - 1; i >= 0; i--)
                {
                    var composite = (CompositeTreeItem)compositeGroup.children[i];
                    InputActionSerializationHelpers.RemoveBinding(actionProperty, composite.index, actionMapProperty);
                }
                InputActionSerializationHelpers.RemoveBinding(actionProperty, compositeGroup.index, actionMapProperty);
            }
            foreach (var bindingRow in rows.Where(r => r.GetType() == typeof(BindingTreeItem)).OrderByDescending(r => r.index).Cast<BindingTreeItem>())
            {
                var actionMapProperty = (bindingRow.parent.parent as InputTreeViewLine).elementProperty;
                var actionProperty = (bindingRow.parent as InputTreeViewLine).elementProperty;
                InputActionSerializationHelpers.RemoveBinding(actionProperty, bindingRow.index, actionMapProperty);
            }
            foreach (var actionRow in rows.Where(r => r.GetType() == typeof(ActionTreeItem)).OrderByDescending(r => r.index).Cast<ActionTreeItem>())
            {
                var actionProperty = (actionRow).elementProperty;
                var actionMapProperty = (actionRow.parent as InputTreeViewLine).elementProperty;

                for (var i = actionRow.bindingsCount - 1; i >= 0; i--)
                    InputActionSerializationHelpers.RemoveBinding(actionProperty, i, actionMapProperty);

                InputActionSerializationHelpers.DeleteAction(actionMapProperty, actionRow.index);
            }
            foreach (var mapRow in rows.Where(r => r.GetType() == typeof(ActionMapTreeItem)).OrderByDescending(r => r.index).Cast<ActionMapTreeItem>())
            {
                InputActionSerializationHelpers.DeleteActionMap(m_SerializedObject, mapRow.index);
            }
            m_Window.Apply();
            m_Window.OnSelectionChanged();
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
