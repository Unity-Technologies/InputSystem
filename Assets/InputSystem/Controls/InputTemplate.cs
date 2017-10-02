using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

////REVIEW: rename InputControlTemplate?

namespace ISX
{
	// A template lays out the composition of an input control.
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
	    
	    ////TODO: turn into IEnumerable
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

	    // Add all fields of the given type that are marked with InputControlAttribute
	    // as ControlTemplate children to the given control template.
	    private void AddControlFieldsAsChildren(Type type, ref ControlTemplate template)
	    {
	       	////TODO: need to look *inside* the types of fields to find controls nested inside the types (really?)
	        ////TODO: allow referring to a *subcontrol* in the current field by name
	        foreach (var field in type.GetFields(BindingFlags.FlattenHierarchy))
	        {
		        var attribute = field.GetCustomAttribute<InputControlAttribute>();
		        if (attribute == null)
			        continue;
		        
                ////REVIEW: make sure that the value type of the field and the value type of the control match?
                var offset = Marshal.OffsetOf(type, field.Name);
                var name = attribute.name;
                if (string.IsNullOrEmpty(name))
                    name = field.Name;
                var typeName = attribute.type;
                if (string.IsNullOrEmpty(typeName))
                    typeName = field.FieldType.Name;
	        }
	    }

        // Uses reflection to construct a template from the given type.
        // Can be used with both control classes and state structs.
        public static InputTemplate FromType(string name, Type type)
        {
	        var template = new InputTemplate(name, type);
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