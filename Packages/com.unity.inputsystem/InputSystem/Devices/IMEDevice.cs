using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Input.LowLevel
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct IMEDeviceState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('I', 'M', 'E'); }
        }

        // Actual control
        [InputControl(layout = "Button")]
        public bool isSelected;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}

namespace UnityEngine.Experimental.Input
{
    public enum IMECompositionMode
    {
        Auto = 0,
        On,
        Off,
    }

    [InputControlLayout(stateType = typeof(IMEDeviceState))]
    public class IMEDevice : InputDevice
    {
        public static IMEDevice current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        ButtonControl isSelected;

        IMECompositionMode m_Mode;
        public IMECompositionMode mode
        {
            set
            {
                if (m_Mode != value)
                {
                    // TODO: How can I better handle errors here?
                    SetIMECompositionModeCommand command = SetIMECompositionModeCommand.Create(value);
                    if (ExecuteCommand(ref command) >= 0)
                    {
                        m_Mode = value;
                    }
                }
            }
            get
            {
                return m_Mode;
            }
        }

        Vector2 m_Position;
        public Vector2 position
        {
            set
            {
                if (m_Position != value)
                {
                    SetIMECursorPositionCommand command = SetIMECursorPositionCommand.Create(value);
                    if (ExecuteCommand(ref command) >= 0)
                    {
                        m_Position = value;
                    }
                }
            }
            get
            {
                return m_Position;
            }
        }

        public event Action<string> onIMECompositionChange
        {
            add { m_CompositionStringListeners.Append(value); }
            remove { m_CompositionStringListeners.Remove(value); }
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            isSelected = builder.GetControl<ButtonControl>("isSelected");
            base.FinishSetup(builder);
        }

        public override void OnIMEStringEvent(IMECompositionStringEvent imeEvent)
        {
            if(m_CompositionStringListeners.length > 0)
            {
                string imeString = imeEvent.AsString();
                for (var i = 0; i < m_CompositionStringListeners.length; ++i)
                    m_CompositionStringListeners[i](imeString);
            }
            
        }

        internal InlinedArray<Action<string>> m_CompositionStringListeners;
    }
}
