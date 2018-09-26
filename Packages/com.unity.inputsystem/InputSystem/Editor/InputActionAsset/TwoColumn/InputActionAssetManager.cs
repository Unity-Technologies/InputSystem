using System;
using System.IO;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    [Serializable]
    public class InputActionAssetManager
    {
        [SerializeField]
        internal InputActionAsset m_AssetObjectForEditing;
        [SerializeField]
        private InputActionAsset m_ImportedAssetObject;
        [SerializeField]
        private string m_AssetGUID;
        [SerializeField]
        private string m_AssetPath;
        [SerializeField]
        private string m_AssetJson;
        [SerializeField]
        private bool m_IsDirty;

        private SerializedObject m_SerializedObject;

        public InputActionAssetManager(InputActionAsset inputActionAsset)
        {
            m_ImportedAssetObject = inputActionAsset;
        }

        public SerializedObject serializedObject
        {
            get { return m_SerializedObject; }
        }

        public bool dirty
        {
            get { return m_IsDirty; }
        }

        public bool IsAssetImportedAssetSet()
        {
            return m_ImportedAssetObject != null;
        }

        public void InitializeObjectReferences()
        {
            // If we have an asset object, grab its path and GUID.
            if (m_ImportedAssetObject != null)
            {
                m_AssetPath = AssetDatabase.GetAssetPath(m_ImportedAssetObject);
                m_AssetGUID = AssetDatabase.AssetPathToGUID(m_AssetPath);
            }
            else
            {
                // Otherwise look it up from its GUID. We're not relying on just
                // the path here as the asset may have been moved.
                InitializeReferenceToImportedAssetObject();
            }

            if (m_AssetObjectForEditing == null)
            {
                // Duplicate the asset along 1:1. Unlike calling Clone(), this will also preserve
                // GUIDs.
                m_AssetObjectForEditing = Object.Instantiate(m_ImportedAssetObject);
                m_AssetObjectForEditing.hideFlags = HideFlags.HideAndDontSave;
                m_AssetObjectForEditing.name = m_ImportedAssetObject.name;
            }

            m_AssetJson = null;
            m_SerializedObject = new SerializedObject(m_AssetObjectForEditing);
        }

        private void InitializeReferenceToImportedAssetObject()
        {
            LoadImportedObjectFromGuid();
            if (m_AssetObjectForEditing != null)
            {
                Object.DestroyImmediate(m_AssetObjectForEditing);
                m_AssetObjectForEditing = null;
            }
        }

        public void LoadImportedObjectFromGuid()
        {
            Debug.Assert(!string.IsNullOrEmpty(m_AssetGUID));

            m_AssetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            if (string.IsNullOrEmpty(m_AssetPath))
                throw new Exception("Could not determine asset path for " + m_AssetGUID);

            m_ImportedAssetObject = AssetDatabase.LoadAssetAtPath<InputActionAsset>(m_AssetPath);
        }

        public bool IsEditingAssetDifferent()
        {
            return m_AssetObjectForEditing.ToJson() != m_ImportedAssetObject.ToJson();
        }

        public void ApplyChanges()
        {
            m_SerializedObject.ApplyModifiedProperties();
            m_SerializedObject.Update();
        }

        internal void SaveChangesToAsset()
        {
            ////TODO: has to be made to work with version control
            Debug.Assert(!string.IsNullOrEmpty(m_AssetPath));

            // Update JSON.
            var asset = m_AssetObjectForEditing;
            m_AssetJson = asset.ToJson();

            // Write out, if changed.
            var existingJson = File.ReadAllText(m_AssetPath);
            if (m_AssetJson != existingJson)
            {
                File.WriteAllText(m_AssetPath, m_AssetJson);
                AssetDatabase.ImportAsset(m_AssetPath);
            }

            m_IsDirty = false;
        }

        public void SetAssetDirty()
        {
            m_IsDirty = true;
        }

        public bool ImportedAssetObjectEquals(InputActionAsset inputActionAsset)
        {
            return m_ImportedAssetObject.Equals(inputActionAsset);
        }

        public bool IsAssetReferenceValid()
        {
            return m_ImportedAssetObject == null;
        }

        public bool IsEditedAssetDifferent()
        {
            m_AssetJson = m_ImportedAssetObject.ToJson();
            return m_AssetObjectForEditing.ToJson() != m_AssetJson;
        }
    }
}
