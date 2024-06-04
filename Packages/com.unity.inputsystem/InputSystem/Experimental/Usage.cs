using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental
{
    public readonly struct Usage : IEquatable<Usage>
    {
        public static readonly Usage Invalid = new (0);
        
        public readonly uint Value;

        public Usage(uint value)
        {
            this.Value = value;
        }

        public bool Equals(Usage other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is Usage other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}