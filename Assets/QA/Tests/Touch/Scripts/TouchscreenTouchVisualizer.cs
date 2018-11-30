using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input;

public class TouchscreenTouchVisualizer : MonoBehaviour
{
    [Tooltip("Prefab to use for the average position")]
    public GameObject averagePositionIndicatorPrefab;
    [Tooltip("Prefab to use for each individual touch")]
    public GameObject touchPositionIndicatorPrefab;

    private GameObject m_AveragePositionMarker;
    private List<GameObject> m_Touches;
    private Camera m_MainCamera;

    public void Start()
    {
        m_Touches = new List<GameObject>();

        m_AveragePositionMarker = Instantiate<GameObject>(averagePositionIndicatorPrefab);
        m_AveragePositionMarker.SetActive(false);

        m_MainCamera = Camera.main;
    }

    public void Update()
    {
        var touchscreen = InputSystem.GetDevice<Touchscreen>();
        var averagePosition = Vector3.zero;

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

        m_AveragePositionMarker.SetActive(true);

        // Set touch indicator position values, creating them if necessary
        // Also start accumulating average position
        //
        for (var i = 0; i < touchscreen.activeTouches.Count; i++)
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

    private void DeleteExtraTouches(int numActiveTouches)
    {
        if (numActiveTouches >= m_Touches.Count)
            return;

        for (var i = 0; i < m_Touches.Count; i++)
        {
            Destroy(m_Touches[i]);
            m_Touches.RemoveAt(i);
        }
    }
}
