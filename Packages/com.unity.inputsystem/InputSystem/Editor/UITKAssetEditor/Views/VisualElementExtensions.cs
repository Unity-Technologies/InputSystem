#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal static class VisualElementExtensions
    {
        public static TElement Q<TElement>(this VisualElement visualElement, string name) where TElement : VisualElement
        {
            var element = UQueryExtensions.Q<TElement>(visualElement, name);
            if (element == null)
                throw new InvalidOperationException(
                    $"Expected a visual element called '{name}' of type '{typeof(TElement)}' to exist " +
                    $"but none was found.");

            return element;
        }
    }
}

#endif
