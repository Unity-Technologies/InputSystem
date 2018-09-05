#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

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
        private const int kVersion = 3;

        [SerializeField] internal bool m_GenerateWrapperCode;
        [SerializeField] internal string m_WrapperCodePath;
        [SerializeField] internal string m_WrapperClassName;
        [SerializeField] internal string m_WrapperCodeNamespace;

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

            // Parse JSON.
            InputActionMap[] maps;
            try
            {
                maps = InputActionMap.FromJson(text);
            }
            catch (Exception exception)
            {
                ctx.LogImportError(string.Format("Could not parse input actions in JSON format from '{0}' ({1})",
                    ctx.assetPath, exception));
                return;
            }

            ////TODO: make sure action names are unique

            // Create asset.
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.m_ActionMaps = maps;
            ctx.AddObjectToAsset("<root>", asset);
            ctx.SetMainObject(asset);

            // Create subasset for each action.
            for (var i = 0; i < maps.Length; ++i)
            {
                var set = maps[i];
                var haveSetName = !string.IsNullOrEmpty(set.name);

                foreach (var action in set.actions)
                {
                    var actionReference = ScriptableObject.CreateInstance<InputActionReference>();
                    actionReference.Set(asset, action);

                    var objectName = action.name;
                    if (haveSetName)
                        objectName = string.Format("{0}/{1}", set.name, action.name);

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
                    className = m_WrapperClassName
                };

                if (InputActionCodeGenerator.GenerateWrapperCode(wrapperFilePath, maps, options))
                {
                    // Inform database that we modified a source asset *during* import.
                    AssetDatabase.ImportAsset(wrapperFilePath);
                }
            }

            // Refresh editors.
            ActionInspectorWindow.RefreshAll();
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
