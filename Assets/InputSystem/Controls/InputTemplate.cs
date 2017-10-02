using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

////REVIEW: rename InputControlTemplate?

namespace ISX
{
	// A template lays out the composition of an input control.
	//
	// Can be created in one of three ways:
	//	1) Manually in code through InputTemplateBuilder.
	// 	2) Loaded from JSON.
	//  3) Constructed through reflection from InputControls classes.
	//
	// Once constructed, templates are immutable (but you can always
	// replace a registered template in the system and it will affect
	// everything constructed from the template).
	//
	// Templates can be for arbitrary control rigs or for entire
	// devices. Device templates can use the 'deviceDescriptor' field
	// to specify regexs that are to match against compatible devices.
#if UNITY_EDITOR
	[Serializable]
#endif
    public class InputTemplate
#if UNITY_EDITOR
		: ISerializationCallbackReceiver
#endif
    {
	    // Both controls and processors can have public fields that can be set
	    // directly from templates. The values are usually specified in strings
	    // (like "clampMin=-1") but we parse them ahead of time into instances
	    // of this structure that tell us where to store the value in the control.
#if UNITY_EDITOR
		[Serializable]
#endif
	    public unsafe struct ParameterValue
	    {
		    public const int kMaxValueSize = 8;
		    
		    public uint offset;
		    public uint sizeInBytes;
		    public fixed byte value[kMaxValueSize];
	    }
	    
        // Specifies the composition of an input control.
#if UNITY_EDITOR
		[Serializable]
#endif
        public struct ControlTemplate
        {
            public string name; // Can be null/empty for "root" control but only one such control may exist.
            public string template;
	        public ParameterValue[] parameters;
            public string[] usages;
            public KeyValuePair<string, ParameterValue[]>[] processors;
            public uint offset;
            public uint bit;
        }

	    // Unique name of the template.
	    // NOTE: Case-insensitive.
	    public string name
	    {
		    get { return m_Name; }
	    }

	    public Type type
	    {
		    get { return m_Type; }
	    }

	    public string extendsTemplate
	    {
		    get { return m_ExtendsTemplate; }
	    }

	    // Unlike in a normal device descriptor, the strings in this descriptor are
	    // regular expressions which can be used to match against the strings of an
	    // actual device descriptor.
	    public InputDeviceDescriptor deviceDescriptor
	    {
		    get { return m_DeviceDescriptor; }
	    }
	    
        public ReadOnlyCollection<ControlTemplate> controls
        {
	        get
	        {
		        if (m_ControlsReadOnly == null)
		        {
			        var controls = m_Controls;
			        if (controls == null)
				        controls = Array.Empty<ControlTemplate>();
			        m_ControlsReadOnly = Array.AsReadOnly(controls);
		        }
		        return m_ControlsReadOnly;
	        }
        }

	    public string ToJson()
	    {
		    throw new NotImplementedException();
	    }

	    // Uses reflection to construct a template from the given type.
        // Can be used with both control classes and state structs.
        public static InputTemplate FromType(string name, Type type)
        {
	        var controlTemplates = new List<ControlTemplate>();
	        
	        // If it's a device with an InputStructAttribute, add control templates
	        // from its state (if present) instead of from the device.
	        var isDeviceWithStateAttribute = false;
	        if (typeof(InputDevice).IsAssignableFrom(type))
	        {
		        var stateAttribute = type.GetCustomAttribute<InputStateAttribute>();
		        if (stateAttribute != null)
		        {
			        isDeviceWithStateAttribute = true;
			        AddControlTemplates(stateAttribute.type, controlTemplates);
		        }
	        }
	        if (!isDeviceWithStateAttribute)
	        {
		        // Add control templates from type contents.
		        AddControlTemplates(type, controlTemplates);
	        }
	        
	        // Create template object.
	        var template = new InputTemplate(name, type);
	        template.m_Controls = controlTemplates.ToArray();
	        
	        return template;
        }

	    // Constructs a template from the given JSON source.
	    public static InputTemplate Parse(string json)
	    {
		    throw new NotImplementedException();
	    }

	    private string m_Name;
	    private Type m_Type;
	    private string m_ExtendsTemplate;
	    private List<string> m_OverridesTemplates;
	    internal ControlTemplate[] m_Controls;
	    private ReadOnlyCollection<ControlTemplate> m_ControlsReadOnly;
	    private InputDeviceDescriptor m_DeviceDescriptor;
        
        private InputTemplate(string name, Type type)
        {
	        m_Name = name;
	        m_Type = type;
        }

	    private static void AddControlTemplates(Type type, List<ControlTemplate> controlTemplates)
	    {
		    AddControlTemplatesFromFields(type, controlTemplates);
		    AddControlTemplatesFromProperties(type, controlTemplates);
	    }
	    
	    // Add ControlTemplates for every public property in the given type thas has
    	// InputControlAttribute applied to it or has an InputControl-derived value type.
	    private static void AddControlTemplatesFromFields(Type type, List<ControlTemplate> controlTemplates)
	    {
		    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
		    AddControlTemplatesFromMembers(fields, controlTemplates);
	    }
	    
	    // Add ControlTemplates for every public property in the given type thas has
    	// InputControlAttribute applied to it or has an InputControl-derived value type.
	    private static void AddControlTemplatesFromProperties(Type type, List<ControlTemplate> controlTemplates)
	    {
		    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		    AddControlTemplatesFromMembers(properties, controlTemplates);
	    }
	    
	    // Add ControlTemplates for every member in the list thas has InputControlAttribute applied to it
	    // or has an InputControl-derived value type.
	    private static void AddControlTemplatesFromMembers(MemberInfo[] members, List<ControlTemplate> controlTemplates)
	    {
	        foreach (var member in members)
	        {
                // Skip anything declared inside InputControl itself.
                // Filters out m_Device etc.
                if (member.DeclaringType == typeof(InputControl))
                    continue;
		        
                var valueType = TypeHelpers.GetValueType(member);
                
                // If the value type of the member is a struct type and implements the IInputStateTypeInfo
			    // interface, dive inside and look. This is useful for composing states of one another.
                if (valueType != null && valueType.IsValueType && typeof(IInputStateTypeInfo).IsAssignableFrom(valueType))
                {
                    AddControlTemplates(valueType, controlTemplates);
	                // We still fall back into the default codepath as there may also be attributes on
	                // the struct which modify the templates inside the struct.
	                ////TODO: modification isn't implemented ATM
                }

		        // Look for InputControlAttributes. If they aren't there, the member has to be
		        // of an InputControl-derived value type.
		        var attributes = member.GetCustomAttributes<InputControlAttribute>().ToArray();
		        if (attributes.Length == 0)
		        {
			        if (valueType == null || !typeof(InputControl).IsAssignableFrom(valueType))
			        	continue;
		        }

		        AddControlTemplatesFromMember(member, attributes, controlTemplates);
	        }
	    }

	    private static void AddControlTemplatesFromMember(MemberInfo member,
		    InputControlAttribute[] attributes, List<ControlTemplate> controlTemplates)
	    {
		    // InputControlAttribute can be applied multiple times to the same member,
		    // generating a separate control for each ocurrence. However, it can also
		    // not be applied at all in which case we still add a control template (the
		    // logic that called us already made sure the member is eligible for this kind
		    // of operation).

		    if (attributes.Length == 0)
		    {
			    var controlTemplate = CreateControlTemplateFromMember(member, null);
			    controlTemplates.Add(controlTemplate);
		    }
		    else
		    {
                foreach (var attribute in attributes)
                {
                    var controlTemplate = CreateControlTemplateFromMember(member, attribute);
                    controlTemplates.Add(controlTemplate);
                }
		    }
	    }
	    
	    private static ControlTemplate CreateControlTemplateFromMember(MemberInfo member, InputControlAttribute attribute)
	    {
		    ////REVIEW: make sure that the value type of the field and the value type of the control match?
		    
		    // Determine name.
		    var name = attribute?.name;
		    if (string.IsNullOrEmpty(name))
		    {
			    name = member.Name;
			    if (name.IndexOf('/') != -1)
				    throw new Exception($"InputControlAttribute annotations cannot have paths as names: " + name);
		    }

		    // Determine template.
		    var template = attribute?.template;
		    if (string.IsNullOrEmpty(template))
		    {
			    var valueType = TypeHelpers.GetValueType(member);
			    template = InferTemplateFromValueType(valueType);
		    }

		    // Determine offset.
		    var offset = InputStateBlock.kInvalidOffset;
		    if (member is FieldInfo)
			    offset = (uint)Marshal.OffsetOf(member.DeclaringType, member.Name).ToInt32();
		    
		    ////TODO: remaining template stuff

		    return new ControlTemplate
		    {
				name = name,
			    template = template,
			    offset = offset
		    };
	    }

	    private static string InferTemplateFromValueType(Type type)
	    {
		    var typeName = type.Name;
		    if (typeName.EndsWith("Control"))
			    return typeName.Substring(0, typeName.Length - "Control".Length);
		    return typeName;
	    }


        // This dictionary is owned and managed by InputManager.
        internal static Dictionary<string, InputTemplate> s_Templates;

        internal static InputTemplate TryGetTemplate(string name)
        {
            InputTemplate template;
            if (s_Templates.TryGetValue(name.ToLower(), out template))
                return template;
            return null;
        }

        internal static InputTemplate GetTemplate(string name)
        {
            InputTemplate template;
            if (!s_Templates.TryGetValue(name.ToLower(), out template))
                throw new Exception($"No input template called '{name}' has been registered");
            return template;
        }
	    

	    // Domain reload survival logic.
#if UNITY_EDITOR
	    [Serializable]
	    private struct SerializedState
	    {
		    public string name;
		    public string type;
		    public ControlTemplate[] controls;
	    }

	    [SerializeField] private SerializedState m_SerializedState;
	    
	    public void OnBeforeSerialize()
	    {
		    m_SerializedState = new SerializedState
		    {
			    name = m_Name,
			    type = m_Type.AssemblyQualifiedName,
			    controls = m_Controls
		    };
	    }

	    public void OnAfterDeserialize()
	    {
		    m_Name = m_SerializedState.name;
		    m_Type = Type.GetType(m_SerializedState.type, true);
		    m_Controls = m_SerializedState.controls;
		    
		    m_SerializedState = default(SerializedState);
	    }
#endif
    }

	// For constructing templates from code.
	public class InputTemplateBuilder
	{
	}
}