using System;
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

        private static readonly Dictionary<Type, string> k_TypeMarker = new Dictionary<Type, string>()
        {
            {typeof(InputActionMap), "InputActionMap"},
            {typeof(InputAction), "InputAction"},
            {typeof(InputBinding), "InputBinding"},
        };

        public static void CopySelectedTreeViewItemsToClipboard(List<SerializedProperty> items, Type type)
        {
            var copyBuffer = new StringBuilder();
            CopyItems(items, copyBuffer, type); // TODO: sort out contained children
            EditorGUIUtility.systemCopyBuffer = copyBuffer.ToString();
        }

        private static void CopyItems(List<SerializedProperty> items, StringBuilder buffer, Type type)
        {
            buffer.Append(k_CopyPasteMarker);
            buffer.Append(k_TypeMarker[type]);
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

        public bool HavePastableClipboardData(Type selectedType)
        {
            var clipboard = EditorGUIUtility.systemCopyBuffer;
            var isInputAssetData = clipboard.StartsWith(k_CopyPasteMarker);
            var isMatchingType = clipboard.Substring(k_CopyPasteMarker.Length).StartsWith(k_TypeMarker[selectedType]);
            return isInputAssetData && isMatchingType;
        }

        public static Type GetCopiedClipboardType()
        {
            if (!EditorGUIUtility.systemCopyBuffer.StartsWith(k_CopyPasteMarker))
                return null;
            foreach (var typePair in k_TypeMarker)
            {
                if (EditorGUIUtility.systemCopyBuffer.Substring(k_CopyPasteMarker.Length).StartsWith(typePair.Value))
                    return typePair.Key;
            }
            return null;
        }

        public static void PasteFromClipboard(int[] indicesToInsert)
        {
            PasteData(EditorGUIUtility.systemCopyBuffer, indicesToInsert);
        }

        private static void PasteData(string copyBufferString, int[] indicesToInsert)
        {
            if (!copyBufferString.StartsWith(k_CopyPasteMarker))
                return;

            ////REVIEW: filtering out children may remove the very item we need to get the right match for a copy block?
            PasteItems(copyBufferString, indicesToInsert);
        }

        private static void PasteItems(string copyBufferString, int[] indicesToInsert, bool assignNewIDs = true, bool selectNewItems = true)
        {
            var newItemPropertyPaths = new List<string>();

            // Split buffer into transmissions and then into transmission blocks. Each transmission is an item subtree
            // meant to be pasted as a whole and each transmission block is a single chunk of serialized data.
            foreach (var transmission in copyBufferString.Substring(k_CopyPasteMarker.Length + k_TypeMarker[GetCopiedClipboardType()].Length)
                     .Split(new[] {k_EndOfTransmission}, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var location in indicesToInsert)
                    PasteBlocks(transmission, location, assignNewIDs, newItemPropertyPaths);
            }
        }

        private static void PasteBlocks(string transmission, int indexToInsert, bool assignNewIDs, List<string> newItemPropertyPaths)
        {
        }
    }
}
