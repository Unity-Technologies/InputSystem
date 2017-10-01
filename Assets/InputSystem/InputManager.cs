using System;
using System.Collections.Generic;

namespace InputSystem
{
	[Serializable]
    public class InputManager
    {
        public static InputManager instance
        {
            get { return s_Instance; }
        }
        
        public void RegisterTemplate(string name, Type type)
        {
        }

	    public void RegisterProcessor(string name, Type type)
	    {
	    }

	    public void RegisterUsage(string name, string type, params string[] processors)
	    {
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
            if (s_Instance != null)
                throw new InvalidOperationException("There already is an InputManager! Can only have one instance.");
            s_Instance = this;
	        
	        m_Usages = new Dictionary<string, InputUsage>();
	        m_Templates = new Dictionary<string, InputTemplate>();
	        m_Processors = new Dictionary<string, Type>();
            
			// Register input types.
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

	        InputUsage.s_Usages = m_Usages;
	        InputTemplate.s_Templates = m_Templates;
        }
        
        internal void Destroy()
        {
            if (s_Instance == this)
                s_Instance = null;

	        InputUsage.s_Usages = null;
	        InputTemplate.s_Templates = null;
        }

        private static InputManager s_Instance;

	    private Dictionary<string, InputUsage> m_Usages;
	    private Dictionary<string, InputTemplate> m_Templates;
	    private Dictionary<string, Type> m_Processors;
    }
}
