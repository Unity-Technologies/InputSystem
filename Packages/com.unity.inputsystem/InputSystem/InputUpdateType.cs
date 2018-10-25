using System;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input
{
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
        /// Input update that happens right before <see cref="EditorWindow">EditorWindows</see> are updated.
        /// </summary>
        /// <remarks>
        /// This update only occurs in the editor.
        /// </remarks>
        Editor = 1 << 3,

        ////TODO
        Manual = 1 << 4,

        ////REVIEW: this will likely be a problem for the main thread queue; we can't block to access it; it's
        ////        designed for exclusive access by the main thread
        /// <summary>
        /// Variation of <see cref="Manual"/> that additionally allows calling <see cref="InputSystem.Update"/>
        /// on a thread other than the main thread.
        /// </summary>
        /// <remarks>
        /// Note that this mode doesn't mean the input system as a whole is thread-safe and can be accessed concurrently
        /// from multiple threads. Instead, what this mode permits is just to call <see cref="InputSystem.Update"/>
        /// on a thread other than the main thread. While the update is running, the executing thread must be the only
        /// one accessing the input system.
        /// </remarks>
        ManualThreaded = Manual | 1 << 5,

        Default = Dynamic | Fixed | Editor,
    }

    internal static class InputUpdate
    {
        public static InputUpdateType lastUpdateType;
        public static uint dynamicUpdateCount;
        public static uint fixedUpdateCount;

        [Serializable]
        public struct SerializedState
        {
            public InputUpdateType lastUpdateType;
            public uint dynamicUpdateCount;
            public uint fixedUpdateCount;
        }

        public static SerializedState Save()
        {
            return new SerializedState
            {
                lastUpdateType = lastUpdateType,
                dynamicUpdateCount = dynamicUpdateCount,
                fixedUpdateCount = fixedUpdateCount,
            };
        }

        public static void Restore(SerializedState state)
        {
            lastUpdateType = state.lastUpdateType;
            dynamicUpdateCount = state.dynamicUpdateCount;
            fixedUpdateCount = state.fixedUpdateCount;
        }
    }
}
