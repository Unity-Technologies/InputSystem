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
        protected override string GetNonAliasedNames(string name)
        {
            switch (name)
            {
                case nameof(GamepadButton.North):
                case nameof(GamepadButton.Y):
                case nameof(GamepadButton.Triangle):
                    return nameof(GamepadButton.North);

                case nameof(GamepadButton.South):
                case nameof(GamepadButton.A):
                case nameof(GamepadButton.Cross):
                    return nameof(GamepadButton.South);

                case nameof(GamepadButton.East):
                case nameof(GamepadButton.B):
                case nameof(GamepadButton.Circle):
                    return nameof(GamepadButton.East);

                case nameof(GamepadButton.West):
                case nameof(GamepadButton.X):
                case nameof(GamepadButton.Square):
                    return nameof(GamepadButton.West);

                default: return string.Empty;
            }
        }
    }
}
#endif // UNITY_EDITOR
