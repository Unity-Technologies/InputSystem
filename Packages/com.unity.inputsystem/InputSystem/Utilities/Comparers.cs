using System.Collections.Generic;

namespace UnityEngine.InputSystem.Utilitites
{
    public struct Vector2MagnitudeComparer : IComparer<Vector2>
    {
        public int Compare(Vector2 x, Vector2 y)
        {
            var lenx = x.sqrMagnitude;
            var leny = y.sqrMagnitude;

            if (lenx < leny)
                return -1;
            if (lenx > leny)
                return 1;
            return 0;
        }
    }

    public struct Vector3MagnitudeComparer : IComparer<Vector3>
    {
        public int Compare(Vector3 x, Vector3 y)
        {
            var lenx = x.sqrMagnitude;
            var leny = y.sqrMagnitude;

            if (lenx < leny)
                return -1;
            if (lenx > leny)
                return 1;
            return 0;
        }
    }
}
