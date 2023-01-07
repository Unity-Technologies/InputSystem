using System.ComponentModel;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Processors
{
    [DesignTimeVisible(false)]
    internal class CompensateRotationProcessor : InputProcessor<Quaternion>
    {
        public override Quaternion Process(Quaternion value, InputControl control)
        {
            if (!InputSystem.settings.compensateForScreenOrientation)
                return value;

            const float kSqrtOfTwo = 1.4142135623731f;
            var q = Quaternion.identity;

            switch (InputRuntime.s_Instance.screenOrientation)
            {
                case ScreenOrientation.PortraitUpsideDown: q = new Quaternion(0.0f, 0.0f, 1.0f /*sin(pi/2)*/, 0.0f /*cos(pi/2)*/); break;
                case ScreenOrientation.LandscapeLeft:      q = new Quaternion(0.0f, 0.0f, kSqrtOfTwo * 0.5f /*sin(pi/4)*/, -kSqrtOfTwo * 0.5f /*cos(pi/4)*/); break;
                case ScreenOrientation.LandscapeRight:     q = new Quaternion(0.0f, 0.0f, -kSqrtOfTwo * 0.5f /*sin(3pi/4)*/, -kSqrtOfTwo * 0.5f /*cos(3pi/4)*/); break;
            }

            return value * q;
        }

        public override string ToString()
        {
            return "CompensateRotation()";
        }

        public override CachingPolicy cachingPolicy => CachingPolicy.EvaluateOnEveryRead;
    }
}
