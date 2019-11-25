using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.UI
{
    /// <summary>
    /// A modified EventSystem class, which allows multiple players to have their own instances of a UI,
    /// each with it's own selection.
    /// </summary>
    /// <remarks>
    /// You can use the <see cref="playerRoot"/> property to specify a part of the hierarchy belonging to the current player.
    /// Mouse selection will ignore any game objects not within this hierarchy. For gamepad/keyboard selection, you need to make sure that
    /// the navigation links stay within the player's hierarchy.
    /// </remarks>
    public class MultiplayerEventSystem : EventSystem
    {
        [Tooltip("If set, only process mouse events for any game objects which are children of this game object.")]
        [SerializeField] private GameObject m_PlayerRoot;

        public GameObject playerRoot
        {
            get => m_PlayerRoot;
            set => m_PlayerRoot = value;
        }

        protected override void Update()
        {
            var originalCurrent = current;
            current = this; // in order to avoid reimplementing half of the EventSystem class, just temporarily assign this EventSystem to be the globally current one
            try
            {
                base.Update();
            }
            finally
            {
                current = originalCurrent;
            }
        }
    }
}
