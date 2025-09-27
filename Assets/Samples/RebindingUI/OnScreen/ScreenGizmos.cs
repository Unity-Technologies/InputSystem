using UnityEngine;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    public static class ScreenGizmos
    {
        public static void DrawGizmoCircle(Vector2 center, float radius)
        {
            for (var i = 0; i < 32; i++)
            {
                var radians = i / 32f * Mathf.PI * 2;
                var nextRadian = (i + 1) / 32f * Mathf.PI * 2;
                Gizmos.DrawLine(
                    new Vector3(center.x + Mathf.Cos(radians) * radius, center.y + Mathf.Sin(radians) * radius, 0),
                    new Vector3(center.x + Mathf.Cos(nextRadian) * radius, center.y + Mathf.Sin(nextRadian) * radius, 0));
            }
        }

        public static void DrawLine(
            Camera camera,
            Vector3 startPixelPos,
            Vector3 endPixelPos)
        {
            if (camera == null)
                return;
            var startWorld = PixelToCameraClipPlane(camera, startPixelPos);
            var endWorld = PixelToCameraClipPlane(camera, endPixelPos);
            Gizmos.DrawLine(startWorld, endWorld);
        }

        public static void DrawLine(
            Canvas canvas,
            Camera camera,
            Vector3 startPixelPos,
            Vector3 endPixelPos)
        {
            if (camera == null || canvas == null)
                return;
            var startWorld = PixelToCameraClipPlane(camera, startPixelPos * canvas.scaleFactor);
            var endWorld = PixelToCameraClipPlane(camera, endPixelPos * canvas.scaleFactor);
            Gizmos.DrawLine(startWorld, endWorld);
        }

        /// <summary>
        /// Converts the <paramref name="screenPos"/> to world space
        /// near the <paramref name="camera"/> near clip plane. The
        /// z component of the <paramref name="screenPos"/>
        /// will be overriden.
        /// </summary>
        private static Vector3 PixelToCameraClipPlane(
            Camera camera,
            Vector3 screenPos)
        {
            // The z-position defines the distance to the camera
            // when using Camera.ScreenToWorldPoint.
            screenPos.z = camera.nearClipPlane + 0.001f;
            return camera.ScreenToWorldPoint(screenPos);
        }
    }
}
