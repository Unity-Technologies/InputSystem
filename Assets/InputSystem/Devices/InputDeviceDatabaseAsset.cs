using UnityEngine;

namespace ISX
{
    // Imported version of a JSON source asset that determines which
    // input device-related assets in the project go into the build. The
    // imported asset corresponds to only the assets that get included
    // in the build for the current build target.
    public class InputDeviceDatabaseAsset : ScriptableObject, IInputModuleManager
    {
        public const string kExtension = "inputdevices";

        public void Initialize()
        {
            throw new System.NotImplementedException();
        }
    }
}
