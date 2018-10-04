//#if UNITY_EDITOR
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEditor;
//using UnityEditor.IMGUI.Controls;
//
//namespace UnityEngine.Experimental.Input.Editor
//{
//    internal class InspectorTree : InputActionTreeBase
//    {
//       
//
////        protected override TreeViewItem BuildRoot()
////        {
////            var root = new TreeViewItem
////            {
////                id = 0,
////                depth = -1
////            };
////            root.children = new List<TreeViewItem>();
////            if (m_SerializedObject != null)
////            {
////                BuildFromSerializedObject(root);
////            }
////            return root;
////        }
////
////        private void BuildFromSerializedObject(TreeViewItem root)
////        {
////            m_SerializedObject.Update();
////            var actionMapArrayProperty = m_SerializedObject.FindProperty("m_ActionMaps");
////            for (var i = 0; i < actionMapArrayProperty.arraySize; i++)
////            {
////                var actionMapProperty = actionMapArrayProperty.GetArrayElementAtIndex(i);
////                var actionMapItem = new ActionMapTreeItem(actionMapProperty, i);
////                ParseActionMap(actionMapItem, actionMapProperty, 1);
////                root.AddChild(actionMapItem);
////            }
////        }
//
//       
//    }
//}
//#endif // UNITY_EDITOR
