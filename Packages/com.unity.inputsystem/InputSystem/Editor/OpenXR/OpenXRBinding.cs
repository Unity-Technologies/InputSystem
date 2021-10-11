using System.Collections.Generic;

namespace com.unity.InputSystem.Editor.OpenXR
{
    public struct OpenXRBinding
    {
        // /interaction_profiles/khr/simple_controller
	    public string interactionProfile;
	    public string binding;

	    public OpenXRBinding(string interactionProfile, string binding)
	    {
		    this.interactionProfile = interactionProfile;
		    this.binding = binding;
	    }
    }

    public struct OpenXRAction
    {
	    public string actionName;
	    public OpenXRActionType type;
	    public List<OpenXRBinding> bindings;

	    public OpenXRAction(string name, OpenXRActionType actionType)
	    {
		    this.actionName = name;
		    this.type = actionType;
		    bindings = new List<OpenXRBinding>();
	    }
    }

    public enum OpenXRActionType
    {
        Bool,
        Float,
        Vector2,
        Pose
    }

    public struct OpenXRSet
    {
	    public int priority;
	    public string name;
	    public List<OpenXRAction> actions;

	    public OpenXRSet(string name, int priority)
	    {
		    this.name = name;
		    this.priority = priority;
		    actions = new List<OpenXRAction>();
	    }
    }
}
