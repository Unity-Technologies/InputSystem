using UnityEngine;
using UnityEngine.Experimental.Input.Plugins.UI;

/// <summary>
/// This is a small helper to enable and disable any active controls on the UIActionInputModule.
/// Used as a placeholder for now until action ownership becomes more clear.
/// </summary>
public class UIActionInputModuleEnabler : MonoBehaviour
{
    void OnEnable()
    {
        UIActionInputModule inputModule = GetComponent<UIActionInputModule>();
        if (inputModule != null)
            inputModule.EnableAllActions();
    }

    void OnDisable()
    {
        UIActionInputModule inputModule = GetComponent<UIActionInputModule>();
        if (inputModule != null)
            inputModule.DisableAllActions();
    }
}
