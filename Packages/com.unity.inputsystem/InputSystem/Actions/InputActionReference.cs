using System;
using System.Linq;

////REVIEW: akin to this, also have an InputActionMapReference?

namespace UnityEngine.Experimental.Input
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
        public InputActionAsset asset
        {
            get { return m_Asset; }
        }

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
                throw new ArgumentNullException("action");

            var map = action.actionMap;
            if (map == null || map.asset == null)
                throw new InvalidOperationException(string.Format(
                    "Action '{0}' must be part of an InputActionAsset in order to be able to create an InputActionReference for it",
                    action));

            SetInternal(map.asset, action);
        }

        public void Set(InputActionAsset asset, string mapName, string actionName)
        {
            if (asset == null)
                throw new ArgumentNullException("asset");
            if (string.IsNullOrEmpty(mapName))
                throw new ArgumentNullException("mapName");
            if (string.IsNullOrEmpty(actionName))
                throw new ArgumentNullException("actionName");

            var actionMap = asset.GetActionMap(mapName);
            var action = actionMap.GetAction(actionName);

            SetInternal(asset, action);
        }

        private void SetInternal(InputActionAsset asset, InputAction action)
        {
            var actionMap = action.actionMap;
            if (!asset.actionMaps.Contains(actionMap))
                throw new ArgumentException(
                    string.Format("Action '{0}' is not contained in asset '{1}'", action, asset));

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
                return string.Format("{0}:{1}/{2}", m_Asset.name, action.actionMap.name, action.name);
            }
            catch
            {
                if (m_Asset != null)
                    return string.Format("{0}:{1}/{2}", m_Asset.name, m_ActionMapId, m_ActionId);
            }

            return base.ToString();
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
