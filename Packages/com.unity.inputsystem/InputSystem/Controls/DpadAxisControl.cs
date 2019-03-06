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

            // Set the state block to be the parent's state block. We don't use that to read
            // the axis directly (we call the parent control to do that), but we need to set
            // it up the actions know to monitor this memory for changes to the control.
            m_StateBlock = m_Parent.m_StateBlock;
        }

        public override unsafe float ReadUnprocessedValueFromState(void* statePtr)
        {
            var value = (m_Parent as DpadControl).ReadUnprocessedValueFromState(statePtr);
            return value[component];
        }
    }
}
