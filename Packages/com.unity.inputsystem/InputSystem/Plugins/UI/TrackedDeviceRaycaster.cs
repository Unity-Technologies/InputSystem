#if UNITY_INPUT_SYSTEM_ENABLE_XR
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#pragma warning disable CS0649
namespace UnityEngine.InputSystem.UI
{
    public class TrackedDeviceRaycaster : BaseRaycaster
    {
        private struct RaycastHitData
        {
            public RaycastHitData(Graphic graphic, Vector3 worldHitPosition, Vector2 screenPosition, float distance)
            {
                this.graphic = graphic;
                this.worldHitPosition = worldHitPosition;
                this.screenPosition = screenPosition;
                this.distance = distance;
            }

            public Graphic graphic { get; set; }
            public Vector3 worldHitPosition { get; set; }
            public Vector2 screenPosition { get; set; }
            public float distance { get; set; }
        }

        [SerializeField]
        private bool ignoreReversedGraphics;

        [SerializeField]
        private bool checkFor2DOcclusion;

        [SerializeField]
        private bool checkFor3DOcclusion;

        [SerializeField]
        private LayerMask m_BlockingMask;

        [NonSerialized]
        private Canvas m_Canvas;

        private Canvas canvas
        {
            get
            {
                if (m_Canvas != null)
                    return m_Canvas;

                m_Canvas = GetComponent<Canvas>();
                return m_Canvas;
            }
        }

        public override Camera eventCamera
        {
            get
            {
                var myCanvas = canvas;
                return myCanvas != null ? myCanvas.worldCamera : null;
            }
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            var trackedEventData = eventData as TrackedPointerEventData;
            if (trackedEventData != null)
            {
                PerformRaycast(trackedEventData, resultAppendList);
            }
        }

        // Use this list on each raycast to avoid continually allocating.
        [NonSerialized]
        private List<RaycastHitData> m_RaycastResultsCache = new List<RaycastHitData>();

        private void PerformRaycast(TrackedPointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (canvas == null)
                return;

            if (eventCamera == null)
                return;

            var ray = eventData.ray;

            var hitDistance = eventData.maxDistance;
            if (checkFor3DOcclusion)
            {
                var hits = Physics.RaycastAll(ray, hitDistance, m_BlockingMask);

                if (hits.Length > 0 && hits[0].distance < hitDistance)
                {
                    hitDistance = hits[0].distance;
                }
            }

            if (checkFor2DOcclusion)
            {
                var raycastDistance = hitDistance;
                var hits = Physics2D.GetRayIntersectionAll(ray, raycastDistance, m_BlockingMask);

                if (hits.Length > 0 && hits[0].fraction * raycastDistance < hitDistance)
                {
                    hitDistance = hits[0].fraction * raycastDistance;
                }
            }

            m_RaycastResultsCache.Clear();
            SortedRaycastGraphics(canvas, ray, m_RaycastResultsCache);
            var screenPosition = Vector2.zero;
            if (m_RaycastResultsCache.Count == 0)
            {
                Vector3 endPosition = ray.origin + (ray.direction.normalized * hitDistance);
                screenPosition = eventCamera.WorldToScreenPoint(endPosition);
            }
            else
            {
                screenPosition = m_RaycastResultsCache[0].screenPosition;
            }

            var thisFrameDelta = screenPosition - eventData.position;
            eventData.position = screenPosition;
            eventData.delta = thisFrameDelta;

            //Now that we have a list of sorted hits, process any extra settings and filters.
            for (var i = 0; i < m_RaycastResultsCache.Count; i++)
            {
                var validHit = true;

                var hitData = m_RaycastResultsCache[i];

                var go = hitData.graphic.gameObject;
                if (ignoreReversedGraphics)
                {
                    var forward = ray.direction;
                    var goDirection = go.transform.rotation * Vector3.forward;
                    validHit = Vector3.Dot(forward, goDirection) > 0;
                }

                validHit &= hitData.distance < hitDistance;

                if (validHit)
                {
                    var castResult = new RaycastResult
                    {
                        gameObject = go,
                        module = this,
                        distance = hitData.distance,
                        index = resultAppendList.Count,
                        depth = hitData.graphic.depth,

                        worldPosition = hitData.worldHitPosition
                    };
                    resultAppendList.Add(castResult);
                }
            }
        }

        [NonSerialized]
        static readonly List<RaycastHitData> s_SortedGraphics = new List<RaycastHitData>();
        private void SortedRaycastGraphics(Canvas canvas, Ray ray, List<RaycastHitData> results)
        {
            var graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);

            s_SortedGraphics.Clear();
            for (int i = 0; i < graphics.Count; ++i)
            {
                Graphic graphic = graphics[i];

                if (graphic.depth == -1)
                    continue;

                Vector3 worldPos;
                float distance;
                if (RayIntersectsRectTransform(graphic.rectTransform, ray, out worldPos, out distance))
                {
                    Vector2 screenPos = eventCamera.WorldToScreenPoint(worldPos);
                    // mask/image intersection - See Unity docs on eventAlphaThreshold for when this does anything
                    if (graphic.Raycast(screenPos, eventCamera))
                    {
                        s_SortedGraphics.Add(new RaycastHitData(graphic, worldPos, screenPos, distance));
                    }
                }
            }

            s_SortedGraphics.Sort((g1, g2) => g2.graphic.depth.CompareTo(g1.graphic.depth));

            results.AddRange(s_SortedGraphics);
        }

        private bool RayIntersectsRectTransform(RectTransform transform, Ray ray, out Vector3 worldPosition, out float distance)
        {
            var corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            var plane = new Plane(corners[0], corners[1], corners[2]);

            float enter;
            if (plane.Raycast(ray, out enter))
            {
                var intersection = ray.GetPoint(enter);

                var bottomEdge = corners[3] - corners[0];
                var leftEdge = corners[1] - corners[0];
                var bottomDot = Vector3.Dot(intersection - corners[0], bottomEdge);
                var leftDot = Vector3.Dot(intersection - corners[0], leftEdge);

                // If the intersection is right of the left edge and above the bottom edge.
                if (leftDot >= 0 && bottomDot >= 0)
                {
                    var topEdge = corners[1] - corners[2];
                    var rightEdge = corners[3] - corners[2];
                    var topDot = Vector3.Dot(intersection - corners[2], topEdge);
                    var rightDot = Vector3.Dot(intersection - corners[2], rightEdge);

                    //If the intersection is left of the right edge, and below the top edge
                    if (topDot >= 0 && rightDot >= 0)
                    {
                        worldPosition = intersection;
                        distance = enter;
                        return true;
                    }
                }
            }
            worldPosition = Vector3.zero;
            distance = 0;
            return false;
        }
    }
}
#endif
