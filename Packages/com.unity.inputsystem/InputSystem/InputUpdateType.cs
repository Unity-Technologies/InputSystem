using System;
using UnityEngine.InputSystem.Layouts;

////TODO: ManualThreaded

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Enum of different player loop positions where the input system can invoke its update mechanism.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames", Justification = "Not consistently used as flags, many using APIs expect only one type to be passed.")]
    [Flags]
    public enum InputUpdateType
    {
        None = 0,

        /// <summary>
        /// Update corresponding to <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html">Update</a>.
        ///
        /// Every frame has exactly one dynamic update. If not reconfigured using <see cref="PlayerLoop"/>,
        /// the dynamic update happens after all the fixed updates for the frame have run (which can be
        /// zero or more).
        ///
        /// Input updates run before script callbacks on MonoBehaviours are fired.
        /// </summary>
        Dynamic = 1 << 0,

        /// <summary>
        /// Update corresponding to <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html">FixedUpdate</a>.
        ///
        /// Every frame has zero or more fixed updates. These are run before the dynamic update for the
        /// frame.
        ///
        /// Input updates run before script callbacks on MonoBehaviours are fired.
        /// </summary>
        Fixed = 1 << 1,

        ////REVIEW: Axe this update type from the public API?
        /// <summary>
        /// Input update that happens right before rendering.
        ///
        /// The BeforeRender update affects only devices that have before-render updates enabled. This
        /// has to be done through a device's layout (<see cref="InputControlLayout.updateBeforeRender"/>
        /// and is visible through <see cref="InputDevice.updateBeforeRender"/>.
        ///
        /// BeforeRender updates are useful to minimize lag of transform data that is used in rendering
        /// but is coming from external tracking devices. An example are HMDs. If the head transform used
        /// for the render camera is not synchronized right before rendering, it can result in a noticeable
        /// lag between head and camera movement.
        /// </summary>
        BeforeRender = 1 << 2,

        /// <summary>
        /// Input update that happens right before <see cref="UnityEditor.EditorWindow"/>s are updated.
        ///
        /// This update only occurs in the editor. It is triggered right before <see cref="UnityEditor.EditorApplication.update"/>.
        /// </summary>
        Editor = 1 << 3,

        /// <summary>
        /// Input updates do not happen automatically but have to be triggered manually by calling <see cref="InputSystem.Update"/>.
        /// </summary>
        Manual = 1 << 4,

        /// <summary>
        /// Default update mask. Combines <see cref="Dynamic"/>, <see cref="Fixed"/>, and <see cref="Editor"/>.
        /// </summary>
        Default = Dynamic | Fixed | Editor,
    }

    internal static class InputUpdate
    {
        public static uint s_UpdateStepCount; // read only, but kept as a variable for performance reasons
        public static InputUpdateType s_LastUpdateType;
        public static UpdateStepCount s_PlayerUpdateStepCount;
        #if UNITY_EDITOR
        public static UpdateStepCount s_EditorUpdateStepCount;
        #endif
        public static uint s_LastUpdateRetainedEventBytes;
        public static uint s_LastUpdateRetainedEventCount;

        [Serializable]
        public struct UpdateStepCount
        {
            private bool m_WasUpdated;

            public uint value { get; private set; }

            public void OnBeforeUpdate()
            {
                m_WasUpdated = true;
                value++;
            }

            public void OnUpdate()
            {
                // only increment if OnBeforeUpdate was not called
                if (!m_WasUpdated)
                    value++;
                m_WasUpdated = false;
            }
        };

        [Serializable]
        public struct SerializedState
        {
            public InputUpdateType lastUpdateType;
            public UpdateStepCount playerUpdateStepCount;
            #if UNITY_EDITOR
            public UpdateStepCount editorUpdateStepCount;
            #endif
            public uint lastUpdateRetainedEventBytes;
            public uint lastUpdateRetainedEventCount;
        }

        internal static void OnBeforeUpdate(InputUpdateType type)
        {
            s_LastUpdateType = type;
            switch (type)
            {
                case InputUpdateType.Dynamic:
                case InputUpdateType.Manual:
                case InputUpdateType.Fixed:
                    s_PlayerUpdateStepCount.OnBeforeUpdate();
                    s_UpdateStepCount = s_PlayerUpdateStepCount.value;
                    break;
                #if UNITY_EDITOR
                case InputUpdateType.Editor:
                    s_EditorUpdateStepCount.OnBeforeUpdate();
                    s_UpdateStepCount = s_EditorUpdateStepCount.value;
                    break;
                #endif
            }
        }

        internal static void OnUpdate(InputUpdateType type)
        {
            s_LastUpdateType = type;
            switch (type)
            {
                case InputUpdateType.Dynamic:
                case InputUpdateType.Manual:
                case InputUpdateType.Fixed:
                    s_PlayerUpdateStepCount.OnUpdate();
                    s_UpdateStepCount = s_PlayerUpdateStepCount.value;
                    break;
                #if UNITY_EDITOR
                case InputUpdateType.Editor:
                    s_EditorUpdateStepCount.OnUpdate();
                    s_UpdateStepCount = s_EditorUpdateStepCount.value;
                    break;
                #endif
            }
        }

        public static SerializedState Save()
        {
            return new SerializedState
            {
                lastUpdateType = s_LastUpdateType,
                playerUpdateStepCount = s_PlayerUpdateStepCount,
                #if UNITY_EDITOR
                editorUpdateStepCount = s_EditorUpdateStepCount,
                #endif
                lastUpdateRetainedEventBytes = s_LastUpdateRetainedEventBytes,
                lastUpdateRetainedEventCount = s_LastUpdateRetainedEventCount,
            };
        }

        public static void Restore(SerializedState state)
        {
            s_LastUpdateType = state.lastUpdateType;
            s_PlayerUpdateStepCount = state.playerUpdateStepCount;
            s_LastUpdateRetainedEventBytes = state.lastUpdateRetainedEventBytes;
            s_LastUpdateRetainedEventCount = state.lastUpdateRetainedEventCount;
            #if UNITY_EDITOR
            s_EditorUpdateStepCount = state.editorUpdateStepCount;
            #endif

            switch (s_LastUpdateType)
            {
                case InputUpdateType.Dynamic:
                case InputUpdateType.Manual:
                case InputUpdateType.Fixed:
                    s_UpdateStepCount = s_PlayerUpdateStepCount.value;
                    break;
                #if UNITY_EDITOR
                case InputUpdateType.Editor:
                    s_UpdateStepCount = s_EditorUpdateStepCount.value;
                    break;
                #endif
                default:
                    // if there was no previous update type, reset the counter
                    s_UpdateStepCount = 0;
                    break;
            }
        }

        public static InputUpdateType GetUpdateTypeForPlayer(this InputUpdateType mask)
        {
            if ((mask & InputUpdateType.Manual) != 0)
                return InputUpdateType.Manual;

            if ((mask & InputUpdateType.Dynamic) != 0)
                return InputUpdateType.Dynamic;

            if ((mask & InputUpdateType.Fixed) != 0)
                return InputUpdateType.Fixed;

            return InputUpdateType.None;
        }

        public static bool IsPlayerUpdate(this InputUpdateType updateType)
        {
            if (updateType == InputUpdateType.Editor)
                return false;
            return updateType != default;
        }
    }
}
