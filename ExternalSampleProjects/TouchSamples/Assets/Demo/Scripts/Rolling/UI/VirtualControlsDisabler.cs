using UnityEngine;

namespace InputSamples.Demo.Rolling.UI
{
    /// <summary>
    /// Simple component to disable virtual controls on desktop.
    /// </summary>
    public class VirtualControlsDisabler : MonoBehaviour
    {
        protected void OnEnable()
        {
#if (!UNITY_ANDROID && !UNITY_IOS) || UNITY_EDITOR
            gameObject.SetActive(false);
#endif
        }
    }
}
