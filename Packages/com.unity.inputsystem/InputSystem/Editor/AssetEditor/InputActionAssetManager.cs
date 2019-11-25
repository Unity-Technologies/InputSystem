#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

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
        [SerializeField] private string m_AssetPath;
        [SerializeField] private string m_ImportedAssetJson;
        [SerializeField] private bool m_IsDirty;

        private SerializedObject m_SerializedObject;

        public string guid => m_AssetGUID;

        public string path { get => m_AssetPath; set => m_AssetPath = value; }

        public string name
        {
            get
            {
                if (m_ImportedAssetObject != null)
                    return m_ImportedAssetObject.name;

                if (!string.IsNullOrEmpty(m_AssetPath))
                    return Path.GetFileNameWithoutExtension(m_AssetPath);

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
            m_AssetPath = AssetDatabase.GetAssetPath(importedAsset);
            m_AssetGUID = AssetDatabase.AssetPathToGUID(m_AssetPath);
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
            Debug.Assert(!string.IsNullOrEmpty(m_AssetGUID));

            m_AssetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            if (string.IsNullOrEmpty(m_AssetPath))
                throw new InvalidOperationException("Could not determine asset path for " + m_AssetGUID);

            m_ImportedAssetObject = AssetDatabase.LoadAssetAtPath<InputActionAsset>(m_AssetPath);
        }

        public void ApplyChanges()
        {
            m_SerializedObject.ApplyModifiedProperties();
            m_SerializedObject.Update();
        }

        internal void SaveChangesToAsset()
        {
            Debug.Assert(!string.IsNullOrEmpty(m_AssetPath));

            // Update JSON.
            var asset = m_AssetObjectForEditing;
            m_ImportedAssetJson = asset.ToJson();

            // Write out, if changed.
            var existingJson = File.ReadAllText(m_AssetPath);
            if (m_ImportedAssetJson != existingJson)
            {
                ////TODO: has to be made to work with version control
                File.WriteAllText(m_AssetPath, m_ImportedAssetJson);
                AssetDatabase.ImportAsset(m_AssetPath);
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
