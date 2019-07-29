using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchscreenTouchVisualizer : MonoBehaviour
{
    [Tooltip("Prefab to use for the average position")]
    public GameObject averagePositionIndicatorPrefab;
    [Tooltip("Prefab to use for each individual touch")]
    public GameObject touchPositionIndicatorPrefab;

    private GameObject m_AveragePositionMarker;
    private List<GameObject> m_Touches;
    private Camera m_MainCamera;

    public void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    public void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    public void Start()
    {
        m_Touches = new List<GameObject>();

        m_AveragePositionMarker = Instantiate<GameObject>(averagePositionIndicatorPrefab);
        m_AveragePositionMarker.SetActive(false);

        m_MainCamera = Camera.main;
    }

    public void Update()
    {
        var averagePosition = Vector3.zero;

        if (Touch.activeTouches.Count == 0)
        {
            m_AveragePositionMarker.SetActive(false);
            DeleteExtraTouches(Touch.activeTouches.Count);
            return;
        }

        m_AveragePositionMarker.SetActive(true);

        // Set touch indicator position values, creating them if necessary
        // Also start accumulating average position
        //
        for (var i = 0; i < Touch.activeTouches.Count; i++)
        {
            // Create new indicator if necessary
            if (i >= m_Touches.Count)
            {
                m_Touches.Add(Instantiate<GameObject>(touchPositionIndicatorPrefab));
            }

            m_Touches[i].transform.position = m_MainCamera.ScreenToWorldPoint(
                new Vector3(Touch.activeTouches[i].screenPosition.x,
                    Touch.activeTouches[i].screenPosition.y,
                    m_MainCamera.nearClipPlane));


            // Accumulate average position.  Division happens later
            averagePosition += m_Touches[i].transform.position;
        }

        // Set average indicator value
        //
        averagePosition /= Touch.activeTouches.Count;
        m_AveragePositionMarker.transform.position = averagePosition;

        DeleteExtraTouches(Touch.activeTouches.Count);
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
