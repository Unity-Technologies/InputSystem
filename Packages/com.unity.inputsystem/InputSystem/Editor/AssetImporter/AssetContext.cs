#if UNITY_EDITOR

using System;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Represents an asset import or creation context.
    /// </summary>
    internal interface IAssetContext
    {
        /// <summary>
        /// The associated asset destination path of the main object.
        /// </summary>
        string assetPath { get; }

        /// <summary>
        /// The associated source path.
        /// </summary>
        string sourcePath { get; }

        /// <summary>
        /// Adds an object to the asset.
        /// </summary>
        /// <param name="identifier">Object identifier</param>
        /// <param name="obj">The object to be added.</param>
        /// <param name="icon">Optional icon</param>
        void AddObjectToAsset(string identifier, Object obj, Texture2D icon = null);

        /// <summary>
        /// Sets the main object of the asset.
        /// </summary>
        /// <param name="obj">The main object of the asset</param>
        void SetMainObject(Object obj);

        /// <summary>
        /// Logs an error associated with the importing or creation of the asset.
        /// </summary>
        /// <param name="message">An error message. Never null.</param>
        void LogError(string message);
    }

    struct AssetImporterAssetContext : IAssetContext
    {
        private readonly AssetImportContext m_Context;

        public AssetImporterAssetContext(AssetImportContext context)
        {
            m_Context = context;
        }

        public string assetPath => m_Context.assetPath;
        public string sourcePath => m_Context.assetPath;

        public void AddObjectToAsset(string identifier, Object subAsset, Texture2D icon)
        {
            m_Context.AddObjectToAsset(identifier, subAsset, icon);
        }

        public void SetMainObject(Object obj)
        {
            // Note that importer doesn't need to do any storage
            m_Context.SetMainObject(obj);
        }

        public void LogError(string message)
        {
            m_Context.LogImportError(message);
        }
    }

    struct AssetDatabaseAssetContext : IAssetContext
    {
        private readonly string m_AssetPath;
        private readonly string m_SourcePath;

        public AssetDatabaseAssetContext(string assetPath, string sourcePath)
        {
            m_AssetPath = assetPath ?? throw new ArgumentNullException(nameof(assetPath));
            m_SourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
        }

        public string assetPath => m_AssetPath;
        public string sourcePath => m_SourcePath;

        public void AddObjectToAsset(string identifier, Object subAsset, Texture2D icon)
        {
            AssetDatabase.AddObjectToAsset(subAsset, m_AssetPath);
        }

        public void SetMainObject(Object obj)
        {
            AssetDatabase.AddObjectToAsset(obj, assetPath); // Note that adding is necessary here compared to importer
            AssetDatabase.SetMainObject(obj, m_AssetPath);
        }

        public void LogError(string message)
        {
            Debug.LogError(message);
        }
    }
}

#endif // UNITY_EDITOR
