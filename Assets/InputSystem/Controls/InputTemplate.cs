using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

////REVIEW: make templates the container and control specs the contents?
////REVIEW: rename InputControlTemplate?

namespace InputSystem
{
	// A template lays out the composition of an input control.
    public class InputTemplate
    {
	    // Both controls and processors can have public fields that can be set
	    // directly from templates. The values are usually specified in strings
	    // (like "clampMin=-1") but we parse them ahead of time into instances
	    // of this structure that tell us where to store the value in the control.
	    public unsafe struct ParameterValue
	    {
		    public const int kMaxValueSize = 8;
		    
		    public uint offset;
		    public uint sizeInBytes;
		    public fixed byte value[kMaxValueSize];
	    }
	    
        // Specifies the composition of an input control.
        public struct ControlTemplate
        {
            public string parent;
            public string name;
            public string template;
	        public ParameterValue[] parameters;
            public string[] usages;
            public KeyValuePair<string, ParameterValue[]>[] processors;
            public uint offset;
            public uint bit;
        }

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
        
        private InputTemplate(string name, Type type)
        {
	        m_Name = name;
	        m_Type = type;
        }

        // Uses reflection to construct a template from the given type.
        // Can be used with both control classes and state structs.
        public static InputTemplate FromType(Type type)
        {
	        /*
	       	        ////TODO: need to look *inside* the types of fields to find controls nested inside the types
	        ////TODO: allow referring to a *subcontrol* in the current field by name
	        // Construct controls from fields.
	        foreach (var field in type.GetFields(BindingFlags.FlattenHierarchy))
	        {
		        var attribute = field.GetCustomAttribute<InputControlAttribute>();
		        if (attribute != null)
		        {
			        ////REVIEW: make sure that the value type of the field and the value type of the control match?
			        var offset = Marshal.OffsetOf(type, field.Name);
			        var name = attribute.name;
			        if (string.IsNullOrEmpty(name))
				        name = field.Name;
			        var typeName = attribute.type;
			        if (string.IsNullOrEmpty(typeName))
				        typeName = field.FieldType.Name;
			        var control = (InputControl) Activator.CreateInstance(attribute.type, name);
			        control.stateBlock.byteOffset = (uint) offset.ToInt32();
			        control.stateBlock.bitOffset = (uint) attribute.bit;
		        }
	        }
	        */
            throw new NotImplementedException();
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
	    private ControlTemplate[] m_Controls;
	    private ReadOnlyCollection<ControlTemplate> m_ControlsReadOnly;
	    private InputDeviceDescriptor m_DeviceDescriptor;
        

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
    }

	// For constructing templates from code.
	public class InputTemplateBuilder
	{
	}
}