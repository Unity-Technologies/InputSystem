using System;
using System.Linq;

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
        public InputActionAsset asset => m_Asset;

        /// <summary>
        /// The action that the reference resolves to. Null if the action
        /// cannot be found.
        /// </summary>
        /// <value>The action that reference points to.</value>
        /// <remarks>
        /// Actions are resolved on demand based on their internally stored IDs.
        /// </remarks>
        public InputAction action
        {
            get
            {
                if (m_Action == null)
                {
                    if (m_Asset == null)
                        return null;

                    m_Action = m_Asset.FindAction(new Guid(m_ActionId));
                }

                return m_Action;
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
        public void Set(InputAction action)
        {
            if (action == null)
            {
                m_Asset = default;
                m_ActionId = default;
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

            var action = actionMap.FindAction(actionName);
            if (action == null)
                throw new ArgumentException($"No action '{actionName}' in map '{mapName}' of asset '{asset}'",
                    nameof(actionName));

            SetInternal(asset, action);
        }

        private void SetInternal(InputActionAsset asset, InputAction action)
        {
            var actionMap = action.actionMap;
            if (!asset.actionMaps.Contains(actionMap))
                throw new ArgumentException(
                    $"Action '{action}' is not contained in asset '{asset}'", nameof(action));

            m_Asset = asset;
            m_ActionId = action.id.ToString();
            name = GetDisplayName(action);

            ////REVIEW: should this dirty the asset if IDs had not been generated yet?
        }

        /// <summary>
        /// Return a string representation of the reference useful for debugging.
        /// </summary>
        /// <returns>A string representation of the reference.</returns>
        public override string ToString()
        {
            try
            {
                var action = this.action;
                return $"{m_Asset.name}:{action.actionMap.name}/{action.name}";
            }
            catch
            {
                if (m_Asset != null)
                    return $"{m_Asset.name}:{m_ActionId}";
            }

            return base.ToString();
        }

        private static string GetDisplayName(InputAction action)
        {
            return !string.IsNullOrEmpty(action?.actionMap?.name) ? $"{action.actionMap?.name}/{action.name}" : action?.name;
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
            if (action == null)
                return null;
            var reference = CreateInstance<InputActionReference>();
            reference.Set(action);
            return reference;
        }

        [SerializeField] internal InputActionAsset m_Asset;
        // Can't serialize System.Guid and Unity's GUID is editor only so these
        // go out as strings.
        [SerializeField] internal string m_ActionId;

        /// <summary>
        /// The resolved, cached input action.
        /// </summary>
        [NonSerialized] private InputAction m_Action;

        // Make annoying Microsoft code analyzer happy.
        public InputAction ToInputAction()
        {
            return action;
        }
    }
}
