namespace ISX
{
	public class DiscreteControl : InputControl<int>
	{
		public DiscreteControl()
		{
			m_StateBlock.sizeInBits = sizeof(int)*8;
		}

		public override int value
		{
			get
			{
				unsafe
				{
					var buffer = (byte*) currentStatePtr;
					return Process(*((int*) &buffer[m_StateBlock.byteOffset]));
				}
			}
		}
	}
}