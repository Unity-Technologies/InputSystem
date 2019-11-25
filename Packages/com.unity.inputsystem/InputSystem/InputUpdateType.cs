using System;
using UnityEngine.InputSystem.Layouts;

////TODO: ManualThreaded

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Enum of different player loop positions where the input system can invoke it's update mechanism.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames", Justification = "Not consistently used as flags, many using APIs expect only one type to be passed.")]
    [Flags]
    public enum InputUpdateType
    {
        None = 0,

        /// <summary>
        /// Update corresponding to <see cref="MonoBehaviour.OnUpdate"/>.
        /// </summary>
        /// <remarks>
        /// Every frame has exactly one dynamic update. If not reconfigured using <see cref="PlayerLoop"/>,
        /// the dynamic update happens after all the fixed updates for the frame have run (which can be
        /// zero or more).
        ///
        /// Input updates run before script callbacks on MonoBehaviours are fired.
        /// </remarks>
        Dynamic = 1 << 0,

        /// <summary>
        /// Update corresponding to <see cref="MonoBehaviour.OnFixedUpdate"/>.
        /// </summary>
        /// <remarks>
        /// Every frame has zero or more fixed updates. These are run before the dynamic update for the
        /// frame.
        ///
        /// Input updates run before script callbacks on MonoBehaviours are fired.
        /// </remarks>
        Fixed = 1 << 1,

        ////REVIEW: Axe this update type from the public API?
        /// <summary>
        /// Input update that happens right before rendering.
        /// </summary>
        /// <remarks>
        /// The BeforeRender update affects only devices that have before-render updates enabled. This
        /// has to be done through a device's layout (<see cref="InputControlLayout.updateBeforeRender"/>
        /// and is visible through <see cref="InputDevice.updateBeforeRender"/>.
        ///
        /// BeforeRender updates are useful to minimize lag of transform data that is used in rendering
        /// but is coming from external tracking devices. An example are HMDs. If the head transform used
        /// for the render camera is not synchronized right before rendering, it can result in a noticeable
        /// lag between head and camera movement.
        /// </remarks>
        BeforeRender = 1 << 2,

        /// <summary>
        /// Input update that happens right before <see cref="UnityEditor.EditorWindow"/>s are updated.
        /// </summary>
        /// <remarks>
        /// This update only occurs in the editor. It is triggered right before <see cref="UnityEditor.EditorApplication.update"/>.
        /// </remarks>
        /// <seealso cref="UnityEditor.EditorApplication.update"/>
        Editor = 1 << 3,

        ////TODO
        Manual = 1 << 4,

        ////REVIEW: kill?
        Default = Dynamic | Fixed | Editor,
    }

    internal static class InputUpdate
    {
        public static InputUpdateType s_LastUpdateType;
        public static uint s_UpdateStepCount;
        public static uint s_LastUpdateRetainedEventBytes;
        public static uint s_LastUpdateRetainedEventCount;

        [Serializable]
        public struct SerializedState
        {
            public InputUpdateType lastUpdateType;
            public uint updateStepCount;
            public uint lastUpdateRetainedEventBytes;
            public uint lastUpdateRetainedEventCount;
        }

        public static SerializedState Save()
        {
            return new SerializedState
            {
                lastUpdateType = s_LastUpdateType,
                updateStepCount = s_UpdateStepCount,
                lastUpdateRetainedEventBytes = s_LastUpdateRetainedEventBytes,
                lastUpdateRetainedEventCount = s_LastUpdateRetainedEventCount,
            };
        }

        public static void Restore(SerializedState state)
        {
            s_LastUpdateType = state.lastUpdateType;
            s_UpdateStepCount = state.updateStepCount;
            s_LastUpdateRetainedEventBytes = state.lastUpdateRetainedEventBytes;
            s_LastUpdateRetainedEventCount = state.lastUpdateRetainedEventCount;
        }
    }
}
