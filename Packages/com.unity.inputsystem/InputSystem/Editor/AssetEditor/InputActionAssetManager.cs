#if UNITY_EDITOR
using System;
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
        [SerializeField] internal InputActionAsset m_AssetObjectForEditing;
        [SerializeField] private InputActionAsset m_ImportedAssetObject;
        [SerializeField] private string m_AssetGUID;
        [SerializeField] private string m_ImportedAssetJson;
        [SerializeField] private bool m_IsDirty;

        private SerializedObject m_SerializedObject;

        public string guid => m_AssetGUID;

        public string path
        {
            get
            {
                Debug.Assert(!string.IsNullOrEmpty(m_AssetGUID), "Asset GUID is empty");
                return AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            }
        }

        public string name
        {
            get
            {
                if (m_ImportedAssetObject != null)
                    return m_ImportedAssetObject.name;

                if (!string.IsNullOrEmpty(path))
                    return Path.GetFileNameWithoutExtension(path);

                return string.Empty;
            }
        }

        private InputActionAsset importedAsset
        {
            get
            {
                if (m_ImportedAssetObject == null)
                    LoadImportedObjectFromGuid();

                return m_ImportedAssetObject;
            }
        }

        public Action<bool> onDirtyChanged { get; set; }

        public InputActionAssetManager(InputActionAsset inputActionAsset)
        {
            m_ImportedAssetObject = inputActionAsset;
            bool isGUIDObtained = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(importedAsset, out m_AssetGUID, out long _);
            Debug.Assert(isGUIDObtained, $"Failed to get asset {inputActionAsset.name} GUID");
        }

        public SerializedObject serializedObject => m_SerializedObject;

        public bool dirty => m_IsDirty;

        public bool Initialize()
        {
            if (m_AssetObjectForEditing == null)
            {
                if (importedAsset == null)
                    // The asset we want to edit no longer exists.
                    return false;

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
            m_SerializedObject?.Dispose();
        }

        public bool ReInitializeIfAssetHasChanged()
        {
            var asset = importedAsset;
            var json = asset.ToJson();
            if (m_ImportedAssetJson == json)
                return false;

            CreateWorkingCopyAsset();
            return true;
        }

        private void CreateWorkingCopyAsset()
        {
            if (m_AssetObjectForEditing != null)
                Cleanup();

            // Duplicate the asset along 1:1. Unlike calling Clone(), this will also preserve
            // GUIDs.
            var asset = importedAsset;
            m_AssetObjectForEditing = Object.Instantiate(asset);
            m_AssetObjectForEditing.hideFlags = HideFlags.HideAndDontSave;
            m_AssetObjectForEditing.name = importedAsset.name;
            m_ImportedAssetJson = asset.ToJson();
            m_SerializedObject = new SerializedObject(m_AssetObjectForEditing);
        }

        public void Cleanup()
        {
            if (m_AssetObjectForEditing == null)
                return;

            Object.DestroyImmediate(m_AssetObjectForEditing);
            m_AssetObjectForEditing = null;
        }

        public void LoadImportedObjectFromGuid()
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
            Debug.Assert(importedAsset != null);

            // Update JSON.
            var asset = m_AssetObjectForEditing;
            m_ImportedAssetJson = asset.ToJson();

            // Write out, if changed.
            var assetPath = path;
            var existingJson = File.ReadAllText(assetPath);
            if (m_ImportedAssetJson != existingJson)
            {
                EditorHelpers.CheckOut(assetPath);
                File.WriteAllText(assetPath, m_ImportedAssetJson);
                AssetDatabase.ImportAsset(assetPath);
            }

            m_IsDirty = false;
            onDirtyChanged(false);
        }

        public void SetAssetDirty()
        {
            m_IsDirty = true;
            onDirtyChanged(true);
        }

        public bool ImportedAssetObjectEquals(InputActionAsset inputActionAsset)
        {
            if (importedAsset == null)
                return false;
            return importedAsset.Equals(inputActionAsset);
        }

        public void UpdateAssetDirtyState()
        {
            m_SerializedObject.Update();
            m_IsDirty = m_AssetObjectForEditing.ToJson() != importedAsset.ToJson();
            onDirtyChanged(m_IsDirty);
        }
    }
}
#endif // UNITY_EDITOR
