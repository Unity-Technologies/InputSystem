#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Utilities;

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
    [HelpURL(InputSystem.kDocUrl + "/manual/UISupport.html#multiplayer-uis")]
    public class MultiplayerEventSystem : EventSystem
    {
        [Tooltip("If set, only process mouse events for any game objects which are children of this game object.")]
        [SerializeField] private GameObject m_PlayerRoot;

        /// <summary>
        /// The root object of the UI hierarchy that belongs to the given player.
        /// </summary>
        /// <remarks>
        /// This can either be an entire <c>Canvas</c> or just part of the hierarchy of
        /// a specific <c>Canvas</c>.
        ///
        /// Note that if the given <c>GameObject</c> has a <c>CanvasGroup</c> component on it, its
        /// <c>interactable</c> property will be toggled back and forth by <see cref="MultiplayerEventSystem"/>.
        /// If no such component exists on the <c>GameObject</c>, one will be added automatically.
        ///
        /// Only the <c>CanvasGroup</c> corresponding to the <see cref="MultiplayerEventSystem"/> that is currently
        /// executing its <see cref="Update"/> method (or did so last) will have <c>interactable</c> set to true.
        /// In other words, only the UI hierarchy corresponding to the player that is currently running a UI
        /// update (or that did so last) can be interacted with.
        /// </remarks>
        public GameObject playerRoot
        {
            get => m_PlayerRoot;
            set
            {
                m_PlayerRoot = value;
                InitializeCanvasGroup();
            }
        }

        private CanvasGroup m_CanvasGroup;
        private bool m_CanvasGroupWasAddedByUs;

        private static int s_MultiplayerEventSystemCount;
        private static MultiplayerEventSystem[] s_MultiplayerEventSystems;

        protected override void OnEnable()
        {
            base.OnEnable();

            ArrayHelpers.AppendWithCapacity(ref s_MultiplayerEventSystems, ref s_MultiplayerEventSystemCount, this);

            InitializeCanvasGroup();
        }

        private void InitializeCanvasGroup()
        {
            if (m_PlayerRoot != null)
            {
                m_CanvasGroup = m_PlayerRoot.GetComponent<CanvasGroup>();
                if (m_CanvasGroup == null)
                {
                    m_CanvasGroup = m_PlayerRoot.AddComponent<CanvasGroup>();
                    m_CanvasGroupWasAddedByUs = true;
                }
                else
                    m_CanvasGroupWasAddedByUs = false;
            }
            else
            {
                m_CanvasGroup = null;
            }
        }

        protected override void OnDisable()
        {
            var index = s_MultiplayerEventSystems.IndexOfReference(this);
            if (index != -1)
                s_MultiplayerEventSystems.EraseAtWithCapacity(ref s_MultiplayerEventSystemCount, index);

            if (m_CanvasGroupWasAddedByUs)
                Destroy(m_CanvasGroup);

            m_CanvasGroup = default;
            m_CanvasGroupWasAddedByUs = default;

            base.OnDisable();
        }

        protected override void Update()
        {
            for (var i = 0; i < s_MultiplayerEventSystemCount; ++i)
            {
                var system = s_MultiplayerEventSystems[i];
                if (system.m_PlayerRoot == null)
                    continue;

                system.m_CanvasGroup.interactable = system == this;
            }

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
#endif
