using System;
using ISX.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

////REVIEW: we can't get shift and alt symbols on all platforms (Windows won't play, for example); worth even keeping it in the API?

namespace ISX
{
    /// <summary>
    /// A key on a <see cref="Keyboard"/>.
    /// </summary>
    /// <remarks>
    /// This is an extended button control which adds various features to account for the fact that keys
    /// have symbols associated with them which may change depending on keyboard layout as well as in combination
    /// with other keys.
    ///
    /// Note that there is no text input associated with individual keys as text composition is highly
    /// layout specific and does not need to be key-by-key. For general text input, see <see cref="Keyboard.onTextInput"/>.
    /// To find the text displayed on a key, use <see cref="KeyControl.displayName"/>.
    /// </remarks>
    public class KeyControl : ButtonControl
    {
        public static FourCC IOCTLGetKeyConfig { get { return new FourCC('K', 'Y', 'C', 'F'); } }

        /// <summary>
        /// The code used in Unity to identify the key.
        /// </summary>
        /// <remarks>
        /// This property must be initialized by <see cref="InputControl.FinishControlSetup"/> of
        /// the device owning the control.
        /// </remarks>
        public Key keyCode { get; set; }

        ////REVIEW: rename this to something like platformKeyCode? We're not really dealing with scan code here.
        /// <summary>
        /// The code that the underlying platform uses to identify the key.
        /// </summary>
        public int scanCode
        {
            get
            {
                RefreshConfigurationIfNeeded();
                return m_ScanCode;
            }
        }

        /// <summary>
        /// Display name for key when used in combination with the shift key.
        /// </summary>
        public string shiftDisplayName
        {
            get
            {
                RefreshConfigurationIfNeeded();
                return m_ShiftDisplayName;
            }
        }

        /// <summary>
        /// Display name for key when used in combination with the alt key.
        /// </summary>
        public string altDisplayName
        {
            get
            {
                RefreshConfigurationIfNeeded();
                return m_AltDisplayName;
            }
        }

        protected override unsafe void RefreshConfiguration()
        {
            // Wipe our last cached set of data (if any).
            displayName = null;
            m_ScanCode = 0;
            m_AltDisplayName = null;
            m_ShiftDisplayName = null;

            const int kMaxBufferSize = 256;

            ////TODO: remove the allocation here and put things on the stack with a fixed byte buffer in a struct

            // Allocate memory buffer.
            var buffer = UnsafeUtility.Malloc(kMaxBufferSize, 4, Allocator.Temp);
            try
            {
                // Write key code buffer for device to know which key we want.
                *((int*)buffer) = (int)keyCode;

                // Read key configuration data from device.
                var numBytesRead = device.IOCTL(IOCTLGetKeyConfig, new IntPtr(buffer), kMaxBufferSize);
                if (numBytesRead < sizeof(int) * 4)
                {
                    // Got nothing. Device probably does not support key configuration data.
                    return;
                }

                ////REVIEW: We could write code that does the stuff here automatically by just taking a plain
                ////        C# struct describing the data. However, it's difficult to do that a) without
                ////        allocating memory and b) efficiently using reflection.

                // Key configuration is sent in a variable-size block. First field
                // is an int that is the scan code for the key. Then come three strings
                // each of which is an int field denoting length followed by a number of
                // characters equal to length (no null terminator). First string is the
                // key's symbol, second string is the shift symbol, third string is the
                // alt symbol.
                m_ScanCode = *((int*)buffer);
                var offset = 4u;
                displayName = StringHelpers.ReadStringFromBuffer(new IntPtr(buffer), kMaxBufferSize, ref offset);
                m_ShiftDisplayName = StringHelpers.ReadStringFromBuffer(new IntPtr(buffer), kMaxBufferSize, ref offset);
                m_AltDisplayName = StringHelpers.ReadStringFromBuffer(new IntPtr(buffer), kMaxBufferSize, ref offset);
            }
            finally
            {
                UnsafeUtility.Free(buffer, Allocator.Temp);
            }
        }

        // Cached configuration data for the key. We fetch this from the
        // device on demand.
        private int m_ScanCode;
        private string m_ShiftDisplayName;
        private string m_AltDisplayName;
    }
}
