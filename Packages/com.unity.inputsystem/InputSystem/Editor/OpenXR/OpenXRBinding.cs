using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.unity.InputSystem.Editor.OpenXR
{
	[Serializable]
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

	[Serializable]
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

	[Serializable]
    public enum OpenXRActionType
    {
        Bool,
        Float,
        Vector2,
        Pose
    }

	[Serializable]
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

    [Serializable]
    public class OpenXRConfiguration
    {
		[SerializeField]
	    private List<OpenXRSet> sets;

	    public OpenXRConfiguration()
	    {
		    sets = new List<OpenXRSet>();
	    }

	    public void Add(OpenXRSet set)
	    {
		    sets.Add(set);
	    }
    }
}
