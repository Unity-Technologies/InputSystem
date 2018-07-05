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

        public CopyPasteUtility(ActionInspectorWindow window)
        {
            m_Window = window;
            m_TreeView = window.m_TreeView;
        }

        public void HandleCopyEvent()
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

        public bool CanCopySelection()
        {
            var selectedRows = m_TreeView.GetSelectedRows();
            var rowTypes = selectedRows.Select(r => r.GetType()).Distinct().ToList();
            return rowTypes.Count == 1;
        }

        public void HandlePasteEvent()
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
                    InputActionSerializationHelpers.AddActionMapFromObject(m_Window.m_SerializedObject, map);
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
    }
}
#endif // UNITY_EDITOR
