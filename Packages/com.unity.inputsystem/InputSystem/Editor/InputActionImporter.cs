#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEngine.Experimental.Input.Editor
{
    // Imports an InputActionAsset from JSON.
    // Can generate code wrappers for the contained action sets as a convenience.
    // Will not overwrite existing wrappers except if the generated code actually differs.
    [ScriptedImporter(kVersion, InputActionAsset.kExtension)]
    public class InputActionImporter : ScriptedImporter
    {
        private const int kVersion = 2;

        [SerializeField] internal bool m_GenerateWrapperCode;
        [SerializeField] internal string m_WrapperCodePath;
        [SerializeField] internal string m_WrapperClassName;
        [SerializeField] internal string m_WrapperCodeNamespace;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Parse JSON.
            var text = File.ReadAllText(ctx.assetPath);
            var sets = InputActionMap.FromJson(text);
            ////TODO: catch errors

            ////TODO: make sure action names are unique

            // Create asset.
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.m_ActionMaps = sets;
            ctx.AddObjectToAsset("<root>", asset);
            ctx.SetMainObject(asset);

            // Create subasset for each action.
            for (var i = 0; i < sets.Length; ++i)
            {
                var set = sets[i];
                var haveSetName = !string.IsNullOrEmpty(set.name);

                foreach (var action in set.actions)
                {
                    var actionObject = ScriptableObject.CreateInstance<InputActionReference>();

                    actionObject.m_Asset = asset;
                    actionObject.m_MapName = set.name;
                    actionObject.m_ActionName = action.name;

                    var objectName = action.name;
                    if (haveSetName)
                        objectName = string.Format("{0}/{1}", set.name, action.name);

                    actionObject.name = objectName;
                    ctx.AddObjectToAsset(objectName, actionObject);
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
                    className = m_WrapperClassName
                };

                if (InputActionCodeGenerator.GenerateWrapperCode(wrapperFilePath, sets, options))
                {
                    // Inform database that we modified a source asset *during* import.
                    AssetDatabase.ImportAsset(wrapperFilePath);
                }
            }
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
