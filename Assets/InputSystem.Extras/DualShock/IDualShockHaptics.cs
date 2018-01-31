using ISX.Haptics;
using UnityEngine;

namespace ISX.Plugins.DualShock
{
    public interface IDualShockHaptics : IDualMotorRumble
    {
        void SetLightBarColor(Color color);
    }
}
