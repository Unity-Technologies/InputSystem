using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;

namespace UnityEngine.InputSystem.Experimental
{
    [Serializable]
    public struct Vector2Control
    {
        [SerializeField] private Usage xUsage;
        [SerializeField] private Usage yUsage;
        
        public Vector2Control(Usage x, Usage y)
        {
            this.xUsage = x;
            this.yUsage = y;
        }
        
        public ObservableInput<float> x => new(Endpoint.FromUsage(xUsage));
        public ObservableInput<float> y => new(Endpoint.FromUsage(yUsage));
    }
}