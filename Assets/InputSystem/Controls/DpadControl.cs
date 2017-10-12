using System;
using UnityEngine;

namespace ISX
{
    // A control made up of four discrete, directional buttons. Forms a vector
    // but can also be addressed as individual buttons. Is stored as two bits.
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
            m_StateBlock.format = InputStateBlock.kTypeBit;
        }

        public override Vector2 value
        {
            get
            {
                ////FIXME: this produces unnormalized vectors; we want diagonal vectors to still be unit vectors
                var upValue = up.isPressed ? 1.0f : 0.0f;
                var downValue = down.isPressed ? -1.0f : 0.0f;
                var leftValue = left.isPressed ? -1.0f : 0.0f;
                var rightValue = right.isPressed ? 1.0f : 0.0f;

                return Process(new Vector2(leftValue + rightValue, upValue + downValue));
            }
        }

        public override Vector2 previous
        {
            get
            {
                var upValue = up.isPressed ? 1.0f : 0.0f;
                var downValue = down.isPressed ? -1.0f : 0.0f;
                var leftValue = left.isPressed ? -1.0f : 0.0f;
                var rightValue = right.isPressed ? 1.0f : 0.0f;

                return Process(new Vector2(leftValue + rightValue, upValue + downValue));
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
