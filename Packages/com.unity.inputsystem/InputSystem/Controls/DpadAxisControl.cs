using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Controls
{
    public class DpadAxisControl : AxisControl
    {
        public int component;

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);
            component = name == "x" ? 0 : 1;
        }

        public override unsafe float ReadUnprocessedValueFromState(void* statePtr)
        {
            var value = (m_Parent as DpadControl).ReadUnprocessedValueFromState(statePtr);
            return value[component];
        }
    }
}
