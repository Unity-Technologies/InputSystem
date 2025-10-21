using System;

////REVIEW: Can we somehow make this a simple struct? The one problem we have is that we can't put struct instances as sub-assets into
////        the import (i.e. InputActionImporter can't do AddObjectToAsset with them). However, maybe there's a way around that. The thing
////        is that we really want to store the asset reference plus the action GUID on the *user* side, i.e. the referencing side. Right
////        now, what happens is that InputActionImporter puts these objects along with the reference and GUID they contain in the
////        *imported* object, i.e. right with the asset. This partially defeats the whole purpose of having these objects and it means
////        that now the GUID doesn't really matter anymore. Rather, it's the file ID that now has to be stable.
////
////        If we always store the GUID and asset reference on the user side, we can put the serialized data *anywhere* and it'll remain
////        save and proper no matter what we do in InputActionImporter.

////REVIEW: should this throw if you try to assign an action that is not a singleton?

////REVIEW: akin to this, also have an InputActionMapReference?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// References a specific <see cref="InputAction"/> in an <see cref="InputActionMap"/>
    /// stored inside an <see cref="InputActionAsset"/>.
    /// </summary>
    /// <remarks>
    /// The difference to a plain reference directly to an <see cref="InputAction"/> object is
    /// that an InputActionReference can be serialized without causing the referenced <see cref="InputAction"/>
    /// to be serialized as well. The reference will remain intact even if the action or the map
    /// that contains the action is renamed.
    ///
    /// References can be set up graphically in the editor by dropping individual actions from the project
    /// browser onto a reference field.
    /// </remarks>
    /// <seealso cref="InputActionProperty"/>
    /// <seealso cref="InputAction"/>
    /// <seealso cref="InputActionAsset"/>
    public class InputActionReference : ScriptableObject
    {
        /// <summary>
        /// The asset that the referenced action is part of. Null if the reference
        /// is not initialized or if the asset has been deleted.
        /// </summary>
        /// <value>InputActionAsset of the referenced action.</value>
        public InputActionAsset asset => m_Action?.m_ActionMap != null ? m_Action.m_ActionMap.asset : m_Asset;

        /// <summary>
        /// The action that the reference resolves to. Null if the action cannot be found.
        /// </summary>
        /// <value>The action that reference points to.</value>
        /// <remarks>
        /// Actions are resolved on demand based on their internally stored IDs.
        /// </remarks>
        public InputAction action
        {
            get
            {
                // Note that we need to check multiple things here that could invalidate the validity of the reference:
                // 1) m_Action != null, this indicates if we have a resolved cached reference.
                // 2) m_Action.actionMap != null, this would fail if the action has been removed from an action map
                //    and converted into a "singleton action". This would render the reference invalid since the action
                //    is no longer indirectly bound to m_Asset.
                // 3) m_Action.actionMap.asset == m_Asset, needs to be checked to make sure that its action map
                //    have not been moved to another asset which would invalidate the reference since reference is
                //    defined by action GUID and asset reference.
                // 4) m_Asset, a Unity object life-time check that would fail if the asset has been deleted.
                if (m_Action != null && m_Action.actionMap != null && m_Action.actionMap.asset == m_Asset && m_Asset)
                    return m_Action;

                // Attempt to resolve action based on asset and GUID.
                return (m_Action = m_Asset ? m_Asset.FindAction(new Guid(m_ActionId)) : null);
            }
        }

        /// <summary>
        /// Initialize the reference to refer to the given action.
        /// </summary>
        /// <param name="action">An input action. Must be contained in an <see cref="InputActionMap"/>
        /// that is itself contained in an <see cref="InputActionAsset"/>. Can be <c>null</c> in which
        /// case the reference is reset to its default state which does not reference an action.</param>
        /// <exception cref="InvalidOperationException"><paramref name="action"/> is not contained in an
        /// <see cref="InputActionMap"/> that is itself contained in an <see cref="InputActionAsset"/>.</exception>
        /// <exception cref="InvalidOperationException">If attempting to mutate a reference object
        /// that is backed by an .inputactions asset. This is not allowed to prevent side-effects.</exception>
        public void Set(InputAction action)
        {
            if (action == null)
            {
                m_Asset = null;
                m_ActionId = null;
                m_Action = null;
                name = string.Empty; // Scriptable object default name is empty string.
                return;
            }

            var map = action.actionMap;
            if (map == null || map.asset == null)
                throw new InvalidOperationException(
                    $"Action '{action}' must be part of an InputActionAsset in order to be able to create an InputActionReference for it");

            SetInternal(map.asset, action);
        }

        /// <summary>
        /// Look up an action in the given asset and initialize the reference to
        /// point to it.
        /// </summary>
        /// <param name="asset">An .inputactions asset.</param>
        /// <param name="mapName">Name of the <see cref="InputActionMap"/> in <paramref name="asset"/>
        /// (see <see cref="InputActionAsset.actionMaps"/>). Case-insensitive.</param>
        /// <param name="actionName">Name of the action in <paramref name="mapName"/>. Case-insensitive.</param>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> is <c>null</c> -or-
        /// <paramref name="mapName"/> is <c>null</c> or empty -or- <paramref name="actionName"/>
        /// is <c>null</c> or empty.</exception>
        /// <exception cref="InvalidOperationException">If attempting to mutate a reference object
        /// that is backed by by .inputactions asset. This is not allowed to prevent side-effects.</exception>
        /// <exception cref="ArgumentException">No action map called <paramref name="mapName"/> could
        /// be found in <paramref name="asset"/> -or- no action called <paramref name="actionName"/>
        /// could be found in the action map called <paramref name="mapName"/> in <paramref name="asset"/>.</exception>
        public void Set(InputActionAsset asset, string mapName, string actionName)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (string.IsNullOrEmpty(mapName))
                throw new ArgumentNullException(nameof(mapName));
            if (string.IsNullOrEmpty(actionName))
                throw new ArgumentNullException(nameof(actionName));

            var actionMap = asset.FindActionMap(mapName);
            if (actionMap == null)
                throw new ArgumentException($"No action map '{mapName}' in '{asset}'", nameof(mapName));

            var foundAction = actionMap.FindAction(actionName);
            if (foundAction == null)
                throw new ArgumentException($"No action '{actionName}' in map '{mapName}' of asset '{asset}'",
                    nameof(actionName));

            SetInternal(asset, foundAction);
        }

        private void SetInternal(InputActionAsset assetArg, InputAction actionArg)
        {
            CheckImmutableReference();

            // If we are setting the reference in edit-mode, we want the state to be reflected in the serialized
            // object and hence assign serialized fields. This is a destructive operation.
            m_Asset = assetArg;
            m_ActionId = actionArg.id.ToString();
            m_Action = actionArg;
            name = GetDisplayName(actionArg);
        }

        /// <summary>
        /// Return a string representation of the reference useful for debugging.
        /// </summary>
        /// <returns>A string representation of the reference.</returns>
        public override string ToString()
        {
            var value = action; // Indirect resolve
            if (value == null)
                return base.ToString();
            if (value.actionMap != null)
                return m_Asset != null ? $"{m_Asset.name}:{value.actionMap.name}/{value.name}" : $"{value.actionMap.name}/{value.name}";
            return m_Asset != null ? $"{m_Asset.name}:{m_ActionId}" : m_ActionId;
        }

        private static string GetDisplayName(InputAction action)
        {
            return !string.IsNullOrEmpty(action?.actionMap?.name)
                ? $"{action.actionMap?.name}/{action.name}"
                : action?.name;
        }

        /// <summary>
        /// Return a string representation useful for showing in UI.
        /// </summary>
        internal string ToDisplayName()
        {
            return string.IsNullOrEmpty(name) ? GetDisplayName(action) : name;
        }

        /// <summary>
        /// Convert an InputActionReference to the InputAction it points to.
        /// </summary>
        /// <param name="reference">An InputActionReference object. Can be null.</param>
        /// <returns>The value of <see cref="action"/> from <paramref name="reference"/>. Can be null.</returns>
        public static implicit operator InputAction(InputActionReference reference)
        {
            return reference?.action;
        }

        /// <summary>
        /// Create a new InputActionReference object that references the given action.
        /// </summary>
        /// <param name="action">An input action. Must be contained in an <see cref="InputActionMap"/>
        /// that is itself contained in an <see cref="InputActionAsset"/>. Can be <c>null</c> in which
        /// case the reference is reset to its default state which does not reference an action.</param>
        /// <returns>A new InputActionReference referencing <paramref name="action"/>.</returns>
        public static InputActionReference Create(InputAction action)
        {
            var reference = CreateInstance<InputActionReference>();
            reference.Set(action);
            return reference;
        }

        /// <summary>
        /// Clears the cached <see cref="m_Action"/> field for all current <see cref="InputActionReference"/> objects.
        /// </summary>
        /// <remarks>
        /// This method is used to clear the Action references when exiting PlayMode since those objects are no
        /// longer valid.
        /// </remarks>
        internal static void InvalidateAll()
        {
            // It might be possible that Object.FindObjectOfTypeAll(true) would be sufficient here since we only
            // need to invalidate non-serialized data on active/loaded objects. This returns a lot more, but for
            // now we keep it like this to not change more than we have to. Optimizing this can be done separately.
            var allActionRefs = Resources.FindObjectsOfTypeAll(typeof(InputActionReference));
            foreach (var obj in allActionRefs)
                ((InputActionReference)obj).Invalidate();
        }

        /// <summary>
        /// Clears the cached <see cref="m_Action"/> field for this <see cref="InputActionReference"/> instance.
        /// </summary>
        /// <remarks>
        /// After calling this, the next call to <see cref="action"/> will resolve a new <see cref="InputAction"/>
        /// reference from the existing <see cref="InputActionAsset"/> just as if using it for the first time.
        /// The serialized <see cref="m_Asset"/> and <see cref="m_ActionId"/> fields are not touched and will continue
        /// to hold their current values. Also the name remains valid since the underlying reference data is unmodified.
        /// </remarks>
        internal void Invalidate()
        {
            m_Action = null;
        }

        [SerializeField] internal InputActionAsset m_Asset;
        // Can't serialize System.Guid and Unity's GUID is editor only so these
        // go out as strings.
        [SerializeField] internal string m_ActionId;

        /// <summary>
        /// The resolved, cached input action.
        /// </summary>
        [NonSerialized] private InputAction m_Action;

        /// <summary>
        /// Equivalent to <see cref="InputActionReference.action"/>.
        /// </summary>
        /// <returns>The associated action reference if its a valid reference, else <c>null</c>.</returns>
        public InputAction ToInputAction()
        {
            return action;
        }

        /// <summary>
        /// Checks if this input action reference instance can be safely mutated without side effects.
        /// </summary>
        /// <remarks>
        /// This check isn't needed in player builds since ScriptableObject would never be persisted if mutated
        /// in a player.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if this input action reference is part of an
        /// input actions asset and mutating it would have side-effects on the projects assets.</exception>
        private void CheckImmutableReference()
        {
            #if UNITY_EDITOR
            // Note that we do a lot of checking here, but it is only for a rather slim (unintended) use case in
            // editor and not in final builds. The alternative would be to set a non-serialized field on the reference
            // when importing assets which would simplify this class, but it adds complexity to import stage and
            // is more difficult to assess from a asset version portability perspective.
            static bool CanSetReference(InputActionReference reference)
            {
                // "Immutable" input action references are always sub-assets of InputActionAsset.
                var isSubAsset = UnityEditor.AssetDatabase.IsSubAsset(reference);
                if (!isSubAsset)
                    return true;

                // If we cannot get the path of our reference, we cannot be a persisted asset within an InputActionAsset.
                var path = UnityEditor.AssetDatabase.GetAssetPath(reference);
                if (path == null)
                    return true;

                // If we cannot get the main asset we cannot be a persisted asset within an InputActionAsset.
                // Also we check that it is the expected type.
                var mainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
                if (!mainAsset)
                    return true;

                // We can only allow setting the reference if it is not part of an persisted InputActionAsset.
                return (mainAsset is not InputActionAsset);
            }

            // Prevent accidental mutation of the source asset if this InputActionReference is a persisted object
            // residing as a sub-asset within a .inputactions asset.
            // This is not needed for players since scriptable objects aren't serialized back from within a player.
            if (!CanSetReference(this))
            {
                throw new InvalidOperationException("Attempting to modify an immutable InputActionReference instance " +
                    "that is part of an .inputactions asset. This is not allowed since it would modify the source " +
                    "asset in which the reference is serialized and potentially corrupt it. " +
                    "Instead use InputActionReference.Create(action) to create a new mutable " +
                    "in-memory instance or serialize it as a separate asset if the intent is for changes to " +
                    "survive domain reloads.");
            }
            #endif // UNITY_EDITOR
        }
    }
}
