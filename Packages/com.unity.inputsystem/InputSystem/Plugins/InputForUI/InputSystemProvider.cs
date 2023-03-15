using UnityEditor;
using UnityEngine;
using UnityEngine.InputForUI;
using Event = UnityEngine.InputForUI.Event;
using EventProvider = UnityEngine.InputForUI.EventProvider;

namespace InputSystem.Plugins.InputForUI
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    internal class InputSystemProvider : IEventProviderImpl
    {
        private InputEventPartialProvider _inputEventPartialProvider;

        static InputSystemProvider()
        {
            // disable for now
            EventProvider.SetInputSystemProvider(new InputSystemProvider());
        }

        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Bootstrap()
        {
            // will invoke static class constructor
        }

        public void Initialize()
        {
            _inputEventPartialProvider ??= new InputEventPartialProvider();
            _inputEventPartialProvider.Initialize();
        }

        public void Shutdown()
        {
        }

        public void Update()
        {
            _inputEventPartialProvider.Update();

            // TODO implement action mapping to axes
        }

        public void OnFocusChanged(bool focus)
        {
            _inputEventPartialProvider.OnFocusChanged(focus);
        }

        public bool RequestCurrentState(Event.Type type)
        {
            if (_inputEventPartialProvider.RequestCurrentState(type))
                return true;

            switch (type)
            {
                // TODO
                default:
                    return false;
            }
        }

        public uint playerCount => 1; // TODO
    }
}
