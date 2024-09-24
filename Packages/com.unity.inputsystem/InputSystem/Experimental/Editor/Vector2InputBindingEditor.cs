using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;

namespace UnityEditor.InputSystem.Experimental
{
    // TODO Related:
    // https://discussions.unity.com/t/how-to-use-renderstaticpreview-with-a-user-defined-property/143371/2
    // https://docs.unity3d.com/ScriptReference/Editor.RenderStaticPreview.html
    
    [CustomEditor(typeof(Vector2InputBinding))]
    public class Vector2InputBindingEditor : UnityEditor.Editor
    {
        //private Vector2InputBinding item => target as Vector2InputBinding;
        
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var item = target as Vector2InputBinding;
            if (item != null)
            {
                var icon = Resources.LoadIcon(Resources.Icon.Action);
                if (icon != null)
                {
                    var tex = new Texture2D (width, height);
                    EditorUtility.CopySerialized (icon, tex);
                    return tex;    
                }
            }
            
            return base.RenderStaticPreview(assetPath, subAssets, width, height);
        }
    }
}