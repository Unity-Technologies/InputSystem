using System;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Query the ID and the name of the user paired to the device the command is sent to.
    /// </summary>
    /// <remarks>
    /// This command is only supported on platforms where devices can be paired to user accounts
    /// at the platform level. Currently this is the case for Xbox and PS4. On Switch, <see
    /// cref="InitiateUserAccountPairingCommand"/> is supported but the platform does not store
    /// associations established between devices and users that way.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = kSize)]
    public unsafe struct QueryPairedUserAccountCommand : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('P', 'A', 'C', 'C');

        internal const int kMaxNameLength = 256;
        internal const int kMaxIdLength = 256;

        ////REVIEW: is this too heavy to allocate on the stack?
        internal const int kSize = InputDeviceCommand.kBaseCommandSize + 8 + kMaxNameLength * 2 + kMaxIdLength * 2;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames", Justification = "`Result` matches other command result names")]
        [Flags]
        public enum Result : long
        {
            // Leave bit #0 unused so as to not lead to possible confusion with GenericSuccess.

            /// <summary>
            /// The device is currently paired to a user account.
            /// </summary>
            DevicePairedToUserAccount = 1 << 1,

            /// <summary>
            /// The system is currently displaying a prompt for the user to select an account to
            /// use the device with.
            /// </summary>
            UserAccountSelectionInProgress = 1 << 2,

            /// <summary>
            /// User account selection completed.
            /// </summary>
            UserAccountSelectionComplete = 1 << 3,

            /// <summary>
            /// The system had been displaying a prompt
            /// </summary>
            UserAccountSelectionCanceled = 1 << 4,
        }

        [FieldOffset(0)]
        public InputDeviceCommand baseCommand;

        /// <summary>
        /// Handle of the user account at the platform level.
        /// </summary>
        /// <remarks>
        /// Note that this is wide enough to store a pointer and does not necessarily need to be a plain integer.
        /// How the backend determines handles for user accounts is up to the backend.
        ///
        /// Be aware that a handle is not guaranteed to be valid beyond the current application run. For stable,
        /// persistent user account handles,use <see cref="id"/>.
        /// </remarks>
        [FieldOffset(InputDeviceCommand.kBaseCommandSize)]
        public ulong handle;

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 8)]
        internal fixed byte nameBuffer[kMaxNameLength * 2];

        [FieldOffset(InputDeviceCommand.kBaseCommandSize + 8 + kMaxNameLength * 2)]
        internal fixed byte idBuffer[kMaxNameLength * 2];

        /// <summary>
        /// Persistent ID of the user account the platform level.
        /// </summary>
        /// <remarks>
        /// This ID is guaranteed to not change between application runs, device restarts, and the user
        /// changing user names on the account.
        ///
        /// Use this ID to associate persistent settings with.
        /// </remarks>
        public string id
        {
            get
            {
                fixed(byte* idBufferPtr = idBuffer)
                return StringHelpers.ReadStringFromBuffer(new IntPtr(idBufferPtr), kMaxIdLength);
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                var length = value.Length;
                if (length > kMaxIdLength)
                    throw new ArgumentException($"ID '{value}' exceeds maximum supported length of {kMaxIdLength} characters", nameof(value));

                fixed(byte* idBufferPtr = idBuffer)
                {
                    StringHelpers.WriteStringToBuffer(value, new IntPtr(idBufferPtr), kMaxIdLength);
                }
            }
        }

        /// <summary>
        /// Name of the user account at the platform level.
        /// </summary>
        public string name
        {
            get
            {
                fixed(byte* nameBufferPtr = nameBuffer)
                return StringHelpers.ReadStringFromBuffer(new IntPtr(nameBufferPtr), kMaxNameLength);
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                var length = value.Length;
                if (length > kMaxNameLength)
                    throw new ArgumentException($"Name '{value}' exceeds maximum supported length of {kMaxNameLength} characters", nameof(value));

                fixed(byte* nameBufferPtr = nameBuffer)
                {
                    StringHelpers.WriteStringToBuffer(value, new IntPtr(nameBufferPtr), kMaxNameLength);
                }
            }
        }

        public FourCC typeStatic => Type;

        public static QueryPairedUserAccountCommand Create()
        {
            return new QueryPairedUserAccountCommand
            {
                baseCommand = new InputDeviceCommand(Type, kSize),
            };
        }
    }
}
