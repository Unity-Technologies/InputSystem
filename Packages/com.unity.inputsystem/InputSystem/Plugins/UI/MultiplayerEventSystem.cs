#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.UI
{
    /// <summary>
    /// A modified EventSystem class, which allows multiple players to have their own instances of a UI,
    /// each with it's own selection.
    /// </summary>
    /// <remarks>
    /// You can use the <see cref="playerRoot"/> property to specify a part of the hierarchy belonging to the current player.
    /// Mouse selection will ignore any game objects not within this hierarchy, and all other navigation, using keyboard or
    /// gamepad for example, will be constrained to game objects under that hierarchy.
    /// </remarks>
    [HelpURL(InputSystem.kDocUrl + "/manual/UISupport.html#multiplayer-uis")]
    public class MultiplayerEventSystem : EventSystem
    {
        [Tooltip("If set, only process mouse and navigation events for any game objects which are children of this game object.")]
        [SerializeField] private GameObject m_PlayerRoot;

        /// <summary>
        /// The root object of the UI hierarchy that belongs to the given player.
        /// </summary>
        /// <remarks>
        /// This can either be an entire <c>Canvas</c> or just part of the hierarchy of
        /// a specific <c>Canvas</c>.
        /// </remarks>
        public GameObject playerRoot
        {
            get => m_PlayerRoot;
            set
            {
                m_PlayerRoot = value;
                InitializePlayerRoot();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            InitializePlayerRoot();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        private void InitializePlayerRoot()
        {
            if (m_PlayerRoot == null) return;

            var inputModule = GetComponent<InputSystemUIInputModule>();
            if (inputModule != null)
                inputModule.localMultiPlayerRoot = m_PlayerRoot;
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
#endif
