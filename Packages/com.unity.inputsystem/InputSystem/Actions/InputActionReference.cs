using System;
using System.Linq;

////REVIEW: should this throw if you try to assign an action that is not a singleton?

////REVIEW: akin to this, also have an InputActionMapReference?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// References a specific <see cref="InputAction">action</see> in an <see cref="InputActionMap">
    /// action map</see> stored inside an <see cref="InputActionAsset"/>.
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
    public class InputActionReference : ScriptableObject
    {
        /// <summary>
        /// The asset that the referenced action is part of.
        /// </summary>
        public InputActionAsset asset => m_Asset;

        /// <summary>
        /// The action that the reference resolves to.
        /// </summary>
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

                    var map = m_Asset.GetActionMap(new Guid(m_ActionMapId));
                    m_Action = map.GetAction(new Guid(m_ActionId));
                }

                return m_Action;
            }
        }

        public void Set(InputAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var map = action.actionMap;
            if (map == null || map.asset == null)
                throw new InvalidOperationException(
                    $"Action '{action}' must be part of an InputActionAsset in order to be able to create an InputActionReference for it");

            SetInternal(map.asset, action);
        }

        public void Set(InputActionAsset asset, string mapName, string actionName)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (string.IsNullOrEmpty(mapName))
                throw new ArgumentNullException(nameof(mapName));
            if (string.IsNullOrEmpty(actionName))
                throw new ArgumentNullException(nameof(actionName));

            var actionMap = asset.GetActionMap(mapName);
            var action = actionMap.GetAction(actionName);

            SetInternal(asset, action);
        }

        private void SetInternal(InputActionAsset asset, InputAction action)
        {
            var actionMap = action.actionMap;
            if (!asset.actionMaps.Contains(actionMap))
                throw new ArgumentException(
                    $"Action '{action}' is not contained in asset '{asset}'", nameof(action));

            m_Asset = asset;
            m_ActionMapId = actionMap.id.ToString();
            m_ActionId = action.id.ToString();

            ////REVIEW: should this dirty the asset if IDs had not been generated yet?
        }

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
                    return $"{m_Asset.name}:{m_ActionMapId}/{m_ActionId}";
            }

            return base.ToString();
        }

        public static implicit operator InputAction(InputActionReference reference)
        {
            return reference.action;
        }

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
        [SerializeField] internal string m_ActionMapId;
        [SerializeField] internal string m_ActionId;

        /// <summary>
        /// The resolved, cached input action.
        /// </summary>
        [NonSerialized] private InputAction m_Action;
    }
}
