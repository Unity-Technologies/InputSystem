#if (UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WSA || UNITY_SWITCH || UNITY_LUMIN || UNITY_INPUT_FORCE_XR_PLUGIN) && UNITY_INPUT_SYSTEM_ENABLE_XR && ENABLE_VR
#define ENABLE_XR_COMBINED_DEFINE
#endif

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteInEditMode]
public class TestEditMode : MonoBehaviour
{
    [SerializeField]
    bool m_RunUpdatesInEditMode;

    public bool runUpdatesInEditMode
    {
        get => m_RunUpdatesInEditMode;
        set => m_RunUpdatesInEditMode = value;
    }

#if UNITY_EDITOR && ENABLE_XR_COMBINED_DEFINE
    void OnEnable()
    {
        EditorApplication.update += TickEditor;
        InputSystem.runUpdatesInEditMode = m_RunUpdatesInEditMode;
    }

    void OnDisable()
    {
        InputSystem.runUpdatesInEditMode = false;
        EditorApplication.update -= TickEditor;
    }

    void OnValidate()
    {
        if (isActiveAndEnabled)
        {
            InputSystem.runUpdatesInEditMode = m_RunUpdatesInEditMode;
        }
    }

    void TickEditor()
    {
        EditorApplication.QueuePlayerLoopUpdate();
    }

#endif
}
