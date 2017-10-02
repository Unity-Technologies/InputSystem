using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISX
{
	// The hub of the input system.
	// Not exposed. Use InputSystem as the public entry point to the system.
#if UNITY_EDITOR
	[Serializable]
#endif
    internal class InputManager
#if UNITY_EDITOR
		: ISerializationCallbackReceiver
#endif
    {
	    // Add a template constructed from a type.
	    // If a template with the same name already exists, the new template
	    // takes its place.
        public void RegisterTemplate(string name, Type type)
        {
	        if (string.IsNullOrEmpty(name))
		        throw new ArgumentException(nameof(name));
	        if (type == null)
		        throw new ArgumentNullException(nameof(type));

	        var template = InputTemplate.FromType(name, type);
	        
	        RegisterTemplate(template);
        }

	    // Add a template. If a template with the same name already exists, the new template
	    // takes its place.
	    public void RegisterTemplate(InputTemplate template)
	    {
		    if (template == null)
			    throw new ArgumentNullException(nameof(template));

		    m_Templates[template.name.ToLower()] = template;
	    }

	    public void RegisterProcessor(string name, Type type)
	    {
	    }

	    public void RegisterUsage(string name, string type, params string[] processors)
	    {
	    }

	    public InputTemplate TryGetTemplate(string name)
	    {
		    if (string.IsNullOrEmpty(name))
			    throw new ArgumentException(nameof(name));

		    InputTemplate template;
		    if (m_Templates.TryGetValue(name.ToLower(), out template))
			    return template;
		    
		    return null;
	    }
	    
	    // Processes a path specification that may match more than a single control.
	    // Adds all controls that match to the given list.
	    // Returns true if at least one control was matched.
	    // Must not generate garbage!
	    public bool TryGetControls(string path, List<InputControl> controls)
	    {
		    throw new NotImplementedException();
	    }

	    // Must not generate garbage!
	    public InputControl TryGetControl(string path)
	    {
		    throw new NotImplementedException();
	    }

	    public InputControl GetControl(string name)
	    {
		    throw new NotImplementedException();
	    }

        internal void Initialize()
        {
	        m_Usages = new Dictionary<string, InputUsage>();
	        m_Templates = new Dictionary<string, InputTemplate>();
	        m_Processors = new Dictionary<string, Type>();
            
			// Register templates.
			RegisterTemplate("Button", typeof(ButtonControl)); // Inputs.
			RegisterTemplate("Axis", typeof(AxisControl));
			RegisterTemplate("Analog", typeof(AxisControl));
			RegisterTemplate("Digital", typeof(DiscreteControl));
			RegisterTemplate("Vector2", typeof(Vector2Control));
			RegisterTemplate("Vector3", typeof(Vector3Control));
			RegisterTemplate("Magnitude2", typeof(Magnitude2Control));
			RegisterTemplate("Magnitude3", typeof(Magnitude3Control));
			RegisterTemplate("Quaternion", typeof(QuaternionControl));
			RegisterTemplate("Pose", typeof(PoseControl));
			RegisterTemplate("Stick", typeof(StickControl));
			RegisterTemplate("Dpad", typeof(DpadControl));

			RegisterTemplate("Motor", typeof(MotorControl)); // Outputs.

			RegisterTemplate("Gamepad", typeof(Gamepad)); // Devices.
			RegisterTemplate("Keyboard", typeof(Keyboard));
	        
			// Register processors.
			RegisterProcessor("Invert", typeof(InvertProcessor));
			RegisterProcessor("Clamp", typeof(ClampProcessor));
			RegisterProcessor("Normalize", typeof(NormalizeProcessor));
			RegisterProcessor("Deadzone", typeof(DeadzoneProcessor));
	        
			// Register usages.
			RegisterUsage("PrimaryStick", "Stick");
			RegisterUsage("SecondaryStick", "Stick");
			RegisterUsage("PrimaryAction", "Button");
			RegisterUsage("SecondaryAction", "Button");
			RegisterUsage("PrimaryTrigger", "Axis", "Normalized(0,1)");
			RegisterUsage("SecondaryTrigger", "Axis", "Normalized(0,1)");
			RegisterUsage("Back", "Button");
			RegisterUsage("Forward", "Button");
			RegisterUsage("Menu", "Button");
			RegisterUsage("Enter", "Button"); // Commit/confirm.
			RegisterUsage("Previous", "Button");
			RegisterUsage("Next", "Button");
			RegisterUsage("ScrollHorizontal", "Axis");
			RegisterUsage("ScrollVertical", "Axis");
			RegisterUsage("Pressure", "Axis", "Normalized(0,1)");
			RegisterUsage("Position", "Vector3");
			RegisterUsage("Orientation", "Quaternion");
			RegisterUsage("Point", "Vector2");
				
			RegisterUsage("LowFreqMotor", "Axis", "Normalized(0,1)");
			RegisterUsage("HighFreqMotor", "Axis", "Normalized(0,1)");

			RegisterUsage("LeftHand", "XRController");
			RegisterUsage("RightHand", "XRController");

	        InitializeStatics();
        }

	    // In the editor, we need to redo these steps after domain reloads.
	    internal void InitializeStatics()
	    {
		    if (m_Usages == null || m_Templates == null)
			    throw new InvalidOperationException("InputManager has lost its state");
		    
	        InputUsage.s_Usages = m_Usages;
	        InputTemplate.s_Templates = m_Templates;
	    }
        
        internal void Destroy()
        {
	        InputUsage.s_Usages = null;
	        InputTemplate.s_Templates = null;
        }

	    private Dictionary<string, InputUsage> m_Usages;
	    private Dictionary<string, InputTemplate> m_Templates;
	    private Dictionary<string, Type> m_Processors;
	    
	    // Domain reload survival logic.
#if UNITY_EDITOR
	    [Serializable]
	    internal struct SerializedState
	    {
		    public InputUsage[] usages;
		    public InputTemplate[] templates;
	    }

	    [SerializeField] private SerializedState m_SerializedState;
	    
	    public void OnBeforeSerialize()
	    {
		    var usageCount = m_Usages.Count;
		    var usageArray = new InputUsage[usageCount];

		    var i = 0;
		    foreach (var usage in m_Usages.Values)
			    usageArray[i++] = usage;

		    var templateCount = m_Templates.Count;
		    var templateArray = new InputTemplate[templateCount];

		    i = 0;
		    foreach (var template in m_Templates.Values)
			    templateArray[i++] = template;

		    m_SerializedState = new SerializedState
		    {
				usages = usageArray,
			    templates = templateArray
		    };
	    }

	    public void OnAfterDeserialize()
	    {
		    m_Usages = new Dictionary<string, InputUsage>();
		    m_Templates = new Dictionary<string, InputTemplate>();
		    m_Processors = new Dictionary<string, Type>();

		    foreach (var usage in m_SerializedState.usages)
			    m_Usages[usage.name.ToLower()] = usage;

		    foreach (var template in m_SerializedState.templates)
			    m_Templates[template.name.ToLower()] = template;

		    m_SerializedState = default(SerializedState);
	    }
#endif
    }
}
