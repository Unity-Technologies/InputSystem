#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.InputSystem.Utilities;

////FIXME: The importer accesses icons through the asset db (which EditorGUIUtility.LoadIcon falls back on) which will
////       not yet have been imported when the project is imported from scratch; this results in errors in the log and in generic
////       icons showing up for the assets

#pragma warning disable 0649
namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Imports an <see cref="InputActionAsset"/> from JSON.
    /// </summary>
    /// <remarks>
    /// Can generate code wrappers for the contained action sets as a convenience.
    /// Will not overwrite existing wrappers except if the generated code actually differs.
    /// </remarks>
    [ScriptedImporter(kVersion, InputActionAsset.Extension)]
    internal class InputActionImporter : ScriptedImporter
    {
        private const int kVersion = 10;

        private const string kActionIcon = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/InputAction.png";
        private const string kAssetIcon = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/InputActionAsset.png";

        [SerializeField] private bool m_GenerateWrapperCode;
        [SerializeField] private string m_WrapperCodePath;
        [SerializeField] private string m_WrapperClassName;
        [SerializeField] private string m_WrapperCodeNamespace;

        private static InlinedArray<Action> s_OnImportCallbacks;

        public static event Action onImport
        {
            add => s_OnImportCallbacks.Append(value);
            remove => s_OnImportCallbacks.Remove(value);
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));

            foreach (var callback in s_OnImportCallbacks)
                callback();

            ////REVIEW: need to check with version control here?
            // Read file.
            string text;
            try
            {
                text = File.ReadAllText(ctx.assetPath);
            }
            catch (Exception exception)
            {
                ctx.LogImportError($"Could not read file '{ctx.assetPath}' ({exception})");
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
                ctx.LogImportError($"Could not parse input actions in JSON format from '{ctx.assetPath}' ({exception})");
                DestroyImmediate(asset);
                return;
            }

            // Force name of asset to be that on the file on disk instead of what may be serialized
            // as the 'name' property in JSON.
            asset.name = Path.GetFileNameWithoutExtension(assetPath);

            // Load icons.
            ////REVIEW: the icons won't change if the user changes skin; not sure it makes sense to differentiate here
            var assetIcon = (Texture2D)EditorGUIUtility.Load(kAssetIcon);
            var actionIcon = (Texture2D)EditorGUIUtility.Load(kActionIcon);

            // Add asset.
            ctx.AddObjectToAsset("<root>", asset, assetIcon);
            ctx.SetMainObject(asset);

            // Make sure all the elements in the asset have GUIDs.
            var maps = asset.actionMaps;
            foreach (var map in maps)
            {
                // Make sure action map has GUID.
                if (string.IsNullOrEmpty(map.m_Id))
                    map.GenerateId();

                // Make sure all actions have GUIDs.
                foreach (var action in map.actions)
                {
                    if (string.IsNullOrEmpty(action.m_Id))
                        action.GenerateId();
                }

                // Make sure all bindings have GUIDs.
                for (var i = 0; i < map.m_Bindings.LengthSafe(); ++i)
                {
                    if (string.IsNullOrEmpty(map.m_Bindings[i].m_Id))
                        map.m_Bindings[i].GenerateId();
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
                        objectName = $"{map.name}/{action.name}";

                    actionReference.name = objectName;
                    ctx.AddObjectToAsset(objectName, actionReference, actionIcon);
                }
            }

            // Generate wrapper code, if enabled.
            if (m_GenerateWrapperCode)
            {
                var wrapperFilePath = m_WrapperCodePath;
                if (string.IsNullOrEmpty(wrapperFilePath))
                {
                    // Placed next to .inputactions file.
                    var assetPath = ctx.assetPath;
                    var directory = Path.GetDirectoryName(assetPath);
                    var fileName = Path.GetFileNameWithoutExtension(assetPath);
                    wrapperFilePath = Path.Combine(directory, fileName) + ".cs";
                }
                else if (wrapperFilePath.StartsWith("./") || wrapperFilePath.StartsWith(".\\") ||
                         wrapperFilePath.StartsWith("../") || wrapperFilePath.StartsWith("..\\"))
                {
                    // User-specified file relative to location of .inputactions file.
                    var assetPath = ctx.assetPath;
                    var directory = Path.GetDirectoryName(assetPath);
                    wrapperFilePath = Path.Combine(directory, wrapperFilePath);
                }
                else if (!wrapperFilePath.ToLower().StartsWith("assets/") &&
                         !wrapperFilePath.ToLower().StartsWith("assets\\"))
                {
                    // User-specified file in Assets/ folder.
                    wrapperFilePath = Path.Combine("Assets", wrapperFilePath);
                }

                var dir = Path.GetDirectoryName(wrapperFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var options = new InputActionCodeGenerator.Options
                {
                    sourceAssetPath = ctx.assetPath,
                    namespaceName = m_WrapperCodeNamespace,
                    className = m_WrapperClassName,
                };

                if (InputActionCodeGenerator.GenerateWrapperCode(wrapperFilePath, asset, options))
                {
                    // When we generate the wrapper code cs file during asset import, we cannot call ImportAsset on that directly because
                    // script assets have to be imported before all other assets, and are not allowed to be added to the import queue during
                    // asset import. So instead we register a callback to trigger a delayed asset refresh which should then pick up the
                    // changed/added script, and trigger a new import.
                    EditorApplication.delayCall += AssetDatabase.Refresh;
                }
            }

            // Refresh editors.
            InputActionEditorWindow.RefreshAllOnAssetReimport();
        }

        ////REVIEW: actually pre-populate with some stuff?
        private const string kDefaultAssetLayout = "{}";

        // Add item to plop an .inputactions asset into the project.
        [MenuItem("Assets/Create/Input Actions")]
        public static void CreateInputAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent("New Controls." + InputActionAsset.Extension,
                kDefaultAssetLayout);
        }
    }
}
#endif // UNITY_EDITOR
