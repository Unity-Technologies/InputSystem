using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.ActionsV2
{
    public delegate void ControlActuatedDelegate<TBinding, TControl>(ref IBinding<TBinding, TControl> binding, TBinding newValue,
        InputControl<TControl> inputControl, double time)
        where TBinding : struct
        where TControl : struct;

    public interface IBinding<TValue, TControl>
        where TValue : struct
        where TControl : struct
    {
        event ControlActuatedDelegate<TValue, TControl> ControlActuated;

        float EvaluateMagnitude();
        bool IsActuated(float magnitude);
        InputControl<TValue> activeControl { get; }
        TValue ReadValue();
        void Enable();
        void Disable();
    }

    public struct Binding<TValue> : IBinding<TValue, TValue> where TValue : struct
    {
        public event ControlActuatedDelegate<TValue, TValue> ControlActuated;

        private IList<InputControl<TValue>> m_Controls;
        private IList<InputProcessor<TValue>> m_Processors;
        private bool m_IsEnabled;
        private InputControl<TValue> m_ActiveControl;

        public Binding(string path)
        {
            m_Controls = new List<InputControl<TValue>>();
            m_Processors = new List<InputProcessor<TValue>>();
            m_IsEnabled = false;
            m_ActiveControl = null;
            ControlActuated = null;
            wantsInitialStateCheck = false;
            bindingPath = path;
            enableEvents = true;
        }

        public string bindingPath { get; set; }
        public bool wantsInitialStateCheck { get; set; }
        public bool enableEvents { get; set; }

        public InputControl<TValue> activeControl => m_ActiveControl;


        public void OnControlActuationChanged(InputControl<TValue> control, TValue newValue, double time)
        {
            m_ActiveControl = control;

            // TODO: fix this silly cast
            var binding = (IBinding<TValue, TValue>)this;
            ControlActuated?.Invoke(ref binding, ApplyProcessors(newValue, control), control, time);
        }

        public void Enable()
        {
            if (m_IsEnabled)
                return;

            var controls = new InputControlList<InputControl<TValue>>(Allocator.Temp);
            InputSystem.FindControls(bindingPath, ref controls);

            foreach (var control in controls)
            {
                if (!(control is InputControl<TValue>))
                {
                    Debug.LogWarning($"Binding '{bindingPath}' matched control '{control.name}' but the type of control ('{control.valueType}') " +
                                     $"does not match the type of the binding ('{typeof(TValue).Name}').");
                    continue;
                }

                m_Controls.Add(control);

                if (enableEvents)
                    control.ValueChanged += OnControlActuationChanged;

                if (wantsInitialStateCheck)
                    InputSystem.onBeforeUpdate += PerformInitialStateCheck;
            }
            controls.Dispose();

            InputSystem.onDeviceChange += OnDeviceChanged;
            m_IsEnabled = true;
        }

        public void Disable()
        {
            foreach (var inputControl in m_Controls)
            {
                inputControl.ValueChanged -= OnControlActuationChanged;
            }

            InputSystem.onDeviceChange -= OnDeviceChanged;

            m_IsEnabled = false;
        }

        public void OverrideBinding(string path)
        {
            foreach (var inputControl in m_Controls)
            {
                inputControl.ValueChanged -= OnControlActuationChanged;
            }
            m_Controls.Clear();

            var controls = new InputControlList<InputControl<TValue>>(Allocator.Temp);
            InputSystem.FindControls(path, ref controls);
            foreach (var control in controls)
            {
                if (!(control is InputControl<TValue>))
                {
                    Debug.LogWarning($"Binding '{bindingPath}' matched control '{control.name}' but the type of control ('{control.valueType}') " +
                                     $"does not match the type of the binding ('{typeof(TValue).Name}').");
                    continue;
                }

                m_Controls.Add(control);
                if (enableEvents)
                    control.ValueChanged += OnControlActuationChanged;
            }
        }

        private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added)
            {
                var tempControls = new InputControlList<InputControl>(Allocator.Temp);
                InputControlPath.TryFindControls(device, bindingPath, ref tempControls);
                foreach (var control in tempControls)
                {
                    if (control is InputControl<TValue> typedControl)
                    {
                        if (m_Controls.Contains(typedControl)) continue;

                        if (enableEvents)
                            typedControl.ValueChanged += OnControlActuationChanged;

                        m_Controls.Add(typedControl);
                    }
                    else
                    {
                        Debug.LogWarning($"Binding '{bindingPath}' matched control '{control.name}' but the type of control ('{control.valueType}') " +
                                         $"does not match the type of the binding ('{typeof(TValue).Name}').");
                    }
                }
                tempControls.Dispose();
            }
            else if (change == InputDeviceChange.Removed)
            {
                for (var i = m_Controls.Count - 1; i >= 0; i--)
                {
                    if (m_Controls[i].device == device)
                        m_Controls.RemoveAt(i);
                }
            }
        }

        private void PerformInitialStateCheck()
        {
            InputSystem.onBeforeUpdate -= PerformInitialStateCheck;

            foreach (var control in m_Controls)
            {
                if (control.CheckStateIsAtDefault())
                    continue;

                OnControlActuationChanged(control, control.ReadValue(), InputState.currentTime);
            }
        }

        public TValue ApplyProcessors(TValue newValue, InputControl<TValue> control = null)
        {
            foreach (var processor in m_Processors)
            {
                newValue = processor.Process(newValue, control);
            }

            return newValue;
        }

        public TValue ReadValue()
        {
            return ApplyProcessors(m_ActiveControl?.ReadValue() ?? default(TValue));
        }

        public float EvaluateMagnitude()
        {
            return m_ActiveControl.EvaluateMagnitude();
        }

        public bool IsActuated(float magnitude)
        {
            return m_ActiveControl.IsActuated(magnitude);
        }
    }

    internal struct BindingPathComponent
    {
        public BindingPathComponentPart[] parts;
    }

    internal struct BindingPathComponentPart
    {
        public StringSlice value;
        public bool containsWildcard;
        public BindingPathComponentType type;
    }

    internal struct StringSlice
    {
        public ushort offset;
        public ushort length;
    }

    [Flags]
    internal enum BindingPathComponentType
    {
        Usage = 1,
        Name = 2,
        DisplayName = 4,
        Layout = 8
    }
}
