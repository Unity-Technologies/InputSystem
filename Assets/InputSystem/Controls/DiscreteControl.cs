namespace InputSystem
{
	public class DiscreteControl : InputControl<int>
	{
		public DiscreteControl(string name)
			: base(name)
		{
			stateBlock.sizeInBits = sizeof(int)*8;
		}

		public override int value
		{
			get
			{
				unsafe
				{
					var buffer = (byte*) currentStatePtr;
					return Process(*((int*) &buffer[stateBlock.byteOffset]));
				}
			}
		}
	}
}