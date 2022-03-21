namespace Unity.InputSystem.DeviceDatabase.IR
{
    internal static class NativeCodeGenerator
    {
        // private static string VerticalAlign(string beforeColumn, string afterColumn, int beforeColumnMaxLength)
        // {
        //     var paddingLength = beforeColumnMaxLength - beforeColumn.Length;
        //     var padding = new string(' ', paddingLength > 0 ? paddingLength : 0);
        //     return $"{beforeColumn}{padding}{afterColumn}";
        // }
        //
        // private static string PadEnumKey(int spacesCount, string enumKey, string enumValue, int enumKeyMaxLength,
        //     bool lastKey = false)
        // {
        //     var tabs = new string(' ', spacesCount);
        //     var lastKeyString = lastKey ? "" : ",";
        //     return VerticalAlign($"{tabs}{enumKey}", $" = {enumValue}{lastKeyString}", enumKeyMaxLength + tabs.Length);
        // }

        public static string Generate(IRDeviceDatabase database)
        {
            var template = Scriban.Template.ParseLiquid(@"
#pragma once
// Auto generated. Do not edit.

#ifndef INPUT_BINDING_GENERATION
#include ""BuiltInControlTypes.h""

// -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----
// Enums
// -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----

enum class InputControlTypeBuiltIn
{
    Invalid = 0,
{%- for x in controlTypes %}
    {{x.name}} = {{x.assignedRef}}, // '{{x.displayName}}'
{%- endfor %}
};

enum class InputDeviceTraitBuiltIn
{
    Invalid = 0,
{%- for x in deviceTraits %}
    {{x.name}} = {{x.assignedRef}}, // '{{x.displayName}}'
{%- endfor %}
};

enum class InputControlUsageBuiltIn
{
    Invalid = 0,
{%- for x in allFlattenControls %}
    {{x.fullyQualifiedName}} = {{x.assignedRef}}, // '{{x.editorFriendlyFullyQualifiedName}}'
{%- endfor %}
};

enum class InputDeviceBuiltIn
{
    Invalid = 0,
{%- for x in devices %}
    {{x.name}} = {{x.assignedRef}}, // '{{x.displayName}}'
{%- endfor %}
};

// -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----
// Control Types
// -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----

{% for x in controlTypes %}
struct Input{{x.name}}ControlRef;
struct InputDerived{{x.name}}ControlRef;
{%- endfor %}

{% for x in controlTypes %}
struct InputDerived{{x.name}}ControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::{{x.name}})};
    typedef {{x.sampleTypeName}} SampleType;
    typedef {{x.stateTypeName}} StateType;
 
    InputControlRef controlRef;

    static inline InputDerived{{x.name}}ControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        InputDerived{{x.name}}ControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline InputDerived{{x.name}}ControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const {{x.sampleTypeName}} sample) const
    {
        {{x.ingressFunctionName}}(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const {{x.sampleTypeName}}* samples, const uint32_t count) const
    {
        {{x.ingressFunctionName}}(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const {{x.sampleTypeName}}* samples, const uint32_t count) const
    {
        {{x.ingressFunctionName}}(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline {{x.stateTypeName}} GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<{{x.stateTypeName}}*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        {{x.sampleTypeName}} sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<{{x.sampleTypeName}}*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        {{x.sampleTypeName}}* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<{{x.sampleTypeName}}*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }
};
{% endfor %}

{% for x in controlTypes %}
struct Input{{x.name}}ControlRef
{
    static constexpr InputControlTypeRef controlTypeRef = {static_cast<uint32_t>(InputControlTypeBuiltIn::{{x.name}})};
    typedef {{x.sampleTypeName}} SampleType;
    typedef {{x.stateTypeName}} StateType;
 
    InputControlRef controlRef;

    static inline Input{{x.name}}ControlRef Setup(const InputControlRef controlRef)
    {
        // TODO assert the type
        Input{{x.name}}ControlRef r = {};
        r.controlRef = controlRef;
        return r;
    }

    static inline Input{{x.name}}ControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return Setup(InputControlRef::Setup(usage, deviceRef));
    }

    inline void Ingress(const InputControlTimestamp timestamp, const {{x.sampleTypeName}} sample) const
    {
        {{x.ingressFunctionName}}(controlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
    }

    inline void Ingress(const InputControlTimestamp* timestamps, const {{x.sampleTypeName}}* samples, const uint32_t count) const
    {
        {{x.ingressFunctionName}}(controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
    }

    template<typename T>
    inline void IngressFrom(const T fromControl, const InputControlTimestamp* timestamps, const {{x.sampleTypeName}}* samples, const uint32_t count) const
    {
        {{x.ingressFunctionName}}(T::controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
    }

    inline {{x.stateTypeName}} GetState(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return *reinterpret_cast<{{x.stateTypeName}}*>(v.controlState);
    }

    struct LatestSample
    {
        InputControlTimestamp timestamp;
        {{x.sampleTypeName}} sample;
    };

    inline const LatestSample GetLatestSample(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericState v;
        InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
        return {
            *v.latestRecordedTimestamp,
            *reinterpret_cast<{{x.sampleTypeName}}*>(v.latestRecordedSample)
        };
    }

    struct Recording
    {
        InputControlTimestamp* timestamps;
        {{x.sampleTypeName}}* samples;
        uint32_t count;
    };

    inline const Recording GetRecording(const InputFramebufferRef framebufferRef) const
    {
        InputControlVisitorGenericRecordings v;
        InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
        return {
            v.allRecordedTimestamps,
            reinterpret_cast<{{x.sampleTypeName}}*>(v.allRecordedSamples),
            v.allRecordedCount
        };
    }

{% for y in x.virtualControls %}
    inline const InputDerived{{y.type}}ControlRef {{y.name}}{{y.type}}() const;
{%- endfor %}
{% for y in x.virtualControlsByType %}
    enum class {{y.type}}s
    {
{%- for z in y.controls %}
        {{z.name}},
{%- endfor %}
    };

    inline const InputDerived{{y.type}}ControlRef operator[](const {{y.type}}s value) const;
{% endfor -%}
};
{% endfor %}
{%- for x in controlTypes %}
{% for y in x.virtualControls %}
inline const InputDerived{{y.type}}ControlRef Input{{x.name}}ControlRef::{{y.name}}{{y.type}}() const { return InputDerived{{y.type}}ControlRef::Setup({controlRef.usage.transparent + static_cast<uint32_t>({{y.virtualControlRelativeIndex}})}, controlRef.deviceRef); }
{%- endfor %}
{% for y in x.virtualControlsByType %}
inline const InputDerived{{y.type}}ControlRef Input{{x.name}}ControlRef::operator[](const Input{{x.name}}ControlRef::{{y.type}}s value) const
{
    switch(value)
    {
{%- for z in y.controls %}
    case Input{{x.name}}ControlRef::{{y.type}}s::{{z.name}}: return {{z.name}}{{z.type}}();
{%- endfor %}
    default: InputAssert(false, ""Unknown control""); return InputDerived{{y.type}}ControlRef::Setup(InputControlRefInvalid);
    }
}
{%- endfor %}
{%- endfor %}

// -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----
// Device Traits
// -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----
{% for x in deviceTraits %}
struct Input{{x.name}}
{
    static constexpr InputDeviceTraitRef traitRef = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::{{x.name}})};
    InputDeviceRef deviceRef;

    static inline Input{{x.name}} Setup(const InputDeviceRef deviceRef)
    {
        // TODO assert that devices has the trait
        Input{{x.name}} r = {};
        r.deviceRef = deviceRef;
{%- for y in x.methods %}
        r.{{y.cppName}} = nullptr;
{%- endfor %}
        return r;
    }
{%- unless x.methods == empty %}
{% for y in x.methods %}
    typedef void (*{{y.name}}Type)({{y.cppArgs}});
{%- endfor %}
{% for y in x.methods %}
    {{y.name}}Type {{y.cppName}};
{%- endfor %}
{% endunless -%}
{% for y in x.controls %}
    inline const Input{{y.type}}ControlRef {{y.name}}{{y.type}}() const { return Input{{y.type}}ControlRef::Setup({static_cast<uint32_t>(InputControlUsageBuiltIn::{{y.fullyQualifiedName}})}, deviceRef); }
{%- endfor %}
{% for y in x.controlsByType %}
    enum class {{y.type}}s
    {
{%- for z in y.controls %}
        {{z.name}},
{%- endfor %}
    };

    inline const Input{{y.type}}ControlRef operator[](const {{y.type}}s value) const
    {
        switch(value)
        {
{%- for z in y.controls %}
        case {{y.type}}s::{{z.name}}: return {{z.name}}{{z.type}}();
{%- endfor %}
        default: InputAssert(false, ""Unknown control""); return Input{{y.type}}ControlRef::Setup(InputControlRefInvalid);
        }
    }
{% endfor -%}
};
{% endfor %}

#ifdef INPUT_NATIVE_DEVICE_DATABASE_PROVIDER

static inline uint32_t _InputStrToBuf(char* buffer, const uint32_t bufferCount, const char* str)
{
    const auto written = snprintf(buffer, bufferCount, ""%s"", str);
    return written > 0 ? static_cast<uint32_t>(written) : 0;
}

static inline InputDeviceDatabaseCallbacks _InputBuiltInDatabaseGetCallbacks()
{
    return {
        [](
            const InputControlTypeRef controlTypeRef,
            const InputControlRef controlRef,
            const InputControlTypeRef samplesType,
            const InputControlTimestamp* timestamps,
            const void* samples,
            const uint32_t count,
            const InputControlRef fromAnotherControl
        )
        {
            switch(static_cast<InputControlTypeBuiltIn>(controlTypeRef.transparent))
            {
            case InputControlTypeBuiltIn::Invalid: break;
{%- for x in controlTypes %}
            case InputControlTypeBuiltIn::{{x.name}}: {{x.ingressFunctionName}}(controlTypeRef, controlRef, samplesType, timestamps, samples, count, fromAnotherControl); break;
{%- endfor %}
            default:
                InputAssert(false, ""Trying to ingress to unknown type"");
                break;
            }
        },
        [](
            const InputControlTypeRef controlTypeRef,
            const InputControlRef* controlRefs,
            void* controlStates,
            InputControlTimestamp* latestRecordedTimestamps,
            void* latestRecordedSamples,
            const uint32_t controlCount
        )
        {
            // TODO type check pointer conversion!
            switch(static_cast<InputControlTypeBuiltIn>(controlTypeRef.transparent))
            {
            case InputControlTypeBuiltIn::Invalid: break;
{%- for x in controlTypes %}
            case InputControlTypeBuiltIn::{{x.name}}: {{x.frameBeginFunctionName}}(controlTypeRef, controlRefs, reinterpret_cast<{{x.stateTypeName}}*>(controlStates), latestRecordedTimestamps, reinterpret_cast<{{x.sampleTypeName}}*>(latestRecordedSamples), controlCount); break;
{%- endfor %}
            default:
                InputAssert(false, ""Trying to frame begin unknown type"");
                break;
            }
        },
        [](const InputDatabaseDeviceAssignedRef assignedRef, InputDeviceTraitRef* o, const uint32_t count)->uint32_t // GetDeviceTraits
        {
            switch(static_cast<InputDeviceBuiltIn>(assignedRef._opaque))
            {
{%- for x in devices %}
            case InputDeviceBuiltIn::{{ x.name }}:
                if (o != nullptr && count == {{x.traits | size}})
                {
{%- for yi in 0..(x.traits | size | minus 1) %}
                    o[{{yi}}] = {static_cast<uint32_t>(InputDeviceTraitBuiltIn::{{x.traits[yi]}})};
{%- endfor %}
                }
                else if (o != nullptr)
                    InputAssert(false, ""Please provide {{x.traits | size}} elements"");
                return {{x.traits | size}};
{%- endfor %}
            default:
                return 0;
            }
        },
        [](const InputDeviceTraitRef traitRef)->uint32_t // GetTraitSizeInBytes
        {
            switch(static_cast<InputDeviceTraitBuiltIn>(traitRef.transparent))
            {
{%- for x in deviceTraits %}
            case InputDeviceTraitBuiltIn::{{x.name}}: return static_cast<uint32_t>(sizeof(Input{{x.name}}));
{%- endfor %}
            default: return 0;
            }
        },
        [](const InputDeviceTraitRef traitRef, const InputDeviceRef deviceRef, InputControlRef* o, const uint32_t count)->uint32_t // GetTraitControls
        {
            switch(static_cast<InputDeviceTraitBuiltIn>(traitRef.transparent))
            {
{%- for x in deviceTraits %}
            case InputDeviceTraitBuiltIn::{{x.name}}:
                if (o != nullptr && count == {{x.flattenControls | size}})
                {
{%- unless (x.flattenControls | size) == 0 -%}
{%- for yi in 0..(x.flattenControls | size | minus 1) %}
                    o[{{yi}}] = InputControlRef::Setup( {static_cast<uint32_t>(InputControlUsageBuiltIn::{{x.flattenControls[yi].fullyQualifiedName}})}, deviceRef);
{%- endfor %}
{%- endunless -%}
                }
                else if (o != nullptr)
                    InputAssert(false, ""Please provide {{x.flattenControls | size}} elements"");
                return {{x.flattenControls | size}};
{%- endfor %}
            default: return 0;
            }
        },
        [](const InputDeviceTraitRef traitRef, void* traitPointer, const InputDeviceRef deviceRef)->void // ConfigureTraitInstance
        {
            switch(static_cast<InputDeviceTraitBuiltIn>(traitRef.transparent))
            {
{%- for x in deviceTraits %}
            case InputDeviceTraitBuiltIn::{{x.name}}: *reinterpret_cast<Input{{x.name}}*>(traitPointer) = Input{{x.name}}::Setup(deviceRef); break;
{%- endfor %}
            default: break;
            }
        },
        [](const InputControlUsage usage)->InputDatabaseControlUsageDescr // GetControlUsageDescr
        {
            switch(static_cast<InputControlUsageBuiltIn>(usage.transparent))
            {
{%- for x in allFlattenControls %}
            case InputControlUsageBuiltIn::{{x.fullyQualifiedName}}: return {
                {static_cast<uint32_t>(InputControlTypeBuiltIn::{{x.assignedControlType.name}})},
                InputControlRecordingMode::{{x.assignedControlType.defaultRecordingMode}},
{%- unless x.parentOfVirtualControlFullyQualifiedName == """" %}
                {static_cast<uint32_t>(InputControlUsageBuiltIn::{{x.parentOfVirtualControlFullyQualifiedName}})}
{% else %}
                InputControlUsageInvalid
{% endunless %}
            };
{%- endfor %}
            default:
                return {};
            }
        },
        [](const InputControlTypeRef controlTypeRef)->InputDatabaseControlTypeDescr // GetControlTypeDescr
        {
            switch(static_cast<InputControlTypeBuiltIn>(controlTypeRef.transparent))
            {
{%- for x in controlTypes %}
            case InputControlTypeBuiltIn::{{x.name}}: return {
                sizeof({{x.stateTypeName}}),
                sizeof({{x.sampleTypeName}})
            };
{%- endfor %}
            default:
                return {};
            }
        },
        // TODO replace guid to id lookups with hashmap
        [](const InputGuid g)->InputDatabaseDeviceAssignedRef // GetDeviceAssignedRef
        {
{%- for x in devices %}
            if (g.a == {{x.guidAsPartA}}ull && g.b == {{x.guidAsPartB}}ull) return {static_cast<uint32_t>(InputDeviceBuiltIn::{{x.name}})}; // {{x.guid}}
{%- endfor %}
            return InputDatabaseDeviceAssignedRefInvalid;
        },
        [](const InputGuid g)->InputDeviceTraitRef // GetTraitAssignedRef
        {
{%- for x in deviceTraits %}
            if (g.a == {{x.guidAsPartA}}ull && g.b == {{x.guidAsPartB}}ull) return {static_cast<uint32_t>(InputDeviceTraitBuiltIn::{{x.name}})}; // {{x.guid}}
{%- endfor %}
            return InputDeviceTraitRefInvalid;
        },
        [](const InputGuid g)->InputControlUsage // GetControlUsage
        {
{%- for x in allFlattenControls %}
            if (g.a == {{x.guidAsPartA}}ull && g.b == {{x.guidAsPartB}}ull) return {static_cast<uint32_t>(InputControlUsageBuiltIn::{{x.fullyQualifiedName}})}; // {{x.guid}}
{%- endfor %}
            return InputControlUsageInvalid;
        },
        [](const InputGuid g)->InputControlTypeRef // GetControlTypeRef
        {
{%- for x in controlTypes %}
            if (g.a == {{x.guidAsPartA}}ull && g.b == {{x.guidAsPartB}}ull) return {static_cast<uint32_t>(InputControlTypeBuiltIn::{{x.name}})}; // {{x.guid}}
{%- endfor %}
            return InputControlTypeRefInvalid;
        },
        [](const InputDatabaseDeviceAssignedRef assignedRef)->InputGuid // GetDeviceGuid
        {
            switch(static_cast<InputDeviceBuiltIn>(assignedRef._opaque))
            {
{%- for x in devices %}
            case InputDeviceBuiltIn::{{ x.name }}: return { {{x.guidAsPartA}}ull, {{x.guidAsPartB}}ull }; // {{x.guid}} '{{x.displayName}}'
{%- endfor %}
            default:
                return InputGuidInvalid;
            }
        },
        [](const InputDeviceTraitRef traitRef)->InputGuid // GetTraitGuid
        {
            switch(static_cast<InputDeviceTraitBuiltIn>(traitRef.transparent))
            {
{%- for x in deviceTraits %}
            case InputDeviceTraitBuiltIn::{{x.name}}: return { {{x.guidAsPartA}}ull, {{x.guidAsPartB}}ull }; // {{x.guid}}
{%- endfor %}
            default:
                return InputGuidInvalid;
            }
        },
        [](const InputControlUsage usage)->InputGuid // GetControlGuid
        {
            switch(static_cast<InputControlUsageBuiltIn>(usage.transparent))
            {
{%- for x in allFlattenControls %}
            case InputControlUsageBuiltIn::{{x.fullyQualifiedName}}: return { {{x.guidAsPartA}}ull, {{x.guidAsPartB}}ull }; // {{x.guid}}
{%- endfor %}
            default:
                return InputGuidInvalid;
            }
        },
        [](const InputControlTypeRef controlTypeRef)->InputGuid // GetControlTypeGuid
        {
            switch(static_cast<InputControlTypeBuiltIn>(controlTypeRef.transparent))
            {
{%- for x in controlTypes %}
            case InputControlTypeBuiltIn::{{x.name}}: return { {{x.guidAsPartA}}ull, {{x.guidAsPartB}}ull }; // {{x.guid}}
{%- endfor %}
            default:
                return InputGuidInvalid;
            }
        },
        []()->uint32_t // GetDeviceRefCount
        {
            return {{maxDeviceAssignedRef}} + 1; // max value + 1
        },
        []()->uint32_t // GetTraitRefCount
        {
            return {{maxDeviceTraitAssignedRef}} + 1; // max value + 1
        },
        []()->uint32_t // GetControlUsageCount
        {
            return {{maxControlUsageAssignedRef}} + 1; // max value + 1
        },
        []()->uint32_t // GetControlTypeCount
        {
            return {{maxControlTypeAssignedRef}} + 1; // max value + 1
        },
        [](const InputDatabaseDeviceAssignedRef assignedRef, char* o, const uint32_t c)->uint32_t // GetDeviceName
        {
            switch(static_cast<InputDeviceBuiltIn>(assignedRef._opaque))
            {
{%- for x in devices %}
            case InputDeviceBuiltIn::{{x.name}}: return _InputStrToBuf(o, c, ""{{x.displayName}}"");
{%- endfor %}
            default: return 0;
            }
        },
        [](const InputDeviceTraitRef traitRef, char* o, const uint32_t c)->uint32_t // GetTraitName
        {
            switch(static_cast<InputDeviceTraitBuiltIn>(traitRef.transparent))
            {
{%- for x in deviceTraits %}
            case InputDeviceTraitBuiltIn::{{x.name}}: return _InputStrToBuf(o, c, ""{{x.displayName}}"");
{%- endfor %}
            default: return 0;
            }
        },
        [](const InputControlUsage usage, char* o, const uint32_t c)->uint32_t // GetControlFullName
        {
            switch(static_cast<InputControlUsageBuiltIn>(usage.transparent))
            {
{%- for x in allFlattenControls %}
            case InputControlUsageBuiltIn::{{x.fullyQualifiedName}}: return _InputStrToBuf(o, c, ""{{x.editorFriendlyFullyQualifiedName}}"");
{%- endfor %}
            default: return 0;
            }
        },
        [](const InputControlTypeRef controlTypeRef, char* o, const uint32_t c)->uint32_t // GetControlTypeName
        {
            switch(static_cast<InputControlTypeBuiltIn>(controlTypeRef.transparent))
            {
{%- for x in controlTypes %}
            case InputControlTypeBuiltIn::{{x.name}}: return _InputStrToBuf(o, c, ""{{x.displayName}}"");
{%- endfor %}
            default: return 0;
            }
        },
    };
}

#endif

#endif
");

            return template.Render(database, member => member.Name);
        }
    }
}