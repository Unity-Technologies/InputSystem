#if UNITY_EDITOR
using System.IO;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

////REVIEW: simply call ".input" instead of ".inputactions"?

namespace ISX.Editor
{
    // Imports an InputActionAsset from JSON.
    // Can generate code wrappers for the contained action sets as a convenience.
    // Will not overwrite existing wrappers except if the generate code actually differs.
    [ScriptedImporter(1, "inputactions")]
    public class InputActionImporter : ScriptedImporter
    {
        [SerializeField] internal bool m_GenerateWrapperCode;
        [SerializeField] internal string m_WrapperCodePath;
        ////TODO: code generator options

        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Parse JSON.
            var text = File.ReadAllText(ctx.assetPath);
            var sets = InputActionSet.FromJson(text);
            ////TODO: catch errors

            ////TODO: make sure set names are unique

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
        }
    }
}
#endif // UNITY_EDITOR
