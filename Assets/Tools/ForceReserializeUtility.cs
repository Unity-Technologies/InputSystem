using UnityEditor;

static class ForceReserializeUtility
{
    // Over time, the data on disk and data in memory diverges naturally, and that can become inconvenient over time.
    // For example, when you make a simple change, save and realize there's a lot more that was serialized actually.
    // That makes reviews harder among other things.
    // So, this simple utility just forces writing all data to disk, use it once in a while to reduce the diff drift.
    [MenuItem("QA Tools/Force Reserialize All Assets")]
    static void ForceReserializeAllAssets() => AssetDatabase.ForceReserializeAssets(AssetDatabase.GetAllAssetPaths(), ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
}
