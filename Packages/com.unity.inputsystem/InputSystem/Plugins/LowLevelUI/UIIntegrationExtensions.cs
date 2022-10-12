// temporary place for bridging API for UI integration to access low level bits of input system

using System;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.LowLevelUIIntegration
{
    public static class UIIntegrationExtensions
    {
        public static float unscaledGameTime => InputRuntime.s_Instance.unscaledGameTime;
        public static bool runInBackground => InputRuntime.s_Instance.runInBackground;
        public static bool isPlayerFocused => InputRuntime.s_Instance.isPlayerFocused;
        
        public static event Action<object> onActionControlsChanged
        {
            add
            {
                InputActionState.s_GlobalState.onActionControlsChanged.AddCallback(value);
            }
            remove
            {
                InputActionState.s_GlobalState.onActionControlsChanged.RemoveCallback(value);
            }
        }
    }
}