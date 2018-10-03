using System;
using UnityEngine.EventSystems;

////REVIEW: apparently EventSystem only supports a single "current" module so the approach here probably
////        won't fly and we'll have to roll all non-action modules into one big module

namespace UnityEngine.Experimental.Input.Plugins.UI
{
    /// <summary>
    /// Base class for <see cref="BaseInputModule">input modules</see> that send
    /// UI input.
    /// </summary>
    /// <remarks>
    /// Multiple input modules may be placed on the same event system. In such a setup,
    /// the modules will synchronize with each other to not send
    /// </remarks>
    public abstract class UIInputModule : BaseInputModule
    {
        /// <summary>
        /// Send <see cref="IUpdateSelectedHandler.OnUpdateSelected"/> event to currently
        /// <see cref="EventSystem.currentSelectedGameObject">selected GameObject</see>.
        /// </summary>
        protected void SendOnUpdateSelected()
        {
            var selectedObject = eventSystem.currentSelectedGameObject;
            if (selectedObject == null)
                return;

            // OnUpdateSelected should really be called OnUpdate*When*Selected.

            var baseEventData = GetBaseEventData();
            ExecuteEvents.Execute(selectedObject, baseEventData, ExecuteEvents.updateSelectedHandler);
        }

        protected void PerformRaycast(PointerEventData eventData)
        {
            if (eventData == null)
                throw new ArgumentNullException("eventData");

            eventSystem.RaycastAll(eventData, m_RaycastResultCache);
            eventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();
        }

        protected PointerEventData GetOrCreateCachedPointerEvent()
        {
            var result = m_CachedPointerEvent;
            if (result == null)
            {
                result = new PointerEventData(eventSystem);
                m_CachedPointerEvent = result;
            }

            return result;
        }

        private AxisEventData m_CachedAxisEvent;
        private PointerEventData m_CachedPointerEvent;
        private BaseEventData m_CachedBaseEvent;
    }
}
