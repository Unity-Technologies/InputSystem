#if UNITY_EDITOR
using UnityEngine;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Legacy compatibility type. Package versions up to and including 1.18 stored the domain-reload
    /// survival state in a hidden, <see cref="HideFlags.HideAndDontSave"/> ScriptableObject of this
    /// exact type and name (this type was renamed to <see cref="InputSystemStateManager"/> in 1.20).
    /// </summary>
    /// <remarks>
    /// When a project upgrades from such a version, that instance survives the domain reload triggered
    /// by the upgrade - but only if its serialized script reference still resolves. That reference is
    /// <c>{ fileID: 11500000, guid: &lt;this file's guid&gt; }</c>, so this file MUST keep the original
    /// <c>InputSystemObject.cs</c> GUID (<c>5cdce2bffd1e49bda08b3db54a031207</c>) and this MUST be the
    /// primary class of the file (matching the file name). If the reference fails to resolve, Unity
    /// drops the object during the reload and the input state - including the list of connected devices
    /// - is lost on upgrade, with no in-process recovery (native only re-reports devices once per editor
    /// process). See ISX-2569.
    ///
    /// The fields below must keep the same names and serialized layout as 1.18's InputSystemObject so the
    /// surviving data deserializes into them. <c>InputSystemEditorInitializer.InitializeInEditor</c>
    /// detects a surviving instance of this type, migrates its state into a
    /// <see cref="InputSystemStateManager"/>, and then destroys it.
    /// </remarks>
    internal class InputSystemObject : ScriptableObject
    {
        [SerializeField] public InputSystemState systemState;
        [SerializeField] public bool newInputBackendsCheckedAsEnabled;
        [SerializeField] public string settings;
        [SerializeField] public double exitEditModeTime;
        [SerializeField] public double enterPlayModeTime;
    }
}
#endif // UNITY_EDITOR
