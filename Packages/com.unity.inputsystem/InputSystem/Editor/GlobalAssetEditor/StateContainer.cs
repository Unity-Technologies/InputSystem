using System;
using System.Linq.Expressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
	internal class StateContainer
	{
		public event Action<GlobalInputActionsEditorState> StateChanged;

		private readonly VisualElement m_RootVisualElement;
		private GlobalInputActionsEditorState m_State;

		public StateContainer(VisualElement rootVisualElement, GlobalInputActionsEditorState initialState)
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
			command?.Invoke(ref m_State);
			StateChanged?.Invoke(m_State);
		}

		public void Initialize()
		{
			StateChanged?.Invoke(m_State);
		}
		
		public void Bind<TValue>(Expression<Func<GlobalInputActionsEditorState, ReactiveProperty<TValue>>> expr,
			Action<GlobalInputActionsEditorState> propertyChangedCallback)
		{
			WhenChanged(expr, propertyChangedCallback);
			propertyChangedCallback(m_State);
		}

		public void Bind(Expression<Func<GlobalInputActionsEditorState, SerializedProperty>> expr,
			Action<SerializedProperty> serializedPropertyChangedCallback)
		{
			var propertyGetterFunc = WhenChanged(expr, serializedPropertyChangedCallback);
			serializedPropertyChangedCallback(propertyGetterFunc(m_State));
		}

		public Func<GlobalInputActionsEditorState, ReactiveProperty<TValue>> WhenChanged<TValue>(Expression<Func<GlobalInputActionsEditorState, ReactiveProperty<TValue>>> expr,
			Action<GlobalInputActionsEditorState> propertyChangedCallback)
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

		public Func<GlobalInputActionsEditorState, SerializedProperty> WhenChanged(Expression<Func<GlobalInputActionsEditorState, SerializedProperty>> expr,
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