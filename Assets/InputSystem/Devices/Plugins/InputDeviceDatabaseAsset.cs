using UnityEngine;

namespace ISX
{
    // Imported version of a JSON source asset that determines which
    // input device-related assets in the project go into the build. The
    // imported asset corresponds to only the assets that get included
    // in the build for the current build target.
    public class InputDeviceDatabaseAsset : ScriptableObject
    {
        public const string kExtension = "inputdevices";
    }
}
