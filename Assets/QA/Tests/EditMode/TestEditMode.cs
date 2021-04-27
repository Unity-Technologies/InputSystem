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

#if UNITY_EDITOR
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
