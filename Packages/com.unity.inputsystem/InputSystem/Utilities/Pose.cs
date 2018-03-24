using System;

namespace UnityEngine.Experimental.Input
{
    [Serializable]
    public struct Pose
    {
        public Vector3 translation;
        public Quaternion rotation;

        public Pose(Vector3 tr, Quaternion rt) { translation = tr; rotation = rt; }

        public override string ToString()
        {
            return String.Format("({0}, {1})", translation.ToString(), rotation.ToString());
        }

        public string ToString(string format)
        {
            return String.Format("({0}, {1})", translation.ToString(format), rotation.ToString(format));
        }

        public Pose GetTransformedBy(Pose lhs)
        {
            return new Pose
            {
                translation = lhs.translation + (lhs.rotation * translation),
                rotation = lhs.rotation * rotation
            };
        }

        public Pose GetTransformedBy(Transform lhs)
        {
            return new Pose
            {
                translation = lhs.TransformPoint(translation),
                rotation = lhs.rotation * rotation
            };
        }

        public static Pose identity
        {
            get
            {
                return new Pose
                {
                    translation = Vector3.zero,
                    rotation = Quaternion.identity
                };
            }
        }
    }
}
