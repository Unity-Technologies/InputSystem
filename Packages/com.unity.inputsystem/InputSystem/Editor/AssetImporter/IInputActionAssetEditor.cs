#if UNITY_EDITOR

namespace UnityEngine.InputSystem.Editor
{
    // For clarity, the tables below indicate the callback sequences of the asset modification processor and
    // asset post-processor for various user operations done on assets.
    //
    // User operation:                Callback sequence:
    // ----------------------------------------------------------------------------------------
    // Save                           Imported(s)
    // Delete                         OnWillDelete(s), Deleted(s)
    // Copy                           Imported(s)
    // Rename                         OnWillMove(s,d), Imported(d), Moved(s,d)
    // Move (drag) / Cut+Paste        OnWillMove(s,d), Moved(s,d)
    // ------------------------------------------------------------------------------------------------------------
    //
    // User operation:                Callback/call sequence:
    // ------------------------------------------------------------------------------------------------------------
    // Save                           Imported(s)
    // Delete                         OnWillDelete(s), Deleted(s)
    // Copy                           Imported(s), Fix(s), Imported(s)
    // Rename                         OnWillMove(s,d), Imported(d), Fix(d), Moved(s,d), Imported(d)
    // Move(drag) / Cut+Paste         OnWillMove(s,d), Moved(s,d)
    // ------------------------------------------------------------------------------------------------------------
    // Note that as stated in table above, JSON name changes (called "Fix" above) will only be executed when either
    // Copying, Renaming within the editor. For all other operations the name and file name would not differ.
    //
    // External user operation:       Callback/call sequence:
    // ------------------------------------------------------------------------------------------------------------
    // Save                           Imported(s)
    // Delete                         Deleted(s)
    // Copy                           Imported(s)
    // Rename                         Imported(d), Deleted(s)
    // Move(drag) / Cut+Paste         Imported(d), Deleted(s)
    // ------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Callback interface for monitoring changes to assets.
    /// </summary>
    internal interface IAssetObserver
    {
        /// <summary>
        /// Callback triggered when the associated asset is imported.
        /// </summary>
        void OnAssetImported();

        /// <summary>
        /// Callback triggered when the associated asset is moved.
        /// </summary>
        void OnAssetMoved();

        /// <summary>
        /// Callback triggered when the associated asset is deleted.
        /// </summary>
        void OnAssetDeleted();
    }

    /// <summary>
    /// Interface representing an editor capable of editing <c>InputActionAsset</c> instances associated
    /// with an asset file in the Asset Database (ADB).
    /// </summary>
    internal interface IInputActionAssetEditor : IAssetObserver
    {
        /// <summary>
        /// A read-only string representation of the asset GUID associated with the asset being edited.
        /// </summary>
        string assetGUID { get; }

        /// <summary>
        /// Returns whether the editor has unsaved changes compared to the associated imported source asset.
        /// </summary>
        bool isDirty { get; }
    }
}

#endif // UNITY_EDITOR
