using UnityEngine;

namespace ISX
{
	// A control made up of four discrete, directional buttons. Forms a vector
	// but can also be addressed as individual buttons.
	public class DpadControl : InputControl<Vector2>
	{
		public enum ButtonBits
		{
			Up,
			Down,
			Left,
			Right,
		}
		
		[InputControl(bit = (int)ButtonBits.Up)]
		public ButtonControl up { get; private set; }
		[InputControl(bit = (int)ButtonBits.Down)]
		public ButtonControl down { get; private set; }
		[InputControl(bit = (int)ButtonBits.Left)]
		public ButtonControl left { get; private set; }
		[InputControl(bit = (int)ButtonBits.Right)]
		public ButtonControl right { get; private set; }
		
		////REVIEW: should have X and Y child controls as well

		public DpadControl()
		{
			m_StateBlock.sizeInBits = 4;
		}

		public override Vector2 value
        {
			get
			{
				unsafe
				{
					var upValue = up.value ? 1.0f : 0.0f;
					var downValue = down.value ? -1.0f : 0.0f;
					var leftValue = left.value ? -1.0f : 0.0f;
					var rightValue = right.value ? 1.0f : 0.0f;

					return Process(new Vector2(leftValue + rightValue, upValue + downValue));
				}
			}
        }

	    protected override void FinishSetup(InputControlSetup setup)
	    {
		    up = setup.GetControl<ButtonControl>(this, "up");
		    down = setup.GetControl<ButtonControl>(this, "down");
		    left = setup.GetControl<ButtonControl>(this, "left");
		    right = setup.GetControl<ButtonControl>(this, "right");
		    base.FinishSetup(setup);
	    }
	}
}