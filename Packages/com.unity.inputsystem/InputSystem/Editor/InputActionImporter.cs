#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor
{
    /// <summary>
    /// Imports an <see cref="InputActionAsset"/> from JSON.
    /// </summary>
    /// <remarks>
    /// Can generate code wrappers for the contained action sets as a convenience.
    /// Will not overwrite existing wrappers except if the generated code actually differs.
    /// </remarks>
    [ScriptedImporter(kVersion, InputActionAsset.kExtension)]
    public class InputActionImporter : ScriptedImporter
    {
        private const int kVersion = 4;

        [SerializeField] internal bool m_GenerateWrapperCode;
        [SerializeField] internal string m_WrapperCodePath;
        [SerializeField] internal string m_WrapperClassName;
        [SerializeField] internal string m_WrapperCodeNamespace;
        [SerializeField] internal bool m_GenerateActionEvents;

        // Actions and maps coming in from JSON may not have IDs assigned to them. However,
        // once imported, we want them to have stable IDs. So we do the same thing that Unity's
        // model importer does and remember the GUID<->name correlations used in the file.
        [SerializeField] internal RememberedGuid[] m_ActionGuids;
        [SerializeField] internal RememberedGuid[] m_ActionMapGuids;

        [Serializable]
        internal struct RememberedGuid
        {
            public string name;
            public string guid;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            ////REVIEW: need to check with version control here?
            // Read file.
            string text;
            try
            {
                text = File.ReadAllText(ctx.assetPath);
            }
            catch (Exception exception)
            {
                ctx.LogImportError(string.Format("Could read file '{0}' ({1})",
                    ctx.assetPath, exception));
                return;
            }

            // Create asset.
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();

            // Parse JSON.
            try
            {
                ////TODO: make sure action names are unique
                asset.LoadFromJson(text);
            }
            catch (Exception exception)
            {
                ctx.LogImportError(string.Format("Could not parse input actions in JSON format from '{0}' ({1})",
                    ctx.assetPath, exception));
                DestroyImmediate(asset);
                return;
            }

            ctx.AddObjectToAsset("<root>", asset);
            ctx.SetMainObject(asset);

            // Make sure every map and every action has a stable ID assigned to it.
            var maps = asset.actionMaps;
            foreach (var map in maps)
            {
                if (map.idDontGenerate == Guid.Empty)
                {
                    // Generate and remember GUID.
                    var id = map.id;
                    ArrayHelpers.Append(ref m_ActionMapGuids, new RememberedGuid
                    {
                        guid = id.ToString(),
                        name = map.name,
                    });
                }
                else
                {
                    // Retrieve remembered GUIDs.
                    if (m_ActionMapGuids != null)
                    {
                        for (var i = 0; i < m_ActionMapGuids.Length; ++i)
                        {
                            if (string.Compare(m_ActionMapGuids[i].name, map.name,
                                StringComparison.InvariantCultureIgnoreCase) == 0)
                            {
                                map.m_Guid = Guid.Empty;
                                map.m_Id = m_ActionMapGuids[i].guid;
                                break;
                            }
                        }
                    }
                }

                foreach (var action in map.actions)
                {
                    var actionName = string.Format("{0}/{1}", map.name, action.name);
                    if (action.idDontGenerate == Guid.Empty)
                    {
                        // Generate and remember GUID.
                        var id = action.id;
                        ArrayHelpers.Append(ref m_ActionGuids, new RememberedGuid
                        {
                            guid = id.ToString(),
                            name = actionName,
                        });
                    }
                    else
                    {
                        // Retrieve remembered GUIDs.
                        if (m_ActionGuids != null)
                        {
                            for (var i = 0; i < m_ActionGuids.Length; ++i)
                            {
                                if (string.Compare(m_ActionGuids[i].name, actionName,
                                    StringComparison.InvariantCultureIgnoreCase) == 0)
                                {
                                    action.m_Guid = Guid.Empty;
                                    action.m_Id = m_ActionGuids[i].guid;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Create subasset for each action.
            foreach (var map in maps)
            {
                var haveSetName = !string.IsNullOrEmpty(map.name);

                foreach (var action in map.actions)
                {
                    var actionReference = ScriptableObject.CreateInstance<InputActionReference>();
                    actionReference.Set(action);

                    var objectName = action.name;
                    if (haveSetName)
                        objectName = string.Format("{0}/{1}", map.name, action.name);

                    actionReference.name = objectName;
                    ctx.AddObjectToAsset(objectName, actionReference);
                }
            }

            // Generate wrapper code, if enabled.
            if (m_GenerateWrapperCode)
            {
                var wrapperFilePath = m_WrapperCodePath;
                if (string.IsNullOrEmpty(wrapperFilePath))
                {
                    var assetPath = ctx.assetPath;
                    var directory = Path.GetDirectoryName(assetPath);
                    var fileName = Path.GetFileNameWithoutExtension(assetPath);
                    wrapperFilePath = Path.Combine(directory, fileName) + ".cs";
                }

                var options = new InputActionCodeGenerator.Options
                {
                    sourceAssetPath = ctx.assetPath,
                    namespaceName = m_WrapperCodeNamespace,
                    className = m_WrapperClassName,
                    generateEvents = m_GenerateActionEvents,
                };

                if (InputActionCodeGenerator.GenerateWrapperCode(wrapperFilePath, maps, asset.controlSchemes, options))
                {
                    // Inform database that we modified a source asset *during* import.
                    AssetDatabase.ImportAsset(wrapperFilePath);
                }
            }

            // Refresh editors.
            AssetInspectorWindow.RefreshAllOnAssetReimport();
        }

        ////REVIEW: actually pre-populate with some stuff?
        private const string kDefaultAssetLayout = "{}";

        // Add item to plop an .inputactions asset into the project.
        [MenuItem("Assets/Create/Input Actions")]
        public static void CreateInputAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent("New Controls." + InputActionAsset.kExtension,
                kDefaultAssetLayout);
        }
    }
}
#endif // UNITY_EDITOR
