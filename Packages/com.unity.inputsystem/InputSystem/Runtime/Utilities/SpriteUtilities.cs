using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.InputSystem.Utilities
{
    internal static class SpriteUtilities
    {
        public static unsafe Sprite CreateCircleSprite(int radius, Color32 colour)
        {
            // cache the diameter
            var d = radius * 2;

            var texture = new Texture2D(d, d, DefaultFormat.LDR, TextureCreationFlags.None);
            var colours = texture.GetRawTextureData<Color32>();
            var coloursPtr = (Color32*)colours.GetUnsafePtr();
            UnsafeUtility.MemSet(coloursPtr, 0, colours.Length * UnsafeUtility.SizeOf<Color32>());

            // pack the colour into a ulong so we can write two pixels at a time to the texture data
            var colorPtr = (uint*)UnsafeUtility.AddressOf(ref colour);
            var colourAsULong = *(ulong*)colorPtr << 32 | *colorPtr;

            float rSquared = radius * radius;

            // loop over the texture memory one column at a time filling in a line between the two x coordinates
            // of the circle at each column
            for (var y = -radius; y < radius; y++)
            {
                // for the current column, calculate what the x coordinate of the circle would be
                // using x^2 + y^2 = r^2, or x^2 = r^2 - y^2. The square root of the value of the
                // x coordinate will equal half the width of the circle at the current y coordinate
                var halfWidth = (int)Mathf.Sqrt(rSquared - y * y);

                // position the pointer so it points at the memory where we should start filling in
                // the current line
                var ptr = coloursPtr
                    + (y + radius) * d  // the position of the memory at the start of the row at the current y coordinate
                    + radius - halfWidth;   // the position along the row where we should start inserting colours

                // fill in two pixels at a time
                for (var x = 0; x < halfWidth; x++)
                {
                    *(ulong*)ptr = colourAsULong;
                    ptr += 2;
                }
            }

            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, d, d), new Vector2(radius, radius), 1, 0, SpriteMeshType.FullRect);
            return sprite;
        }
    }
}
