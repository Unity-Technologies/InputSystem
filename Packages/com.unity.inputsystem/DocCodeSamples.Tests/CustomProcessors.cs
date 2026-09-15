using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;

namespace DocCodeSamples.Tests
{
    #region registernewprocessor
    #if UNITY_EDITOR
    [InitializeOnLoad]
    #endif
    /// <summary>
    /// Example custom processor that registers itself with the Input System.
    /// </summary>
    public class MyValueShiftProcessor : InputProcessor<float>
    {
        #if UNITY_EDITOR
        static MyValueShiftProcessor()
        {
            Initialize();
        }

        #endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            InputSystem.RegisterProcessor<MyValueShiftProcessor>();
        }

        /// <summary>
        /// Returns <paramref name="value"/> unchanged.
        /// </summary>
        /// <param name="value">Value to process.</param>
        /// <param name="control">Control from which the value originates.</param>
        /// <returns>The unchanged value.</returns>
        public override float Process(float value, InputControl control)
        {
            return value;
        }
    }
    #endregion

    class ProcessorExamples : MonoBehaviour
    {
        void Start()
        {
            #region inputactionwithprocessor
            var action = new InputAction(processors: "myvalueshift(valueShift=2.3)");
            #endregion
        }

        void ConfigureProcessorBinding()
        {
            #region processorbindings
            var action = new InputAction();
            action.AddBinding("<Gamepad>/leftStick")
                .WithProcessor("invertVector2(invertX=false)");
            #endregion
        }

        void AddProcessor()
        {
            #region addprocessor
            var action = new InputAction(processors: "invertVector2(invertX=false)");
            #endregion
        }
    }
}
