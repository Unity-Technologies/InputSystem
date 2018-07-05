#if UNITY_EDITOR
using System;
using System.Linq;

namespace UnityEngine.Experimental.Input.Editor
{
    static class StyleHelpers
    {
        public static Texture2D CreateTextureWithBorder(Color color)
        {
            return CreateTextureWithBorder(color, color);
        }

        public static Texture2D CreateTextureWithBorder(Color innerColor, Color borderColor)
        {
            var txtId = "ISX " + innerColor + " " + borderColor;
            var objs = Resources.FindObjectsOfTypeAll<Texture2D>().Where(t => t.name == txtId);
            if (objs.Any())
                return objs.First();

            var texture = new Texture2D(5, 5);
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    texture.SetPixel(i, j, borderColor);
                }
            }

            for (int i = 1; i < 4; i++)
            {
                for (int j = 1; j < 4; j++)
                {
                    texture.SetPixel(i, j, innerColor);
                }
            }
            texture.filterMode = FilterMode.Point;
            texture.name = txtId;
            texture.hideFlags |= HideFlags.DontSaveInEditor;
            texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            texture.Apply();
            return texture;
        }
    }
}
#endif // UNITY_EDITOR
