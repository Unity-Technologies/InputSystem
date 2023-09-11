using System;
using UnityEngine.Serialization;

namespace UnityEngine.InputSystem.Editor
{
    [Serializable]
    public struct InputActionAssetProperty
    {
        public InputActionAsset m_ActionsAsset;
        public bool m_IsAssetProjectWideActions;
    }
}
