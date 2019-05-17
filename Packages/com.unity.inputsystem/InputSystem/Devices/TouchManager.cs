using System;

//base this on InputHistory

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Helper to make tracking of touches easier.
    /// </summary>
    /// <remarks>
    /// This class obsoletes the need to manually track touches by ID and provides
    /// various helpers such as making history data of touches available.
    /// </remarks>
    public class TouchManager
    {
        /// <summary>
        /// The amount of history kept for each single touch.
        /// </summary>
        /// <remarks>
        /// By default, this is zero meaning that no history information is kept for
        /// touches. Setting this to <c>Int32.maxValue</c> will cause all history from
        /// the beginning to the end of a touch being kept.
        /// </remarks>
        public int maxHistoryLengthPerTouch
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public Action<Touch> onTouch
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public static TouchManager instance
        {
            get { throw new NotImplementedException(); }
        }

        private Touch[] m_TouchPool;
    }
}
