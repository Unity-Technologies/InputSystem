using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.Editor
{
    internal class CopyPasteHelper : MonoBehaviour
    {
        internal const string k_CopyPasteMarker = "INPUTASSET ";
        private const string k_StartOfText = "\u0002";
        private const string k_StartOfHeading = "\u0001";
        private const string k_EndOfTransmission = "\u0004";
        private const string k_EndOfTransmissionBlock = "\u0017";

        public static void CopySelectedTreeViewItemsToClipboard(List<SerializedProperty> treeViewItems)
        {
            var copyBuffer = new StringBuilder();
            CopyItems(treeViewItems, copyBuffer); // TODO: sort out contained children
            EditorGUIUtility.systemCopyBuffer = copyBuffer.ToString();
        }

        public static void CopyItems(List<SerializedProperty> items, StringBuilder buffer)
        {
            buffer.Append(k_CopyPasteMarker);
            foreach (var item in items)
            {
                CopyItemData(item, buffer);
                buffer.Append(k_EndOfTransmission);
            }
        }

        private static void CopyItemData(SerializedProperty item, StringBuilder buffer)
        {
            buffer.Append(k_StartOfHeading);
            buffer.Append(item.FindPropertyRelative("m_Name").stringValue);
            buffer.Append(k_StartOfText);
            // InputActionMaps have back-references to InputActionAssets. Make sure we ignore those.
            buffer.Append(item.CopyToJson());
            buffer.Append(k_EndOfTransmissionBlock);

            // if (!item.serializedDataIncludesChildren && item.hasChildrenIncludingHidden) //TODO: copying child data necessary?
            //     foreach (var child in item.childrenIncludingHidden)
            //         CopyItemData(child, buffer);
        }
    }
}
