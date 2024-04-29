using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using InputSystem.Experimental;
using UnityEditor;

namespace UnityEngine.InputSystem.Experimental.Interfaces
{
    enum StandardGamepadButton
    {
        ButtonSouth = 1
    }

    public class DeviceInterface
    {
        private unsafe Stream* m_Stream;
        
        
    }
    
    // NOTE: This would be generated from native representation
    public class StandardGamepad : IResolveDeviceInterface<StandardGamepad> // Consider taking from pool?
    {
        private unsafe Stream* m_Stream;
        
        static StandardGamepad()
        {
            Host.instance.RegisterType(typeof(StandardGamepad), (stream) => new StandardGamepad(stream));
        }

        public StandardGamepad()
        {
            
        }

        internal unsafe StandardGamepad(WrappedStream stream)
        {
            m_Stream = stream.stream;
        }
            
        [StructLayout(LayoutKind.Explicit)]
        private struct State
        {
            [FieldOffset(0)] public uint value;
        }
        
        //public bool buttonSouth() => (value & (uint)StandardGamepadButton.ButtonSouth) != 0;

        //public unsafe bool isDown(StandardGamepadButton button) => (m_State->value & (uint)button) != 0;
        
        public unsafe Reader<StandardGamepad> Subscribe()
        {
            return new Reader<StandardGamepad>(Context.instance, m_Stream);
        }
        
        public event Callback OnChange
        {
            add => Context.instance.RegisterCallback(value);
            remove => Context.instance.UnregisterCallback(value);
        }

        /*readonly struct StandardGamepadReader
        {
            private unsafe StandardGamepadReader(Stream* stream) { m_Reader = new Reader<State>(stream); }
            public void Read(ref State state) { m_Reader.Read(ref state); }
            private readonly Reader<StandardGamepad.State> m_Reader;
            
            public unsafe bool buttonSouth => m_Reader.m_;
        }*/
        
        public unsafe Reader<StandardGamepad> CreateReader()
        {
            return new Reader<StandardGamepad>(Context.instance, m_Stream);
        }
            
        public unsafe Reader<StandardGamepad> CreateReader(Callback callback)
        {
            return new Reader<StandardGamepad>(Context.instance, m_Stream);
        }
            
        // Extensions: Not adding more API, only for convenience
            
        /*public static StandardGamepad GetDevice(int index)
        {
            var device = Context.
            var device = Device.GetDevice(0);
            return device.GetDeviceInterface<Interfaces.StandardGamepad>();
        }*/
    }
}