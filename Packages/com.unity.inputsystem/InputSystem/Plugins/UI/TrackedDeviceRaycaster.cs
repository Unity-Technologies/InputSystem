#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.UI
{
    /// <summary>
    /// Raycasting implementation for use with <see cref="TrackedDevice"/>s.
    /// </summary>
    /// <remarks>
    /// This component needs to be added alongside the <c>Canvas</c> component. Usually, raycasting is
    /// performed by the <c>GraphicRaycaster</c> component found there but for 3D raycasting necessary for
    /// tracked devices, this component is required.
    /// </remarks>
    [AddComponentMenu("Event/Tracked Device Raycaster")]
    [RequireComponent(typeof(Canvas))]
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

            public Graphic graphic { get; }
            public Vector3 worldHitPosition { get; }
            public Vector2 screenPosition { get; }
            public float distance { get; }
        }

        public override Camera eventCamera
        {
            get
            {
                var myCanvas = canvas;
                return myCanvas != null ? myCanvas.worldCamera : null;
            }
        }

        public LayerMask blockingMask
        {
            get => m_BlockingMask;
            set => m_BlockingMask = value;
        }

        public bool checkFor3DOcclusion
        {
            get => m_CheckFor3DOcclusion;
            set => m_CheckFor3DOcclusion = value;
        }

        public bool checkFor2DOcclusion
        {
            get => m_CheckFor2DOcclusion;
            set => m_CheckFor2DOcclusion = value;
        }

        public bool ignoreReversedGraphics
        {
            get => m_IgnoreReversedGraphics;
            set => m_IgnoreReversedGraphics = value;
        }

        public float maxDistance
        {
            get => m_MaxDistance;
            set => m_MaxDistance = value;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            s_Instances.AppendWithCapacity(this);
        }

        protected override void OnDisable()
        {
            var index = s_Instances.IndexOfReference(this);
            if (index != -1)
                s_Instances.RemoveAtByMovingTailWithCapacity(index);

            base.OnDisable();
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventData is ExtendedPointerEventData trackedEventData && trackedEventData.pointerType == UIPointerType.Tracked)
                PerformRaycast(trackedEventData, resultAppendList);
        }

        // Use this list on each raycast to avoid continually allocating.
        [NonSerialized]
        private List<RaycastHitData> m_RaycastResultsCache = new List<RaycastHitData>();

        internal void PerformRaycast(ExtendedPointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (canvas == null)
                return;

            if (eventCamera == null)
                return;

            var ray = new Ray(eventData.trackedDevicePosition, eventData.trackedDeviceOrientation * Vector3.forward);
            var hitDistance = m_MaxDistance;

            #if UNITY_INPUT_SYSTEM_ENABLE_PHYSICS
            if (m_CheckFor3DOcclusion)
            {
                var hits = Physics.RaycastAll(ray, hitDistance, m_BlockingMask);

                if (hits.Length > 0 && hits[0].distance < hitDistance)
                {
                    hitDistance = hits[0].distance;
                }
            }
            #endif

            #if UNITY_INPUT_SYSTEM_ENABLE_PHYSICS2D
            if (m_CheckFor2DOcclusion)
            {
                var raycastDistance = hitDistance;
                var hits = Physics2D.GetRayIntersectionAll(ray, raycastDistance, m_BlockingMask);

                if (hits.Length > 0 && hits[0].fraction * raycastDistance < hitDistance)
                {
                    hitDistance = hits[0].fraction * raycastDistance;
                }
            }
            #endif

            m_RaycastResultsCache.Clear();
            SortedRaycastGraphics(canvas, ray, m_RaycastResultsCache);

            // Now that we have a list of sorted hits, process any extra settings and filters.
            for (var i = 0; i < m_RaycastResultsCache.Count; i++)
            {
                var validHit = true;

                var hitData = m_RaycastResultsCache[i];

                var go = hitData.graphic.gameObject;
                if (m_IgnoreReversedGraphics)
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

                        worldPosition = hitData.worldHitPosition,
                        screenPosition = hitData.screenPosition,
                    };
                    resultAppendList.Add(castResult);
                }
            }
        }

        internal static InlinedArray<TrackedDeviceRaycaster> s_Instances;

        static readonly List<RaycastHitData> s_SortedGraphics = new List<RaycastHitData>();
        private void SortedRaycastGraphics(Canvas canvas, Ray ray, List<RaycastHitData> results)
        {
            var graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);

            s_SortedGraphics.Clear();
            for (var i = 0; i < graphics.Count; ++i)
            {
                var graphic = graphics[i];

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

        private static bool RayIntersectsRectTransform(RectTransform transform, Ray ray, out Vector3 worldPosition, out float distance)
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

        [FormerlySerializedAs("ignoreReversedGraphics")]
        [SerializeField]
        private bool m_IgnoreReversedGraphics;

        [FormerlySerializedAs("checkFor2DOcclusion")]
        [SerializeField]
        private bool m_CheckFor2DOcclusion;

        [FormerlySerializedAs("checkFor3DOcclusion")]
        [SerializeField]
        private bool m_CheckFor3DOcclusion;

        [Tooltip("Maximum distance (in 3D world space) that rays are traced to find a hit.")]
        [SerializeField] private float m_MaxDistance = 1000;

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
    }
}
#endif
