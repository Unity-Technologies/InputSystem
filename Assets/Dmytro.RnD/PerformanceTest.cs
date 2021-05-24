using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PerformanceTest : MonoBehaviour
{
    private TestAsset TestAsset;
    private Vector2 LatestValue1;
    private Vector2 LatestValue2;

    void Start()
    {
        TestAsset = new TestAsset();
        TestAsset.Enable();

        TestAsset.testmap.testaction.performed += context =>
        {
            LatestValue1 = context.ReadValue<Vector2>();
        };

        UnityEngine.InputSystem.DmytroRnD.Core.s_ValueCallback = vector2 =>
        {
            LatestValue2 = vector2;
        };
    }

    void Update()
    {
        
    }
}
