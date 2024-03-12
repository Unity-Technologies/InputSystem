#if UNITY_EDITOR
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEditor;

////TODO: ensure that GUIDs in the asset are unique

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Keeps a reference to the asset being edited and maintains a copy of the asset object
    /// around for editing.
    /// </summary>
    [Serializable]
    internal class InputActionAssetManager : IDisposable
    {
        [SerializeField] private InputActionAsset m_AssetObjectForEditing;
        [SerializeField] private InputActionAsset m_ImportedAssetObject;
        [SerializeField] private string m_AssetGUID;
        [SerializeField] private string m_ImportedAssetJson;
        [SerializeField] private bool m_IsDirty;

        private SerializedObject m_SerializedObject;

        /// <summary>
        /// Returns the Asset GUID uniquely identifying the associated imported asset.
        /// </summary>
        public string guid => m_AssetGUID;

        /// <summary>
        /// Returns the current Asset Path for the associated imported asset.
        /// If the asset have been deleted this will be <c>null</c>.
        /// </summary>
        public string path
        {
            get
            {
                Debug.Assert(!string.IsNullOrEmpty(m_AssetGUID), "Asset GUID is empty");
                return AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            }
        }

        /// <summary>
        /// Returns the name of the associated imported asset.
        /// </summary>
        public string name
        {
            get
            {
                var asset = importedAsset;
                if (asset != null)
                    return asset.name;

                if (!string.IsNullOrEmpty(path))
                    return Path.GetFileNameWithoutExtension(path);

                return string.Empty;
            }
        }

        private InputActionAsset importedAsset
        {
            get
            {
                // Note that this may be null after deserialization from domain reload
                if (m_ImportedAssetObject == null)
                    LoadImportedObjectFromGuid();

                return m_ImportedAssetObject;
            }
        }

        public InputActionAsset editedAsset => m_AssetObjectForEditing; // TODO Remove if redundant

        public Action<bool> onDirtyChanged { get; set; }

        public InputActionAssetManager(InputActionAsset inputActionAsset)
        {
            if (inputActionAsset == null)
                throw new NullReferenceException(nameof(inputActionAsset));
            m_AssetGUID = EditorHelpers.GetAssetGUID(inputActionAsset);
            if (m_AssetGUID == null)
                throw new Exception($"Failed to get asset {inputActionAsset.name} GUID");

            m_ImportedAssetObject = inputActionAsset;

            Initialize();
        }

        public SerializedObject serializedObject => m_SerializedObject;

        public bool dirty => m_IsDirty;

        public bool Initialize()
        {
            if (m_AssetObjectForEditing == null)
            {
                if (importedAsset == null)
                {
                    // The asset we want to edit no longer exists.
                    return false;
                }

                CreateWorkingCopyAsset();
            }
            else
            {
                m_SerializedObject = new SerializedObject(m_AssetObjectForEditing);
            }

            return true;
        }

        public void Dispose()
        {
            if (m_SerializedObject == null)
                return;
            m_SerializedObject?.Dispose();
            m_SerializedObject = null;
        }

        public bool ReInitializeIfAssetHasChanged()
        {
            var json = importedAsset.ToJson();
            if (m_ImportedAssetJson == json)
                return false;

            CreateWorkingCopyAsset();
            return true;
        }

        public static InputActionAsset CreateWorkingCopy(InputActionAsset source)
        {
            var copy = Object.Instantiate(source);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.name = source.name;
            return copy;
        }

        public static void CreateWorkingCopyAsset(ref InputActionAsset copy, InputActionAsset source)
        {
            if (copy != null)
                Cleanup(ref copy);

            copy = CreateWorkingCopy(source);
        }

        private void CreateWorkingCopyAsset() // TODO Can likely be removed if combined with Initialize
        {
            if (m_AssetObjectForEditing != null)
                Cleanup();

            // Duplicate the asset along 1:1. Unlike calling Clone(), this will also preserve GUIDs.
            var asset = importedAsset;
            m_AssetObjectForEditing = CreateWorkingCopy(asset);
            m_ImportedAssetJson = asset.ToJson();
            m_SerializedObject = new SerializedObject(m_AssetObjectForEditing);
        }

        public void Cleanup()
        {
            Cleanup(ref m_AssetObjectForEditing);
        }

        public static void Cleanup(ref InputActionAsset asset)
        {
            if (asset == null)
                return;

            Object.DestroyImmediate(asset);
            asset = null;
        }

        private void LoadImportedObjectFromGuid()
        {
            // https://fogbugz.unity3d.com/f/cases/1313185/
            // InputActionEditorWindow being an EditorWindow, it will be saved as part of the editor's
            // window layout. When a project is opened that has no Library/ folder, the layout from the
            // most recently opened project is used. Which means that when opening an .inputactions
            // asset in project A, then closing it, and then opening project B, restoring the window layout
            // also tries to restore the InputActionEditorWindow having that very same asset open -- which
            // will lead nowhere except there happens to be an InputActionAsset with the very same GUID in
            // the project.
            var assetPath = path;
            if (!string.IsNullOrEmpty(assetPath))
                m_ImportedAssetObject = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
        }

        public void ApplyChanges()
        {
            m_SerializedObject.ApplyModifiedProperties();
            m_SerializedObject.Update();
        }

        internal void SaveChangesToAsset()
        {
            // If this is invoked after a domain reload, importAsset will resolve itself.
            // However, if the asset do not exist importedAsset will be null and we cannot complete the operation.
            if (importedAsset == null)
                throw new Exception("Unable to save changes. Associated asset does not exist.");

            SaveAsset(path, m_AssetObjectForEditing.ToJson());
            SetDirty(false);
        }

        /// <summary>
        /// Saves an asset to the given <c>assetPath</c> with file content corresponding to <c>assetJson</c>
        /// if the current content of the asset given by <c>assetPath</c> is different or the asset do not exist.
        /// </summary>
        /// <param name="assetPath">Destination asset path.</param>
        /// <param name="assetJson">The JSON file content to be written to the asset.</param>
        /// <returns><c>true</c> if the asset was successfully modified or created, else <c>false</c>.</returns>
        internal static bool SaveAsset(string assetPath, string assetJson)
        {
            var existingJson = File.Exists(assetPath) ? File.ReadAllText(assetPath) : string.Empty;

            // Return immediately if file content has not changed, i.e. touching the file would not yield a difference.
            if (assetJson == existingJson)
                return false;

            // Attempt to write asset to disc (including checkout the file) and inform the user if this fails.
            if (EditorHelpers.WriteAsset(assetPath, assetJson))
                return true;

            Debug.LogError($"Unable save asset to \"{assetPath}\" since the asset-path could not be checked-out as editable in the underlying version-control system.");
            return false;
        }

        public void MarkDirty()
        {
            SetDirty(true);
        }

        public void UpdateAssetDirtyState()
        {
            m_SerializedObject.Update();
            SetDirty(m_AssetObjectForEditing.ToJson() != importedAsset.ToJson()); // TODO Why not using cached version?
        }

        private void SetDirty(bool newValue)
        {
            m_IsDirty = newValue;
            if (onDirtyChanged != null)
                onDirtyChanged(newValue);
        }
    }
}
#endif // UNITY_EDITOR
