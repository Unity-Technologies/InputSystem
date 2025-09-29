#if UNITY_EDITOR

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    internal readonly struct Pixels
    {
        public Pixels(int width, int height)
        {
            Width = width;
            Height = height;
            Data = new Color[width * height];
        }

        public void Fill(Color color)
        {
            for (var i = 0; i < Data.Length; i++)
                Data[i] = color;
        }

        public void FillCircle(float innerRadius,
            float outerRadius, Gradient gradient)
        {
            var center = new Vector2(Width / 2f, Height / 2f);

            // Softness in pixels for anti-aliased edges
            const float edgeSoftness = 1.0f;

            for (var y = 0; y < Height; ++y)
            {
                for (var x = 0; x < Width; ++x)
                {
                    var pos = new Vector2(x + 0.5f, y + 0.5f); // pixel center
                    var dist = (pos - center).magnitude;

                    // Normalize distance between inner and outer radius
                    var t = Mathf.InverseLerp(innerRadius, outerRadius, dist);

                    // Gradient only valid between inner/outer radius ± softness
                    if (dist <= outerRadius + edgeSoftness && dist >= innerRadius - edgeSoftness)
                    {
                        var c = gradient.Evaluate(Mathf.Clamp01(t));
                        var alpha = 1f;

                        // Fade near outer edge
                        if (dist > outerRadius - edgeSoftness)
                            alpha = Mathf.Clamp01((outerRadius - dist) / edgeSoftness);

                        // Fade near inner edge (if > 0)
                        if (innerRadius > 0 && dist < innerRadius + edgeSoftness)
                            alpha = Mathf.Clamp01((dist - innerRadius) / edgeSoftness);

                        c.a *= alpha;
                        Data[y * Width + x] = c;
                    }
                }
            }
        }

        public void FillArrow(Vector2 tip, Vector2 baseCenter, float baseWidth, Color color)
        {
            var edgeSoftness = 1.0f; // pixels

            // Build triangle points
            var dir = (tip - baseCenter).normalized;
            var perp = new Vector2(-dir.y, dir.x);
            var baseLeft = baseCenter - perp * (baseWidth * 0.5f);
            var baseRight = baseCenter + perp * (baseWidth * 0.5f);

            var v0 = tip;
            var v1 = baseLeft;
            var v2 = baseRight;

            // Precompute edge vectors
            var e0 = v1 - v0;
            var e1 = v2 - v1;
            var e2 = v0 - v2;

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);

                    // Barycentric test (sign of cross products)
                    var c0 = Cross(e0, p - v0);
                    var c1 = Cross(e1, p - v1);
                    var c2 = Cross(e2, p - v2);

                    var inside = (c0 >= 0 && c1 >= 0 && c2 >= 0) ||
                        (c0 <= 0 && c1 <= 0 && c2 <= 0);

                    if (!inside)
                        continue;

                    // Distance to edges (for AA)
                    var d0 = DistanceToLine(p, v0, v1);
                    var d1 = DistanceToLine(p, v1, v2);
                    var d2 = DistanceToLine(p, v2, v0);
                    var minDist = Mathf.Min(d0, Mathf.Min(d1, d2));

                    var alpha = 1f;
                    if (minDist < edgeSoftness)
                        alpha = Mathf.Clamp01(minDist / edgeSoftness);

                    var c = color;
                    c.a *= alpha;

                    Data[y * Width + x] = c;
                }
            }
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static float DistanceToLine(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var ap = p - a;
            var t = Mathf.Clamp01(Vector2.Dot(ap, ab) / ab.sqrMagnitude);
            var proj = a + t * ab;
            return (p - proj).magnitude;
        }

        public Texture2D ToTexture(string name)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.alphaIsTransparency = true;
            texture.name = name;
            texture.SetPixels(Data);
            texture.Apply();
            return texture;
        }

        public readonly Color[] Data;
        public readonly int Width;
        public readonly int Height;
    }
}

#endif // UNITY_EDITOR
