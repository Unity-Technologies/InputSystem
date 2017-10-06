using UnityEngine;

namespace ISX
{
    // Processes a Vector2 to apply deadzoning according to the magnitude of the vector (rather
    // than just clamping individual axes).
    public class DeadzoneProcessor : IInputProcessor<Vector2>
    {
        public float deadzone;

        public Vector2 Process(Vector2 vector)
        {
            var magnitude = vector.magnitude;
            if (magnitude < deadzone)
                return default(Vector2);
            return vector;
        }
    }
}
