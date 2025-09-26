using System;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.OnScreen
{
    // Current implementation has UGUI dependencies (ISXB-915, ISXB-916)

    internal static class UGUIOnScreenControlUtils
    {
        public static RectTransform GetCanvasRectTransform(Transform transform)
        {
            var parentTransform = transform.parent;
            return parentTransform != null ? transform.parent.GetComponentInParent<RectTransform>() : null;
        }
    }

    // TODO Reconsider this before landing anything, there seems to be no way to access accumulated state without unsafe code?
    public static class OnScreenUnsafeHelpers
    {
        public static bool TryGetStateCopy<T>(InputEventPtr eventPtr, int format, out T touchState) where T : unmanaged
        {
            try
            {
                if (eventPtr == null || eventPtr.type != format)
                {
                    unsafe
                    {
                        var stateEvent = (StateEvent*)eventPtr.ToPointer();
                        if (stateEvent->stateFormat == TouchState.Format)
                        {
                            touchState = *(T*)stateEvent->state;
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            touchState = default(T);
            return false;
        }
    }

#if UNITY_EDITOR
    internal static class UGUIOnScreenControlEditorUtils
    {
        public static void ShowWarningIfNotPartOfCanvasHierarchy(OnScreenControl target)
        {
            if (UGUIOnScreenControlUtils.GetCanvasRectTransform(target.transform) == null)
                UnityEditor.EditorGUILayout.HelpBox(target.GetWarningMessage(), UnityEditor.MessageType.Warning);
        }
    }
#endif
}
