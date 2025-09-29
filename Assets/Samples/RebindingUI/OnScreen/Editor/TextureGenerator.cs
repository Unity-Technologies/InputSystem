#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Edit-mode texture generation facilities to quickly get going without having to create textures when
    /// adding on-screen controls to a new project where it is desirable to have visibile virtual controls.
    /// </summary>
    internal static class TextureGenerator
    {
        #region Unity Editor Menu Extensions

        private const string AssetExtension = ".asset";
        private const string PngExtension = ".png";
        private const int Priority = 10;

        [Flags]
        private enum Direction
        {
            Up = 1,
            Down = 2,
            Left = 4,
            Right = 8,
        }

        [MenuItem("Assets/Create/Input System/On-Screen Button Texture", false, Priority)]
        private static void CreateOnScreenButtonTexture(MenuCommand menuCommand)
        {
            CreateAsset(GenerateButton(), PngExtension);
        }

        [MenuItem("Assets/Create/Input System/On-Screen Stick Rim Texture", false, Priority)]
        private static void CreateOnScreenStickRimTexture(MenuCommand menuCommand)
        {
            CreateAsset(GenerateStickRim(), PngExtension);
        }

        [MenuItem("Assets/Create/Input System/On-Screen Stick Knob Texture", false, Priority)]
        private static void CreateOnScreenStickKnobTexture(MenuCommand menuCommand)
        {
            CreateAsset(GenerateStickKnob(), PngExtension);
        }

        #endregion

        private static Texture2D CreateAsset(Texture2D texture, string extension)
        {
            Texture2D result = texture;

            // Get target path from Project window selection
            var path = GetSelectedPathOrFallback();
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, texture.name + extension));

            if (extension.Equals(PngExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                // Encode to PNG, refresh ADB and then import as texture asset
                File.WriteAllBytes(assetPath, texture.EncodeToPNG());
                AssetDatabase.Refresh();
                result = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
            else if (extension.Equals(AssetExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                // Save the asset
                AssetDatabase.CreateAsset(texture, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                throw new ArgumentOutOfRangeException("Unsupported extension: " + extension);
            }

            // Select it
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = result;

            // Destroy provided in-memory texture if we had to reimport it
            if (!ReferenceEquals(result, texture))
                Object.DestroyImmediate(texture);

            return result;
        }

        // Determines the path for where to create the asset.
        private static string GetSelectedPathOrFallback()
        {
            var path = "Assets";

            foreach (var obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                var selectedPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(selectedPath))
                    continue;

                if (Directory.Exists(selectedPath))
                {
                    path = selectedPath;
                    break;
                }

                path = Path.GetDirectoryName(selectedPath);
                break;
            }

            return path;
        }

        private static Texture2D GenerateButton(string name = "Button")
        {
            // Draw pixels
            var textureSize = 128;
            var padding = 2.0f;
            var pixels = new Pixels(textureSize, textureSize);
            pixels.Fill(new Color(0.0f, 0.0f, 0.0f, 0.0f));
            pixels.FillCircle(0.0f, textureSize / 2.0f - padding, new Gradient
            {
                colorKeys = new GradientColorKey[]
                {
                    new(Grayscale(1.0f), 0.0f),
                    new(Grayscale(0.9f), 0.8f),
                    new(Grayscale(1.0f), 0.9f),
                    new(Grayscale(0.5f), 1.0f)
                },
                mode = GradientMode.PerceptualBlend
            });

            return pixels.ToTexture(name);
        }

        private static Texture2D GenerateStickKnob(string name = "StickKnob", Direction direction =
                Direction.Up | Direction.Down | Direction.Left | Direction.Right)
        {
            // Draw pixels
            var textureSize = 128;
            var halfTextureSize = textureSize / 2.0f;
            var padding = 2.0f;
            var pixels = new Pixels(textureSize, textureSize);
            pixels.Fill(new Color(0.0f, 0.0f, 0.0f, 0.0f));
            pixels.FillCircle(0.0f, halfTextureSize - padding, new Gradient
            {
                colorKeys = new GradientColorKey[]
                {
                    new(Grayscale(0.9f), 0.0f),
                    new(Grayscale(0.8f), 0.8f),
                    new(Grayscale(0.9f), 0.9f),
                    new(Grayscale(0.4f), 1.0f),
                },
                mode = GradientMode.PerceptualBlend
            });
            pixels.FillCircle(0.0f, (halfTextureSize - padding) * 0.75f, new Gradient
            {
                colorKeys = new GradientColorKey[]
                {
                    new(Grayscale(1.0f), 0.0f),
                    new(Grayscale(0.9f), 0.5f),
                    new(Grayscale(0.7f), 1.0f)
                },
                mode = GradientMode.PerceptualBlend
            });

            var arrowOffset = halfTextureSize * 0.2f;
            var arrowLength = halfTextureSize * 0.5f;
            var arrowWidth = halfTextureSize * 0.3f;
            var arrowColor = Grayscale(0.5f);

            if (direction.HasFlag(Direction.Right))
            {
                pixels.FillArrow(
                    baseCenter: new Vector2(halfTextureSize + arrowOffset, halfTextureSize),
                    tip: new Vector2(halfTextureSize + arrowOffset + arrowOffset, halfTextureSize),
                    baseWidth: arrowWidth, color: arrowColor);
            }

            if (direction.HasFlag(Direction.Left))
            {
                pixels.FillArrow(
                    baseCenter: new Vector2(halfTextureSize - arrowOffset, halfTextureSize),
                    tip: new Vector2(halfTextureSize - arrowOffset - arrowOffset, halfTextureSize),
                    baseWidth: arrowWidth, color: arrowColor);
            }

            if (direction.HasFlag(Direction.Up))
            {
                pixels.FillArrow(
                    baseCenter: new Vector2(halfTextureSize, halfTextureSize + arrowOffset),
                    tip: new Vector2(halfTextureSize, halfTextureSize + arrowOffset + arrowOffset),
                    baseWidth: arrowWidth, color: arrowColor);
            }

            if (direction.HasFlag(Direction.Down))
            {
                pixels.FillArrow(
                    baseCenter: new Vector2(halfTextureSize, halfTextureSize - arrowOffset),
                    tip: new Vector2(halfTextureSize, halfTextureSize - arrowOffset - arrowOffset),
                    baseWidth: arrowWidth, color: arrowColor);
            }

            return pixels.ToTexture(name);
        }

        private static Texture2D GenerateStickRim(string name = "StickRim")
        {
            // Draw pixels
            var textureSize = 128;
            var padding = 2.0f;
            var radius = 10.0f;
            var pixels = new Pixels(textureSize, textureSize);
            pixels.Fill(new Color(0.0f, 0.0f, 0.0f, 0.0f));
            pixels.FillCircle(textureSize / 2.0f - padding - radius, textureSize / 2.0f - padding, new Gradient
            {
                colorKeys = new GradientColorKey[]
                {
                    new(Grayscale(0.5f), 0.0f),
                    new(Grayscale(1.0f), 0.5f),
                    new(Grayscale(0.5f), 1.0f)
                },
                mode = GradientMode.PerceptualBlend
            });

            return pixels.ToTexture(name);
        }

        private static Color Grayscale(float v, float alpha = 1.0f) => new Color(v, v, v, alpha);
    }
}

#endif // UNITY_EDITOR
