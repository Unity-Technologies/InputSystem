#if UNITY_EDITOR
using System.IO;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal static class GUIHelpers
    {
        public static class Styles
        {
            public static GUIStyle lineSeparator = new GUIStyle();

            static Styles()
            {
                lineSeparator.fixedHeight = 1;
                lineSeparator.margin.bottom = 2;
                lineSeparator.margin.top = 2;
            }
        }

        private const string kIconPath = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/";

        public static void DrawLineSeparator(string label = null)
        {
            var hasLabel = !string.IsNullOrEmpty(label);
            EditorGUILayout.BeginVertical();
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.lineSeparator, GUILayout.ExpandWidth(true));
            var labelRect = new Rect();
            GUIContent labelContent = null;
            if (hasLabel)
            {
                labelContent = new GUIContent(label);
                labelRect = GUILayoutUtility.GetRect(labelContent, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            }
            EditorGUILayout.EndVertical();

            if (Event.current.type != EventType.Repaint)
                return;

            var orgColor = GUI.color;
            var tintColor = EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f);
            GUI.color = GUI.color * tintColor;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = orgColor;

            if (hasLabel)
                EditorGUI.LabelField(labelRect, labelContent, EditorStyles.miniLabel);
        }

        public static Texture2D LoadIcon(string name)
        {
            var skinPrefix = EditorGUIUtility.isProSkin ? "d_" : "";
            var scale = Mathf.Clamp((int)EditorGUIUtility.pixelsPerPoint, 0, 4);
            var scalePostFix = scale > 1 ? $"@{scale}x" : "";
            var path = Path.Combine(kIconPath, skinPrefix + name + scalePostFix + ".png");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
#endif // UNITY_EDITOR
