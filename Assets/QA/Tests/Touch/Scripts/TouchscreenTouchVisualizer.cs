using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;

public class TouchscreenTouchVisualizer : MonoBehaviour
{
    [Tooltip("Prefab to use for the average position")]
    public GameObject averagePositionIndicatorPrefab;
    [Tooltip("Prefab to use for each individual touch")]
    public GameObject touchPositionIndicatorPrefab;

    private GameObject m_AveragePositionMarker;
    private List<GameObject> m_Touches;
    private Camera m_MainCamera;

    // Use this for initialization
    void Start()
    {
        m_Touches = new List<GameObject>();

        m_AveragePositionMarker = Instantiate<GameObject>(averagePositionIndicatorPrefab);
        m_AveragePositionMarker.SetActive(false);

        m_MainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Touchscreen touchscreen = UnityEngine.Experimental.Input.Touchscreen.current;
        Vector3 averagePosition = Vector2.zero;

        if (touchscreen == null)
        {
            Debug.Log("No touchscreen :-(");
            return;
        }

        if (touchscreen.activeTouches.Count == 0)
        {
            m_AveragePositionMarker.SetActive(false);
            DeleteExtraTouches(touchscreen.activeTouches.Count);
            return;
        }
        else
        {
            m_AveragePositionMarker.SetActive(true);
        }

        // Set touch indicator position values, creating them if necessary
        // Also start accumulating average position
        //
        for (int i = 0; i < touchscreen.activeTouches.Count; i++)
        {
            // Create new indicator if necessary
            if (i >= m_Touches.Count)
            {
                m_Touches.Add(Instantiate<GameObject>(touchPositionIndicatorPrefab));
            }

            m_Touches[i].transform.position = m_MainCamera.ScreenToWorldPoint(
                    new Vector3(touchscreen.activeTouches[i].position.ReadValue().x,
                        touchscreen.activeTouches[i].position.ReadValue().y,
                        m_MainCamera.nearClipPlane));


            // Accumulate average position.  Division happens later
            averagePosition += m_Touches[i].transform.position;
        }

        // Set average indicator value
        //
        averagePosition /= touchscreen.activeTouches.Count;
        m_AveragePositionMarker.transform.position = averagePosition;

        DeleteExtraTouches(touchscreen.activeTouches.Count);
    }

    void DeleteExtraTouches(int numActiveTouches)
    {
        if (numActiveTouches < m_Touches.Count)
        {
            for (int i = 0; i < m_Touches.Count; i++)
            {
                Destroy(m_Touches[i]);
                m_Touches.RemoveAt(i);
            }
        }
    }
}
