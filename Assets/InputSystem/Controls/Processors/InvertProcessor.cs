namespace ISX
{
    public class InvertProcessor : IInputProcessor<float>
    {
        public float Process(float value)
        {
            return value * -1.0f;
        }
    }
}