using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
#if UNITY_MAGIC_LEAP
using UnityEngine.XR.MagicLeap;
#endif

public class MLControllerEvents : MonoBehaviour
{
    // Public Vibe Settings
    public Dropdown vibePatternDropdown;
    public Dropdown vibeIntensityDropdown;

    // Public LED Pattern Settings
    public Dropdown ledPatternDropdown;
    public Dropdown ledColorDropdown;
    public Slider ledDurationSlider;

    // Public LED Effect Settings
    public Dropdown ledEffectDropdown;
    public Dropdown ledSpeedDropdown;

    public void SendVibe()
    {
#if UNITY_MAGIC_LEAP
        VibePattern pattern = (VibePattern)vibePatternDropdown.value;
        VibeIntensity intensity = (VibeIntensity)vibeIntensityDropdown.value;

        MagicLeapController controller = InputSystem.GetDevice<MagicLeapController>();
        if (controller != null)
        {
            controller.StartVibe(pattern, intensity);
        }
#endif
    }

    public void SendLEDPattern()
    {
#if UNITY_MAGIC_LEAP
        LEDPattern pattern = (LEDPattern)ledPatternDropdown.value;
        LEDColor color = (LEDColor)ledColorDropdown.value;
        uint duration = (uint)ledDurationSlider.value;

        MagicLeapController controller = InputSystem.GetDevice<MagicLeapController>();
        if (controller != null)
        {
            controller.StartLEDPattern(pattern, color, duration);
        }
#endif
    }

    public void SendLEDEffect()
    {
#if UNITY_MAGIC_LEAP
        LEDEffect effect = (LEDEffect)ledEffectDropdown.value;
        LEDSpeed speed = (LEDSpeed)ledSpeedDropdown.value;
        LEDPattern pattern = (LEDPattern)ledPatternDropdown.value;
        LEDColor color = (LEDColor)ledColorDropdown.value;
        uint duration = (uint)ledDurationSlider.value;

        MagicLeapController controller = InputSystem.GetDevice<MagicLeapController>();
        if (controller != null)
        {
            controller.StartLEDEffect(effect, speed, pattern, color, duration);
        }
#endif
    }
}
