#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace ISX.Editor
{
    // Imports an InputActionAsset from JSON.
    // Can generate code wrappers for the contained action sets as a convenience.
    // Will not overwrite existing wrappers except if the generated code actually differs.
    [ScriptedImporter(1, InputActionAsset.kExtension)]
    public class InputActionImporter : ScriptedImporter
    {
        [SerializeField] internal bool m_GenerateWrapperCode;
        [SerializeField] internal string m_WrapperCodePath;
        [SerializeField] internal string m_WrapperCodeNamespace;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Parse JSON.
            var text = File.ReadAllText(ctx.assetPath);
            var sets = InputActionSet.FromJson(text);
            ////TODO: catch errors

            ////TODO: make sure action names are unique

            // Create asset.
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.m_ActionSets = sets;
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
                    actionObject.m_SetName = set.name;
                    actionObject.m_ActionName = action.name;

                    var objectName = action.name;
                    if (haveSetName)
                        objectName = $"{set.name}/{action.name}";

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
                    className = Path.GetFileNameWithoutExtension(ctx.assetPath)
                };

                if (InputActionCodeGenerator.GenerateWrapperCode(wrapperFilePath, sets, options))
                {
                    // Inform database that we modified a source asset *during* import.
                    AssetDatabase.ImportAsset(wrapperFilePath);
                }
            }
        }
    }
}
#endif // UNITY_EDITOR
