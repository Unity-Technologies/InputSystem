#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

////TODO: custom icon for OnScreenButton component

namespace UnityEngine.InputSystem.OnScreen
{
    /// <summary>
    /// A button that is visually represented on-screen and triggered by touch or other pointer
    /// input.
    /// </summary>
    [AddComponentMenu("Input/On-Screen Button")]
    [HelpURL(InputSystem.kDocUrl + "/manual/OnScreen.html#on-screen-buttons")]
    public class OnScreenButton : OnScreenControl, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerUp(PointerEventData eventData)
        {
            SendValueToControl(0.0f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SendValueToControl(1.0f);
        }

        ////TODO: pressure support
        /*
        /// <summary>
        /// If true, the button's value is driven from the pressure value of touch or pen input.
        /// </summary>
        /// <remarks>
        /// This essentially allows having trigger-like buttons as on-screen controls.
        /// </remarks>
        [SerializeField] private bool m_UsePressure;
        */

        [InputControl(layout = "Button")]
        [SerializeField]
        private string m_ControlPath;

        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(OnScreenButton))]
        internal class OnScreenButtonEditor : UnityEditor.Editor
        {
            private UnityEditor.SerializedProperty m_ControlPathInternal;

            public void OnEnable()
            {
                m_ControlPathInternal = serializedObject.FindProperty(nameof(OnScreenButton.m_ControlPath));
            }

            public void OnDisable()
            {
                new InputComponentEditorAnalytic(InputSystemComponent.OnScreenButton).Send();
            }

            public override void OnInspectorGUI()
            {
                // Current implementation has UGUI dependencies (ISXB-915, ISXB-916)
                UGUIOnScreenControlEditorUtils.ShowWarningIfNotPartOfCanvasHierarchy((OnScreenButton)target);

                UnityEditor.EditorGUILayout.PropertyField(m_ControlPathInternal);

                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}
#endif
