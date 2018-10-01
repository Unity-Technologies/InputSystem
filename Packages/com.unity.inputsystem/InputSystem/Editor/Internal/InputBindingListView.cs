#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;

////REVIEW: after we've redone the action UIs, this hopefully won't be needed anymore

////TODO: reordering support

namespace UnityEngine.Experimental.Input.Editor
{
    // A ReorderableList that displays an editable list of bindings for an action.
    internal class InputBindingListView : ReorderableList
    {
        // Constructor for binding list of singleton actions.
        public InputBindingListView(SerializedProperty actionProperty, bool displayHeader = true)
            : base(actionProperty.serializedObject, actionProperty.FindPropertyRelative("m_SingletonActionBindings"))
        {
            Initialize(actionProperty, null, displayHeader);
        }

        // Constructor for binding list of actions that are part of action maps.
        public InputBindingListView(SerializedProperty actionProperty, SerializedProperty actionMapProperty, bool displayHeader = true)
            : base(new BindingList(actionProperty, actionMapProperty.FindPropertyRelative("m_Bindings")), typeof(SerializedProperty))
        {
            Initialize(actionProperty, actionMapProperty, displayHeader);
        }

        private void Initialize(SerializedProperty actionProperty, SerializedProperty actionMapProperty, bool displayHeader)
        {
            if (!displayHeader)
                headerHeight = 2;

            drawElementCallback =
                (rect, index, isActive, isFocused) =>
            {
                var binding = serializedProperty != null
                    ? serializedProperty.GetArrayElementAtIndex(index)
                    : (SerializedProperty)list[index];
                EditorGUI.PropertyField(rect, binding);
            };

            drawHeaderCallback =
                (rect) =>
            {
                if (displayHeader)
                    EditorGUI.LabelField(rect, "Bindings");
            };

            drawNoneElementCallback =
                (rect) =>
            {
                EditorGUI.LabelField(rect, s_NoBindingsText, EditorStyles.centeredGreyMiniLabel);
            };

            onAddCallback =
                (list) => InputActionSerializationHelpers.AddBinding(actionProperty, actionMapProperty);

            onRemoveCallback =
                (list) => InputActionSerializationHelpers.RemoveBinding(actionProperty, list.index, actionMapProperty);
        }

        private static GUIContent s_NoBindingsText = new GUIContent("None.");

        // Unfortunately, because of the way arrays are shared between actions in action sets, we
        // can't have ReorderableList access m_Bindings directly in the case of an action that is part
        // of a set (we need a slice of m_Bindings from the set, something we can't do with ReorderableList's
        // SerializedProperty-based interface). To work around this, we only have the IList-based API available,
        // so we manually wrap around the SerializedProperties here.
        //
        // Only implements the portion of IList actually used by ReorderableList -- which pretty much only
        // needs Count and the indexer.
        private class BindingList : IList
        {
            private string m_ActionName;
            private SerializedProperty m_BindingArrayProperty;

            public BindingList(SerializedProperty actionProperty, SerializedProperty bindingArrayProperty)
            {
                m_BindingArrayProperty = bindingArrayProperty;
                m_ActionName = actionProperty.FindPropertyRelative("m_Name").stringValue;
            }

            public int Count
            {
                get
                {
                    return InputActionSerializationHelpers.GetBindingCount(m_BindingArrayProperty, m_ActionName);
                }
            }

            public object this[int index]
            {
                get
                {
                    return InputActionSerializationHelpers.GetBinding(m_BindingArrayProperty, m_ActionName, index);
                }
                set { throw new NotSupportedException(); }
            }

            // The rest is unsupported.

            public IEnumerator GetEnumerator()
            {
                throw new NotSupportedException();
            }

            public void CopyTo(Array array, int index)
            {
                throw new NotSupportedException();
            }

            public int Add(object value)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(object value)
            {
                throw new NotSupportedException();
            }

            public int IndexOf(object value)
            {
                throw new NotSupportedException();
            }

            public void Insert(int index, object value)
            {
                throw new NotSupportedException();
            }

            public void Remove(object value)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            public bool IsSynchronized { get { return false; } }
            public object SyncRoot { get { return this; } }
            public bool IsFixedSize { get { return false; } }
            public bool IsReadOnly { get { return false; } }
        }
    }
}
#endif // UNITY_EDITOR
