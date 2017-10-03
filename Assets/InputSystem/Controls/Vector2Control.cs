using UnityEngine;

namespace ISX
{
    public class Vector2Control : InputControl<Vector2>
    {
        [InputControl(offset = 0)]
        public AxisControl x { get; private set; }
        [InputControl(offset = 4)]
        public AxisControl y { get; private set; }

        public override Vector2 value
        {
            get
            {
                return Process(new Vector2(x.value, y.value));
            }
        }

        protected override void FinishSetup(InputControlSetup setup)
        {
            x = setup.GetControl<AxisControl>(this, "x");
            y = setup.GetControl<AxisControl>(this, "y");
            base.FinishSetup(setup);
        }
    }
}
