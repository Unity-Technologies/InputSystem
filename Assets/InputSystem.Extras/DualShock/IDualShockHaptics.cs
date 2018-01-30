using ISX.Haptics;
using UnityEngine;

namespace ISX.DualShock
{
    public interface IDualShockHaptics : IDualMotorRumble
    {
        void SetLightBarColor(Color color);
    }
}
