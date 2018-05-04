using System;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Dynamic execution state of an <see cref="InputActionMap"/>.
    /// </summary>
    internal struct InputActionMapState
    {
        public int controlCount;

        /// <summary>
        /// List of all resolved controls.
        /// </summary>
        /// <remarks>
        /// As we don't know in advance how many controls a binding may match (if any), we bump the size of
        /// this array in increments during resolution. This means it may be end up being larger than the total
        /// number of used controls and have empty entries at the end. Use <see cref="controlCount"/> and not
        /// <c>.Length</c> to find the actual number of controls.
        /// </remarks>
        public InputControl[] controls;

        /// <summary>
        /// Map an entry in <see cref="controls"/> to an entry in <see cref="bindingStates"/>.
        /// </summary>
        public int[] controlIndexToBindingIndex;

        public BindingState[] bindingStates;

        public ModifierState[] modifierStates;

        /// <summary>
        /// Records the current state of a single modifier attached to a binding.
        /// Each modifier keeps track of its own trigger control and phase progression.
        /// </summary>
        internal struct ModifierState
        {
            public IInputBindingModifier modifier;
            public InputControl triggerControl;
            public Flags flags;
            public double startTime;

            [Flags]
            public enum Flags
            {
                TimerRunning = 1 << 8, // Reserve first 8 bits for phase.
            }

            public bool isTimerRunning
            {
                get { return (flags & Flags.TimerRunning) == Flags.TimerRunning; }
                set
                {
                    if (value)
                        flags |= Flags.TimerRunning;
                    else
                        flags &= ~Flags.TimerRunning;
                }
            }

            public InputActionPhase phase
            {
                // We store the phase in the low 8 bits of the flags field.
                get { return (InputActionPhase)((int)flags & 0xf); }
                set { flags = (Flags)(((uint)flags & 0xfffffff0) | (uint)value); }
            }
        }

        /// <summary>
        /// Runtime state for a single binding.
        /// </summary>
        /// <remarks>
        /// Correlated to the <see cref="InputBinding"/> it corresponds to by the index in the binding
        /// array.
        /// </remarks>
        internal struct BindingState
        {
            [Flags]
            public enum Flags
            {
                ChainsWithNext = 1 << 0,
                EndOfChain = 1 << 1,
                PartOfComposite = 1 << 2,
            }

            /// <summary>
            /// Controls that the binding resolved to.
            /// </summary>
            /// <seealso cref="InputActionMapState.controls"/>
            public ReadOnlyArray<InputControl> controls;

            /// <summary>
            /// State of modifiers applied to the binding.
            /// </summary>
            public ReadWriteArray<ModifierState> modifiers;

            /// <summary>
            /// The action being triggered by the binding (if any).
            /// </summary>
            /// <remarks>
            /// For bindings that don't trigger actions, this is <c>null</c>.
            /// </remarks>
            public InputAction action;

            /// <summary>
            /// The composite that the binding is part of (if any).
            /// </summary>
            /// <remarks>
            /// </remarks>
            public object composite;

            public Flags flags;

            public bool chainsWithNext
            {
                get { return (flags & Flags.ChainsWithNext) == Flags.ChainsWithNext; }
                set
                {
                    if (value)
                        flags |= Flags.ChainsWithNext;
                    else
                        flags &= ~Flags.ChainsWithNext;
                }
            }

            public bool isEndOfChain
            {
                get { return (flags & Flags.EndOfChain) == Flags.EndOfChain; }
                set
                {
                    if (value)
                        flags |= Flags.EndOfChain;
                    else
                        flags &= ~Flags.EndOfChain;
                }
            }

            public bool isPartOfChain
            {
                get { return chainsWithNext || isEndOfChain; }
            }

            ////TODO: remove
            public bool isPartOfComposite
            {
                get { return (flags & Flags.PartOfComposite) == Flags.PartOfComposite; }
                set
                {
                    if (value)
                        flags |= Flags.PartOfComposite;
                    else
                        flags &= ~Flags.PartOfComposite;
                }
            }
        }

        /// <summary>
        /// Information about what triggered an action and how.
        /// </summary>
        public struct TriggerState
        {
            public InputActionPhase phase;
            public double time;
            public double startTime;
            public InputControl control;
            public int bindingIndex;
            public int modifierIndex;
        }
    }
}
