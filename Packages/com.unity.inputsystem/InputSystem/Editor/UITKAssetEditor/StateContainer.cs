#if UNITY_EDITOR
using System;
using System.Linq.Expressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    // Enum used to dictate if a state change should rebuild the Input Actions editor UI
    internal enum UIRebuildMode
    {
        None,
        Rebuild,
    }

    internal class StateContainer
    {
        public event Action<InputActionsEditorState, UIRebuildMode> StateChanged;

        private VisualElement m_RootVisualElement;
        private InputActionsEditorState m_State;
        public readonly string assetGUID;

        // When set, the next change detected by the TrackSerializedObjectValue callback below does not rebuild
        // the editor UI. Used to commit value-only edits (e.g. an interaction/processor parameter) without
        // tearing down and recreating the field the user is interacting with (UUM-144339). It is a one-shot:
        // the very next change notification consumes it, so unrelated changes still rebuild as normal.
        private bool m_IgnoreNextSerializedObjectChange;

        public StateContainer(InputActionsEditorState initialState, string assetGUID)
        {
            m_State = initialState;
            this.assetGUID = assetGUID;
        }

        public void Dispatch(Command command, UIRebuildMode editorRebuildMode = UIRebuildMode.Rebuild)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            m_State = command(m_State);

            // why not just invoke the state changed event immediately you ask? The Dispatch method might have
            // been called from inside a UI element event handler and if we raised the event immediately, a view
            // might try to redraw itself *during* execution of the event handler.
            m_RootVisualElement.schedule.Execute(() =>
            {
                // catch exceptions here or the UIToolkit scheduled event will keep firing forever.
                try
                {
                    StateChanged?.Invoke(m_State, editorRebuildMode);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });
        }

        // Request that the editor UI is not rebuilt in response to the next change of the tracked serialized
        // object. Call this immediately before committing a value-only edit (ApplyModifiedProperties) that must
        // not tear down the VisualElement the user is interacting with. The request is consumed by the next
        // change notification, so any subsequent change still rebuilds normally.
        public void IgnoreNextSerializedObjectChange()
        {
            m_IgnoreNextSerializedObjectChange = true;
        }

        public void Initialize(VisualElement rootVisualElement)
        {
            // We need to use a root element for the TrackSerializedObjectValue that is destroyed with the view.
            // Using a root element from the settings window would not enable the tracking callback to be destroyed or garbage collected.

            m_RootVisualElement = rootVisualElement;

            m_RootVisualElement.Unbind();
            m_RootVisualElement.TrackSerializedObjectValue(m_State.serializedObject, so =>
            {
                // A value-only edit asked us not to rebuild the UI for this change (see
                // IgnoreNextSerializedObjectChange). Consume the request and skip the rebuild.
                if (m_IgnoreNextSerializedObjectChange)
                {
                    m_IgnoreNextSerializedObjectChange = false;
                    return;
                }

                StateChanged?.Invoke(m_State, UIRebuildMode.Rebuild);
            });
            StateChanged?.Invoke(m_State, UIRebuildMode.Rebuild);
            rootVisualElement.Bind(m_State.serializedObject);
        }

        /// <summary>
        /// Return a copy of the state.
        /// </summary>
        /// <remarks>
        /// It can sometimes be necessary to get access to the state outside of a state change event, like for example
        /// when creating views in response to UI click events. This method is for those times.
        /// </remarks>
        /// <returns></returns>
        public InputActionsEditorState GetState()
        {
            return m_State;
        }

        public void Bind<TValue>(Expression<Func<InputActionsEditorState, ReactiveProperty<TValue>>> expr,
            Action<InputActionsEditorState> propertyChangedCallback)
        {
            WhenChanged(expr, propertyChangedCallback);
            propertyChangedCallback(m_State);
        }

        public void Bind(Expression<Func<InputActionsEditorState, SerializedProperty>> expr,
            Action<SerializedProperty> serializedPropertyChangedCallback)
        {
            var propertyGetterFunc = WhenChanged(expr, serializedPropertyChangedCallback);
            serializedPropertyChangedCallback(propertyGetterFunc(m_State));
        }

        public Func<InputActionsEditorState, ReactiveProperty<TValue>> WhenChanged<TValue>(Expression<Func<InputActionsEditorState, ReactiveProperty<TValue>>> expr,
            Action<InputActionsEditorState> propertyChangedCallback)
        {
            var func = ExpressionUtils.CreateGetter(expr);
            if (func == null)
                throw new ArgumentException($"Couldn't get property info from expression.");

            var prop = func(m_State);
            if (prop == null)
                throw new InvalidOperationException($"ReactiveProperty {expr} has not been assigned.");

            prop.Changed += _ => propertyChangedCallback(m_State);

            return func;
        }

        public Func<InputActionsEditorState, SerializedProperty> WhenChanged(Expression<Func<InputActionsEditorState, SerializedProperty>> expr,
            Action<SerializedProperty> serializedPropertyChangedCallback)
        {
            var serializedPropertyGetter = ExpressionUtils.CreateGetter(expr);
            if (serializedPropertyGetter == null)
                throw new ArgumentException($"Couldn't get property info from expression.");

            var serializedProperty = serializedPropertyGetter(m_State);
            if (serializedProperty == null)
                throw new InvalidOperationException($"ReactiveProperty {expr} has not been assigned.");

            m_RootVisualElement.TrackPropertyValue(serializedProperty, serializedPropertyChangedCallback);
            return serializedPropertyGetter;
        }
    }
}

#endif
