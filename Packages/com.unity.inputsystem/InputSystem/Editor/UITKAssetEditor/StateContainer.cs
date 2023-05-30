#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System;
using System.Linq.Expressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class StateContainer
    {
        public event Action<InputActionsEditorState> StateChanged;

        private readonly VisualElement m_RootVisualElement;
        private InputActionsEditorState m_State;

        public StateContainer(VisualElement rootVisualElement, InputActionsEditorState initialState)
        {
            m_RootVisualElement = rootVisualElement;
            m_State = initialState;

            m_RootVisualElement.TrackSerializedObjectValue(initialState.serializedObject, so =>
            {
                StateChanged?.Invoke(m_State);
            });
            rootVisualElement.Bind(initialState.serializedObject);
        }

        public void Dispatch(Command command)
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
                    StateChanged?.Invoke(m_State);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
            });
        }

        public void Initialize()
        {
            StateChanged?.Invoke(m_State);
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
