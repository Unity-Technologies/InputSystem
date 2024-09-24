using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem.Experimental;

namespace UnityEditor.InputSystem.Experimental
{
    /// <summary>
    /// A set of extensions for types implementing <see cref="IObservableInput"/>.
    /// </summary>
    public static partial class ObservableInputEditorExtensions
    {
        /// <summary>
        /// Creates an asset from the given <paramref name="source"/> observable at the asset path given by
        /// <paramref name="path"/>. 
        /// </summary>
        /// <param name="source">The observable to be serialized to an asset.</param>
        /// <param name="path">The asset path (Must be inside "Assets" folder).</param>
        /// <typeparam name="T">The value type of the binding.</typeparam>
        /// <remarks>This method takes an interface which requires struct types to be boxed. The reason this
        /// extension isn't specialized is that boxing cannot be avoided to serialize the asset anyway.</remarks>
        public static void CreateAsset<T>([NotNull] this IObservableInput<T> source, [NotNull] string path, [NotNull] string name) 
            where T : struct
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            
            DoCreateAsset(ScriptableInputBinding.Create(source), path, name);
        }

        public static void CreateAssetFromName<T>([NotNull] this IObservableInput<T> source, string name) 
            where T : struct
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            CreateAsset(source, GetPathFromName(name), name);
        }

        private static void DoCreateAsset(UnityEngine.Object asset, string path, string name)
        {
            // TODO Figure out exactly the minimum needed
            asset.name = name;
            ProjectWindowUtil.CreateAsset(asset, path);

            /*var asset = ScriptableInputBinding.Create(source);
            asset.name = name;
            EditorUtility.SetDirty(asset);
            //EditorUtility.SetDirty(asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();*/
        }
        
        private static string GetPathFromName(string name) => GetSelectedPathOrFallback() + name + Resources.InputBindingAssetExtension;
        
        private static string GetSelectedPathOrFallback()
        {
            var path = "Assets";
            foreach (var obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if ( !string.IsNullOrEmpty(path) && System.IO.File.Exists(path) )
                {
                    path = System.IO.Path.GetDirectoryName(path);
                    break;
                }
            }
            return path + "/";
        }
    }
}