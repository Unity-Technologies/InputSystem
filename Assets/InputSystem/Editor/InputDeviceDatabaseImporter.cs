#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

namespace ISX.Editor
{
    [ScriptedImporter(1, "inputdevices")]
    public class InputDeviceDatabaseImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            throw new System.NotImplementedException();
        }

        private const string kDefaultAssetDatabaseTemplate = "{}";

        // Add item to plop an .inputactions asset into the project.
        [MenuItem("Assets/Create/Input Device Database")]
        public static void CreateInputDeviceDatabaseAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent("New Devices." + InputDeviceDatabaseAsset.kExtension,
                kDefaultAssetDatabaseTemplate);
        }
    }
}
#endif // UNITY_EDITOR
