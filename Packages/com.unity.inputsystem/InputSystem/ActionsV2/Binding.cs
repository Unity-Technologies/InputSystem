using System.Collections.Generic;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.ActionsV2
{
	public enum BindingUpdateMode
	{
		Polling,
		EventDriven,
		Both
	}

    public class Binding<TValue> where TValue : struct
    {
	    private CompiledBindingPath m_CompiledBindingPath;
	    private IList<InputControl<TValue>> m_Controls;
	    private IList<IInputInteraction<TValue>> m_Interactions;
	    private IList<InputProcessor<TValue>> m_Processors;
	    private bool m_IsEnabled;
	    private InputControl<TValue> m_ActiveControl;

	    public string controlPath { get; set; }
	    public InputAction<TValue> action { get; }
	    public bool wantsInitialStateCheck { get; set; }
	    public IInputInteraction<TValue> activeInteraction { get; }
	    public BindingUpdateMode updateMode { get; set; }
		

	    //   public void ResolveControls(IBindingPathCompiler compiler)
	  //   {
		 //    m_CompiledBindingPath = compiler.Compile(controlPath);
	  //
			// m_Controls ??= new List<InputControl<TValue>>();
			// m_Controls.Clear();
		 //    m_CompiledBindingPath.FindMatchingControls(m_Controls);
	  //   }

	    public void OnControlChanged(InputControl<TValue> control, TValue newValue, double time)
	    {
			// TODO: take conflict resolution into account
		    m_ActiveControl = control;

		    var interactionContext = new InputInteractionContext<TValue>(action, control, action.phase, time, );

		    foreach (var interaction in m_Interactions)
		    {
			    interaction.Process(ref interactionContext);
		    }

		    if (control.CheckStateIsAtDefault())
		    {

		    }
		    action.ControlValueChanged(this, ApplyProcessors(newValue, control));
	    }

	    public void Enable()
	    {
		    if (m_IsEnabled)
			    return;

		    foreach (var inputControl in m_Controls)
		    {
				if(updateMode == BindingUpdateMode.EventDriven || updateMode == BindingUpdateMode.Both)
					inputControl.OnValueChanged += OnControlChanged;

			    if (wantsInitialStateCheck)
				    InputSystem.onBeforeUpdate += PerformInitialStateCheck;

			    InputSystem.onDeviceChange += (device, change) =>
			    {
				    if (change == InputDeviceChange.Added)
				    {
						// resolve controls for this binding
						// for any controls in the new list that aren't in the current list, hook them up if we're enabled
				    }
					else if (change == InputDeviceChange.Removed)
				    {
					    // resolve controls for this binding
					    // for any controls missing from the new list that are in the current list, disable and remove them
				    }
			    };
		    }

		    m_IsEnabled = true;
	    }

	    public void Disable()
	    {
		    foreach (var inputControl in m_Controls)
		    {
			    inputControl.OnValueChanged -= OnControlChanged;
		    }

		    m_IsEnabled = false;
	    }

	    private void PerformInitialStateCheck()
	    {
		    InputSystem.onBeforeUpdate -= PerformInitialStateCheck;

		    foreach (var control in m_Controls)
		    {
			    if (control.CheckStateIsAtDefault())
				    continue;

			    OnControlChanged(control, control.ReadValue(), InputState.currentTime);
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
    }

    internal struct CompiledBindingPath
    {
	    public BindingPathComponent device { get; set; }
	    public List<BindingPathComponent> controls { get; set; }

	    public int FindMatchingControls(IList<InputControl> controls)
	    {
		    foreach (var inputDevice in InputSystem.devices)
		    {
			    var numMatches = 0;
			    numMatches += m_PathNodes[0].ResolveControls(inputDevice, controls);
		    }
		    return 0;
	    }
    }

    internal struct BindingPathComponent
    {
	    public bool containsWildcard { get; set; }
	    public string value { get; set; }
	    public BindingPathComponentType type { get; set; }
    }

    internal enum BindingPathComponentType
    {
		Usage,
		Name,
		DisplayName
    }
}
