using System;

////REVIEW: akin to this, also have an InputActionSetReference? :(

namespace UnityEngine.Experimental.Input
{
    // Object that represents a specific action in a specific action
    // set *without* containing the action. This is useful for passing
    // around references to actions as objects.
    //
    // Example: Put an InputActionReference field on your MonoBehaviour and
    //          then drop an action from an .inputaction asset onto your
    //          MonoBehaviour in the inspector.
    //
    // NOTE: InputActionReferences are named-based and will break if either
    //       the action set or the action itself is renamed.
    public class InputActionReference : ScriptableObject
    {
        [SerializeField] internal InputActionAsset m_Asset;

        [SerializeField] internal string m_SetName;
        [SerializeField] internal string m_ActionName;

        [NonSerialized] private InputAction m_Action;

        public InputAction action
        {
            get
            {
                if (m_Action == null)
                {
                    if (m_Asset == null)
                        return null;

                    var set = m_Asset.GetActionSet(m_SetName);
                    m_Action = set.GetAction(m_ActionName);
                }

                return m_Action;
            }
        }

        public void Set(InputActionAsset asset, string set, string action)
        {
            if (string.IsNullOrEmpty(set))
                throw new ArgumentException("set");
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("action");

            m_Asset = asset;
            m_SetName = set;
            m_ActionName = action;
        }

        public override string ToString()
        {
            if (m_Asset != null)
                return string.Format("{0}:{1}/{2}", m_Action.name, m_SetName, m_ActionName);

            return base.ToString();
        }
    }
}
