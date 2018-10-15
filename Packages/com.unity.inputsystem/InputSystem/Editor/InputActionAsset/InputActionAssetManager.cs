#if UNITY_EDITOR
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
        private string m_ImportedAssetJson;
        [SerializeField]
        private bool m_IsDirty;

        private SerializedObject m_SerializedObject;
        Action<bool> m_SetTitle;

        InputActionAsset importedAsset
        {
            get
            {
                if (m_ImportedAssetObject == null)
                {
                    LoadImportedObjectFromGuid();
                }
                return m_ImportedAssetObject;
            }
        }

        public InputActionAssetManager(InputActionAsset inputActionAsset)
        {
            m_ImportedAssetObject = inputActionAsset;
            m_AssetPath = AssetDatabase.GetAssetPath(importedAsset);
            m_AssetGUID = AssetDatabase.AssetPathToGUID(m_AssetPath);
        }

        public SerializedObject serializedObject
        {
            get { return m_SerializedObject; }
        }

        public bool dirty
        {
            get { return m_IsDirty; }
        }

        public void InitializeObjectReferences()
        {
            if (m_AssetObjectForEditing == null)
            {
                CreateWorkingCopyAsset();
            }
            m_SerializedObject = new SerializedObject(m_AssetObjectForEditing);
        }

        internal void CreateWorkingCopyAsset()
        {
            if (m_AssetObjectForEditing != null)
            {
                CleanupAssets();
            }
            // Duplicate the asset along 1:1. Unlike calling Clone(), this will also preserve
            // GUIDs.
            m_AssetObjectForEditing = Object.Instantiate(importedAsset);
            m_AssetObjectForEditing.hideFlags = HideFlags.HideAndDontSave;
            m_AssetObjectForEditing.name = importedAsset.name;
            m_SerializedObject = new SerializedObject(m_AssetObjectForEditing);
        }

        public void CleanupAssets()
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
                throw new Exception("Could not determine asset path for " + m_AssetGUID);

            m_ImportedAssetObject = AssetDatabase.LoadAssetAtPath<InputActionAsset>(m_AssetPath);
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
            m_ImportedAssetJson = asset.ToJson();

            // Write out, if changed.
            var existingJson = File.ReadAllText(m_AssetPath);
            if (m_ImportedAssetJson != existingJson)
            {
                File.WriteAllText(m_AssetPath, m_ImportedAssetJson);
                AssetDatabase.ImportAsset(m_AssetPath);
            }

            m_IsDirty = false;
            m_SetTitle(false);
        }

        public void SetAssetDirty()
        {
            m_IsDirty = true;
            m_SetTitle(true);
        }

        public bool ImportedAssetObjectEquals(InputActionAsset inputActionAsset)
        {
            if (importedAsset == null)
                return false;
            return importedAsset.Equals(inputActionAsset);
        }

        public void UpdateAssetDirtyState()
        {
            m_IsDirty = m_AssetObjectForEditing.ToJson() != importedAsset.ToJson();
            m_SetTitle(m_IsDirty);
        }

        public void SetReferences(Action<bool> setTitle)
        {
            m_SetTitle = setTitle;
        }
    }
}
#endif // UNITY_EDITOR
