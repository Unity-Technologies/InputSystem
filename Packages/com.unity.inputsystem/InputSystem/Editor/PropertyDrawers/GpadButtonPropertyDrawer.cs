#if UNITY_EDITOR

using UnityEditor;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Property drawer for <see cref = "GamepadButton" />
    /// </summary >
    [CustomPropertyDrawer(typeof(GamepadButton))]
    internal class GpadButtonPropertyDrawer : AliasedEnumPropertyDrawer<GamepadButton>
    {
        protected override bool TryGetNonAliasedNames(string enumName, string displayName, out string outputName)
        {
            outputName = "";
            switch (displayName)
            {
                case nameof(GamepadButton.Y):
                case nameof(GamepadButton.Triangle):
                case nameof(GamepadButton.A):
                case nameof(GamepadButton.Cross):
                case nameof(GamepadButton.B):
                case nameof(GamepadButton.Circle):
                case nameof(GamepadButton.X):
                case nameof(GamepadButton.Square):
                    return true;
                default:
                    return false;
            }
        }
    }
}
#endif // UNITY_EDITOR
